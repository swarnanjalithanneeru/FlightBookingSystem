using BookingService.Clients;
using BookingService.Data;
using BookingService.DTOs;
using BookingService.Repositories;
using BookingService.Services;
using FlightBookingSystem.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FlightBookingSystem.Tests;

[TestFixture]
public class BookingServiceTests
{
    private BookingDbContext _dbContext = null!;
    private IBookingRepository _bookingRepository = null!;
    private Mock<IFlightServiceClient> _mockFlightClient = null!;
    private Mock<IPassengerServiceClient> _mockPassengerClient = null!;
    private Mock<IPaymentServiceClient> _mockPaymentClient = null!;
    private Mock<ILogger<BookingServiceImpl>> _mockLogger = null!;
    private IBookingService _bookingService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new BookingDbContext(options);
        _dbContext.Database.EnsureCreated();

        _bookingRepository = new BookingRepository(_dbContext);
        _mockFlightClient = new Mock<IFlightServiceClient>();
        _mockPassengerClient = new Mock<IPassengerServiceClient>();
        _mockPaymentClient = new Mock<IPaymentServiceClient>();
        _mockLogger = new Mock<ILogger<BookingServiceImpl>>();

        _bookingService = new BookingServiceImpl(
            _bookingRepository,
            _mockFlightClient.Object,
            _mockPassengerClient.Object,
            _mockPaymentClient.Object,
            _mockLogger.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task CreateBooking_DuplicateSeats_RejectsRequest()
    {
        var request = new CreateBookingRequestDto
        {
            FlightId = 1,
            Passengers = new List<PassengerBookingRequestItemDto>
            {
                new() { PassengerName = "Passenger One", Age = 30, Gender = "Male", SeatNumber = "1A" },
                new() { PassengerName = "Passenger Two", Age = 25, Gender = "Female", SeatNumber = "1A" } // duplicate seat!
            }
        };

        var result = await _bookingService.CreateBookingAsync(userId: 10, request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("distinct seat"));
    }

    [Test]
    public async Task CreateBooking_SuccessfulPayment_ConfirmsBookingAndGeneratesPnr()
    {
        int flightId = 1;
        decimal baseFare = 3000m;

        _mockFlightClient.Setup(f => f.GetFlightAsync(flightId))
            .ReturnsAsync(new FlightInfoRemoteDto
            {
                FlightId = flightId,
                FlightNumber = "AI-101",
                BaseFare = baseFare,
                IsActive = true
            });

        _mockFlightClient.Setup(f => f.GetSeatMapAsync(flightId))
            .ReturnsAsync(new SeatMapRemoteDto
            {
                FlightId = flightId,
                Seats = new List<FlightSeatRemoteDto>
                {
                    new() { SeatNumber = "1A", IsAvailable = true, SeatStatus = SeatStatusConstants.Available },
                    new() { SeatNumber = "1B", IsAvailable = true, SeatStatus = SeatStatusConstants.Available }
                }
            });

        _mockFlightClient.Setup(f => f.HoldSeatsAsync(flightId, It.IsAny<List<string>>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _mockFlightClient.Setup(f => f.ConfirmSeatsAsync(flightId, It.IsAny<List<string>>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _mockPaymentClient.Setup(p => p.SimulatePaymentAsync(It.IsAny<int>(), 6000m, "CreditCard", "Success"))
            .ReturnsAsync((true, "TXN-TEST-12345", "Payment successful."));

        _mockPassengerClient.Setup(p => p.CreatePassengersBulkAsync(It.IsAny<int>(), It.IsAny<List<PassengerBookingRequestItemDto>>()))
            .ReturnsAsync(new List<PassengerRecordDto>
            {
                new() { PassengerId = 1, PassengerName = "P1", SeatNumber = "1A" },
                new() { PassengerId = 2, PassengerName = "P2", SeatNumber = "1B" }
            });

        var request = new CreateBookingRequestDto
        {
            FlightId = flightId,
            PaymentMethod = "CreditCard",
            SimulatePaymentOutcome = "Success",
            Passengers = new List<PassengerBookingRequestItemDto>
            {
                new() { PassengerName = "P1", Age = 30, Gender = "Male", SeatNumber = "1A" },
                new() { PassengerName = "P2", Age = 28, Gender = "Female", SeatNumber = "1B" }
            }
        };

        var result = await _bookingService.CreateBookingAsync(userId: 10, request);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.BookingStatus, Is.EqualTo(BookingStatusConstants.Confirmed));
        Assert.That(result.Data.TotalAmount, Is.EqualTo(6000m)); // Authoritative calculation (3000 * 2)
        Assert.That(result.Data.PNR, Is.Not.Null.And.Not.Empty);

        // Verify seats were confirmed
        _mockFlightClient.Verify(f => f.ConfirmSeatsAsync(flightId, It.IsAny<List<string>>(), It.IsAny<int>()), Times.Once);
    }

    [Test]
    public async Task CreateBooking_FailedPayment_MarksBookingFailedAndReleasesSeats()
    {
        int flightId = 1;
        _mockFlightClient.Setup(f => f.GetFlightAsync(flightId))
            .ReturnsAsync(new FlightInfoRemoteDto
            {
                FlightId = flightId,
                FlightNumber = "AI-101",
                BaseFare = 4000m,
                IsActive = true
            });

        _mockFlightClient.Setup(f => f.GetSeatMapAsync(flightId))
            .ReturnsAsync(new SeatMapRemoteDto
            {
                FlightId = flightId,
                Seats = new List<FlightSeatRemoteDto>
                {
                    new() { SeatNumber = "3A", IsAvailable = true, SeatStatus = SeatStatusConstants.Available }
                }
            });

        _mockFlightClient.Setup(f => f.HoldSeatsAsync(flightId, It.IsAny<List<string>>(), It.IsAny<int>()))
            .ReturnsAsync(true);

        _mockPaymentClient.Setup(p => p.SimulatePaymentAsync(It.IsAny<int>(), 4000m, "DebitCard", "Failed"))
            .ReturnsAsync((false, string.Empty, "Insufficient funds simulation"));

        var request = new CreateBookingRequestDto
        {
            FlightId = flightId,
            PaymentMethod = "DebitCard",
            SimulatePaymentOutcome = "Failed",
            Passengers = new List<PassengerBookingRequestItemDto>
            {
                new() { PassengerName = "Failed Passenger", Age = 40, Gender = "Male", SeatNumber = "3A" }
            }
        };

        var result = await _bookingService.CreateBookingAsync(userId: 10, request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Payment failed"));

        // Verify seats were released upon payment failure (reliability)
        _mockFlightClient.Verify(f => f.ReleaseSeatsAsync(flightId, It.Is<List<string>>(s => s.Contains("3A")), It.IsAny<int>()), Times.Once);
    }

    [Test]
    public async Task CancelBooking_WhenConfirmedAndOwnedByUser_CancelsAndReleasesSeats()
    {
        var booking = new BookingService.Entities.Booking
        {
            UserId = 15,
            FlightId = 1,
            TotalAmount = 5000m,
            BookingStatus = BookingStatusConstants.Confirmed,
            PNR = "TEST01",
            BookingDate = DateTime.UtcNow
        };
        await _bookingRepository.AddAsync(booking);

        _mockPaymentClient.Setup(p => p.SimulateRefundAsync(booking.BookingId, 5000m))
            .ReturnsAsync(true);

        _mockPassengerClient.Setup(p => p.GetPassengersByBookingIdAsync(booking.BookingId))
            .ReturnsAsync(new List<PassengerRecordDto>
            {
                new() { PassengerId = 1, SeatNumber = "1A" }
            });

        _mockFlightClient.Setup(f => f.ReleaseSeatsAsync(booking.FlightId, It.IsAny<List<string>>(), booking.BookingId))
            .ReturnsAsync(true);

        var cancelResult = await _bookingService.CancelBookingAsync(booking.BookingId, requestUserId: 15, isAdmin: false);

        Assert.That(cancelResult.Success, Is.True);
        Assert.That(cancelResult.Data!.BookingStatus, Is.EqualTo(BookingStatusConstants.Cancelled));
        Assert.That(cancelResult.Data.RefundStatus, Is.EqualTo(PaymentStatusConstants.Refunded));

        // Verify seats released
        _mockFlightClient.Verify(f => f.ReleaseSeatsAsync(booking.FlightId, It.IsAny<List<string>>(), booking.BookingId), Times.Once);
    }

    [Test]
    public async Task CancelBooking_WhenUserNotOwner_ReturnsUnauthorized()
    {
        var booking = new BookingService.Entities.Booking
        {
            UserId = 15,
            FlightId = 1,
            TotalAmount = 5000m,
            BookingStatus = BookingStatusConstants.Confirmed,
            PNR = "TEST02"
        };
        await _bookingRepository.AddAsync(booking);

        var result = await _bookingService.CancelBookingAsync(booking.BookingId, requestUserId: 999, isAdmin: false);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Unauthorized"));
    }
}
