using FlightBookingSystem.Shared.Constants;
using FlightService.Data;
using FlightService.DTOs;
using FlightService.Repositories;
using FlightService.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace FlightBookingSystem.Tests;

[TestFixture]
public class FlightServiceTests
{
    private FlightDbContext _dbContext = null!;
    private IFlightRepository _flightRepository = null!;
    private IFlightSeatRepository _seatRepository = null!;
    private IFlightService _flightService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<FlightDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new FlightDbContext(options);
        _dbContext.Database.EnsureCreated();

        _flightRepository = new FlightRepository(_dbContext);
        _seatRepository = new FlightSeatRepository(_dbContext);
        _flightService = new FlightServiceImpl(_flightRepository, _seatRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task SearchFlights_SameSourceAndDestination_ReturnsFail()
    {
        var request = new FlightSearchRequestDto
        {
            Source = "DEL",
            Destination = "DEL",
            TravelDate = DateTime.UtcNow.AddDays(1)
        };

        var result = await _flightService.SearchFlightsAsync(request);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("must differ"));
    }

    [Test]
    public async Task SearchFlights_ValidQuery_ReturnsMatchingFlights()
    {
        var departure = DateTime.UtcNow.Date.AddDays(2).AddHours(10);
        var createDto = new CreateFlightDto
        {
            FlightNumber = "TEST-100",
            Airline = "Test Airways",
            Source = "NYC",
            Destination = "LON",
            DepartureTime = departure,
            ArrivalTime = departure.AddHours(7),
            TotalSeats = 6,
            BaseFare = 500.00m
        };

        await _flightService.CreateFlightAsync(createDto);

        var searchResult = await _flightService.SearchFlightsAsync(new FlightSearchRequestDto
        {
            Source = "NYC",
            Destination = "LON",
            TravelDate = departure.Date
        });

        Assert.That(searchResult.Success, Is.True);
        Assert.That(searchResult.Data, Is.Not.Null);
        Assert.That(searchResult.Data!.Count, Is.GreaterThanOrEqualTo(1));
        Assert.That(searchResult.Data[0].FlightNumber, Is.EqualTo("TEST-100"));
    }

    [Test]
    public async Task CreateFlight_AutoGeneratesSeatsLayout()
    {
        var departure = DateTime.UtcNow.Date.AddDays(3).AddHours(9);
        var createDto = new CreateFlightDto
        {
            FlightNumber = "AUTO-SEATS",
            Airline = "Delta",
            Source = "ATL",
            Destination = "MIA",
            DepartureTime = departure,
            ArrivalTime = departure.AddHours(2),
            TotalSeats = 6,
            BaseFare = 200.00m
        };

        var response = await _flightService.CreateFlightAsync(createDto);

        Assert.That(response.Success, Is.True);
        Assert.That(response.Data, Is.Not.Null);

        var seatMap = await _flightService.GetSeatMapAsync(response.Data!.FlightId);
        Assert.That(seatMap.Success, Is.True);
        Assert.That(seatMap.Data!.Seats.Count, Is.EqualTo(6));
        Assert.That(seatMap.Data.Seats.All(s => s.IsAvailable && s.SeatStatus == SeatStatusConstants.Available), Is.True);
    }

    [Test]
    public async Task CreateFlight_ArrivalEarlierThanDeparture_Fails()
    {
        var departure = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
        var createDto = new CreateFlightDto
        {
            FlightNumber = "INVALID-TIME",
            Airline = "Invalid Air",
            Source = "SFO",
            Destination = "LAX",
            DepartureTime = departure,
            ArrivalTime = departure.AddHours(-1), // earlier than departure
            TotalSeats = 10,
            BaseFare = 150.00m
        };

        var result = await _flightService.CreateFlightAsync(createDto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("later than departure"));
    }

    [Test]
    public async Task HoldSeats_WhenAvailable_Succeeds()
    {
        var flight = await _flightRepository.GetByIdAsync(1);
        Assert.That(flight, Is.Not.Null);

        var holdRequest = new HoldSeatsRequestDto
        {
            BookingId = 101,
            SeatNumbers = new List<string> { "1A", "1B" }
        };

        var holdResult = await _flightService.HoldSeatsAsync(flight!.FlightId, holdRequest);

        Assert.That(holdResult.Success, Is.True);

        var seatMap = await _flightService.GetSeatMapAsync(flight.FlightId);
        var seat1A = seatMap.Data!.Seats.First(s => s.SeatNumber == "1A");
        Assert.That(seat1A.SeatStatus, Is.EqualTo(SeatStatusConstants.Held));
        Assert.That(seat1A.IsAvailable, Is.False);
    }

    [Test]
    public async Task ConfirmAndReleaseSeats_UpdatesStatusesCorrectly()
    {
        var flight = await _flightRepository.GetByIdAsync(2);
        Assert.That(flight, Is.Not.Null);

        // Hold
        await _flightService.HoldSeatsAsync(flight!.FlightId, new HoldSeatsRequestDto
        {
            BookingId = 202,
            SeatNumbers = new List<string> { "2A" }
        });

        // Confirm
        var confirmResult = await _flightService.ConfirmSeatsAsync(flight.FlightId, new ConfirmSeatsRequestDto
        {
            BookingId = 202,
            SeatNumbers = new List<string> { "2A" }
        });
        Assert.That(confirmResult.Success, Is.True);

        var seatMapConfirmed = await _flightService.GetSeatMapAsync(flight.FlightId);
        var seat2A = seatMapConfirmed.Data!.Seats.First(s => s.SeatNumber == "2A");
        Assert.That(seat2A.SeatStatus, Is.EqualTo(SeatStatusConstants.Booked));

        // Release
        var releaseResult = await _flightService.ReleaseSeatsAsync(flight.FlightId, new ReleaseSeatsRequestDto
        {
            BookingId = 202,
            SeatNumbers = new List<string> { "2A" }
        });
        Assert.That(releaseResult.Success, Is.True);

        var seatMapReleased = await _flightService.GetSeatMapAsync(flight.FlightId);
        var seat2AReleased = seatMapReleased.Data!.Seats.First(s => s.SeatNumber == "2A");
        Assert.That(seat2AReleased.SeatStatus, Is.EqualTo(SeatStatusConstants.Available));
        Assert.That(seat2AReleased.IsAvailable, Is.True);
    }
}
