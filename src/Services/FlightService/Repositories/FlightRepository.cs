using FlightService.Data;
using FlightService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightService.Repositories;

public interface IFlightRepository
{
    Task<Flight?> GetByIdAsync(int flightId);
    Task<Flight?> GetWithSeatsByIdAsync(int flightId);
    Task<List<Flight>> SearchFlightsAsync(string source, string destination, DateTime travelDate);
    Task<List<Flight>> GetAllAsync();
    Task<bool> ExistsFlightNumberAndDepartureAsync(string flightNumber, DateTime departureTime, int? excludeFlightId = null);
    Task<Flight> AddAsync(Flight flight);
    Task UpdateAsync(Flight flight);
    Task SaveChangesAsync();
}

public class FlightRepository : IFlightRepository
{
    private readonly FlightDbContext _context;

    public FlightRepository(FlightDbContext context)
    {
        _context = context;
    }

    public async Task<Flight?> GetByIdAsync(int flightId)
    {
        return await _context.Flights
            .Include(f => f.Seats)
            .FirstOrDefaultAsync(f => f.FlightId == flightId);
    }

    public async Task<Flight?> GetWithSeatsByIdAsync(int flightId)
    {
        return await _context.Flights
            .Include(f => f.Seats)
            .FirstOrDefaultAsync(f => f.FlightId == flightId);
    }

    public async Task<List<Flight>> SearchFlightsAsync(string source, string destination, DateTime travelDate)
    {
        var startDate = travelDate.Date;
        var endDate = startDate.AddDays(1);

        return await _context.Flights
            .Include(f => f.Seats)
            .Where(f => f.IsActive &&
                        f.Source.ToLower() == source.ToLower() &&
                        f.Destination.ToLower() == destination.ToLower() &&
                        f.DepartureTime >= startDate &&
                        f.DepartureTime < endDate)
            .OrderBy(f => f.DepartureTime)
            .Take(50)
            .ToListAsync();
    }

    public async Task<List<Flight>> GetAllAsync()
    {
        return await _context.Flights
            .Include(f => f.Seats)
            .OrderByDescending(f => f.DepartureTime)
            .Take(100)
            .ToListAsync();
    }

    public async Task<bool> ExistsFlightNumberAndDepartureAsync(string flightNumber, DateTime departureTime, int? excludeFlightId = null)
    {
        var query = _context.Flights.Where(f => f.FlightNumber.ToLower() == flightNumber.ToLower() &&
                                                f.DepartureTime == departureTime);
        if (excludeFlightId.HasValue)
        {
            query = query.Where(f => f.FlightId != excludeFlightId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Flight> AddAsync(Flight flight)
    {
        await _context.Flights.AddAsync(flight);
        await _context.SaveChangesAsync();
        return flight;
    }

    public async Task UpdateAsync(Flight flight)
    {
        _context.Flights.Update(flight);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
