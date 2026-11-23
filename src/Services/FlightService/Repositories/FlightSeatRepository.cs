using FlightBookingSystem.Shared.Constants;
using FlightService.Data;
using FlightService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightService.Repositories;

public interface IFlightSeatRepository
{
    Task<List<FlightSeat>> GetSeatsByFlightIdAsync(int flightId);
    Task<List<FlightSeat>> GetSeatsByNumbersAsync(int flightId, IEnumerable<string> seatNumbers);
    Task<bool> HoldSeatsAtomicAsync(int flightId, List<string> seatNumbers);
    Task<bool> ConfirmSeatsAtomicAsync(int flightId, List<string> seatNumbers);
    Task<bool> ReleaseSeatsAtomicAsync(int flightId, List<string> seatNumbers);
    Task AddSeatsAsync(IEnumerable<FlightSeat> seats);
    Task SaveChangesAsync();
}

public class FlightSeatRepository : IFlightSeatRepository
{
    private readonly FlightDbContext _context;

    public FlightSeatRepository(FlightDbContext context)
    {
        _context = context;
    }

    public async Task<List<FlightSeat>> GetSeatsByFlightIdAsync(int flightId)
    {
        return await _context.FlightSeats
            .Where(s => s.FlightId == flightId)
            .OrderBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task<List<FlightSeat>> GetSeatsByNumbersAsync(int flightId, IEnumerable<string> seatNumbers)
    {
        var upperSeats = seatNumbers.Select(s => s.ToUpperInvariant()).ToList();
        return await _context.FlightSeats
            .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
            .ToListAsync();
    }

    public async Task<bool> HoldSeatsAtomicAsync(int flightId, List<string> seatNumbers)
    {
        var upperSeats = seatNumbers.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
                try
                {
                    var seats = await _context.FlightSeats
                        .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
                        .ToListAsync();

                    if (seats.Count != upperSeats.Count)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    foreach (var seat in seats)
                    {
                        if (!seat.IsAvailable || seat.SeatStatus != SeatStatusConstants.Available)
                        {
                            await transaction.RollbackAsync();
                            return false;
                        }
                    }

                    foreach (var seat in seats)
                    {
                        seat.SeatStatus = SeatStatusConstants.Held;
                        seat.IsAvailable = false;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        else
        {
            // For in-memory testing provider
            var seats = await _context.FlightSeats
                .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
                .ToListAsync();

            if (seats.Count != upperSeats.Count) return false;

            foreach (var seat in seats)
            {
                if (!seat.IsAvailable || seat.SeatStatus != SeatStatusConstants.Available)
                {
                    return false;
                }
            }

            foreach (var seat in seats)
            {
                seat.SeatStatus = SeatStatusConstants.Held;
                seat.IsAvailable = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public async Task<bool> ConfirmSeatsAtomicAsync(int flightId, List<string> seatNumbers)
    {
        var upperSeats = seatNumbers.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
                try
                {
                    var seats = await _context.FlightSeats
                        .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
                        .ToListAsync();

                    if (seats.Count != upperSeats.Count)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    foreach (var seat in seats)
                    {
                        seat.SeatStatus = SeatStatusConstants.Booked;
                        seat.IsAvailable = false;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        else
        {
            var seats = await _context.FlightSeats
                .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
                .ToListAsync();

            if (seats.Count != upperSeats.Count) return false;

            foreach (var seat in seats)
            {
                seat.SeatStatus = SeatStatusConstants.Booked;
                seat.IsAvailable = false;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public async Task<bool> ReleaseSeatsAtomicAsync(int flightId, List<string> seatNumbers)
    {
        var upperSeats = seatNumbers.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();

        if (_context.Database.IsRelational())
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
                try
                {
                    var seats = await _context.FlightSeats
                        .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
                        .ToListAsync();

                    foreach (var seat in seats)
                    {
                        seat.SeatStatus = SeatStatusConstants.Available;
                        seat.IsAvailable = true;
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
        }
        else
        {
            var seats = await _context.FlightSeats
                .Where(s => s.FlightId == flightId && upperSeats.Contains(s.SeatNumber.ToUpper()))
                .ToListAsync();

            foreach (var seat in seats)
            {
                seat.SeatStatus = SeatStatusConstants.Available;
                seat.IsAvailable = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }

    public async Task AddSeatsAsync(IEnumerable<FlightSeat> seats)
    {
        await _context.FlightSeats.AddRangeAsync(seats);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
