using FlightBookingSystem.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PassengerService.Clients;
using PassengerService.Data;
using PassengerService.DTOs;
using PassengerService.Entities;
using PassengerService.Repositories;
using PassengerService.Services;

namespace FlightBookingSystem.Tests;

[TestFixture]
public class PassengerServiceTests
{
    private PassengerDbContext _dbContext = null!;
    private IPassengerRepository _passengerRepository = null!;
    private ICheckInRepository _checkInRepository = null!;
    private Mock<IBookingServiceClient> _mockBookingClient = null!;
    private Mock<ILogger<CheckInServiceImpl>> _mockLogger = null!;
    private IPassengerService _passengerService = null!;
    private ICheckInService _checkInService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PassengerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new PassengerDbContext(options);
        _dbContext.Database.EnsureCreated();

        _passengerRepository = new PassengerRepository(_dbContext);
        _checkInRepository = new CheckInRepository(_dbContext);
        _mockBookingClient = new Mock<IBookingServiceClient>();
        _mockLogger = new Mock<ILogger<CheckInServiceImpl>>();

        _passengerService = new PassengerServiceImpl(_passengerRepository);
        _checkInService = new CheckInServiceImpl(
            _checkInRepository,
            _passengerRepository,
            _mockBookingClient.Object,
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
    public async Task CreateBulkPassengers_SavesAllPassengerRecords()
    {
        var request = new CreatePassengersBulkRequestDto
        {
            BookingId = 50,
            Passengers = new List<PassengerCreateItemDto>
            {
                new() { PassengerName = "Alice", Age = 28, Gender = "Female", SeatNumber = "1A" },
                new() { PassengerName = "Bob", Age = 32, Gender = "Male", SeatNumber = "1B" }
            }
        };

        var response = await _passengerService.CreatePassengersBulkAsync(request);

        Assert.That(response.Success, Is.True);
        Assert.That(response.Data!.Count, Is.EqualTo(2));
        Assert.That(response.Data[0].SeatNumber, Is.EqualTo("1A"));
        Assert.That(response.Data[1].SeatNumber, Is.EqualTo("1B"));
    }

    [Test]
    public async Task CheckIn_ValidConfirmedBooking_SucceedsAndRetainsSeat()
    {
        // 1. Setup passenger in DB
        var passenger = new Passenger
        {
            BookingId = 100,
            PassengerName = "Charlie Davis",
            Age = 35,
            Gender = "Male",
            SeatNumber = "2C"
        };
        await _dbContext.Passengers.AddAsync(passenger);
        await _dbContext.SaveChangesAsync();

        // 2. Setup mock booking service returning Confirmed status
        _mockBookingClient.Setup(b => b.GetBookingByPnrAsync("CONFIRM1"))
            .ReturnsAsync(new BookingRemoteValidationDto
            {
                BookingId = 100,
                UserId = 5,
                FlightId = 1,
                BookingStatus = BookingStatusConstants.Confirmed,
                PNR = "CONFIRM1"
            });

        // 3. Perform check-in
        var checkInRequest = new CreateCheckInRequestDto
        {
            PNR = "CONFIRM1",
            PassengerId = passenger.PassengerId
        };

        var result = await _checkInService.CreateCheckInAsync(checkInRequest);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.CheckInStatus, Is.EqualTo(CheckInStatusConstants.CheckedIn));
        Assert.That(result.Data.SeatNumber, Is.EqualTo("2C")); // Seat retained
    }

    [Test]
    public async Task CheckIn_DuplicateCheckIn_Rejects()
    {
        var passenger = new Passenger
        {
            BookingId = 101,
            PassengerName = "Diana Prince",
            Age = 30,
            Gender = "Female",
            SeatNumber = "3A"
        };
        await _dbContext.Passengers.AddAsync(passenger);
        await _dbContext.SaveChangesAsync();

        _mockBookingClient.Setup(b => b.GetBookingByPnrAsync("CONFIRM2"))
            .ReturnsAsync(new BookingRemoteValidationDto
            {
                BookingId = 101,
                BookingStatus = BookingStatusConstants.Confirmed,
                PNR = "CONFIRM2"
            });

        var checkInRequest = new CreateCheckInRequestDto
        {
            PNR = "CONFIRM2",
            PassengerId = passenger.PassengerId
        };

        // First check-in
        var firstResult = await _checkInService.CreateCheckInAsync(checkInRequest);
        Assert.That(firstResult.Success, Is.True);

        // Second check-in (duplicate)
        var secondResult = await _checkInService.CreateCheckInAsync(checkInRequest);
        Assert.That(secondResult.Success, Is.False);
        Assert.That(secondResult.Message, Does.Contain("already checked in"));
    }

    [Test]
    public async Task CheckIn_UnconfirmedBooking_RejectsCheckIn()
    {
        var passenger = new Passenger
        {
            BookingId = 102,
            PassengerName = "Pending User",
            Age = 22,
            Gender = "Other",
            SeatNumber = "4B"
        };
        await _dbContext.Passengers.AddAsync(passenger);
        await _dbContext.SaveChangesAsync();

        _mockBookingClient.Setup(b => b.GetBookingByPnrAsync("PENDING1"))
            .ReturnsAsync(new BookingRemoteValidationDto
            {
                BookingId = 102,
                BookingStatus = BookingStatusConstants.Pending, // Pending!
                PNR = "PENDING1"
            });

        var checkInRequest = new CreateCheckInRequestDto
        {
            PNR = "PENDING1",
            PassengerId = passenger.PassengerId
        };

        var result = await _checkInService.CreateCheckInAsync(checkInRequest);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("only allowed for Confirmed bookings"));
    }
}
