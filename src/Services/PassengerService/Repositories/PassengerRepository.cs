using Microsoft.EntityFrameworkCore;
using PassengerService.Data;
using PassengerService.Entities;

namespace PassengerService.Repositories;

public interface IPassengerRepository
{
    Task<Passenger?> GetByIdAsync(int passengerId);
    Task<List<Passenger>> GetByBookingIdAsync(int bookingId);
    Task<List<Passenger>> AddBulkAsync(IEnumerable<Passenger> passengers);
    Task SaveChangesAsync();
}

public class PassengerRepository : IPassengerRepository
{
    private readonly PassengerDbContext _context;

    public PassengerRepository(PassengerDbContext context)
    {
        _context = context;
    }

    public async Task<Passenger?> GetByIdAsync(int passengerId)
    {
        return await _context.Passengers
            .Include(p => p.CheckIn)
            .FirstOrDefaultAsync(p => p.PassengerId == passengerId);
    }

    public async Task<List<Passenger>> GetByBookingIdAsync(int bookingId)
    {
        return await _context.Passengers
            .Include(p => p.CheckIn)
            .Where(p => p.BookingId == bookingId)
            .ToListAsync();
    }

    public async Task<List<Passenger>> AddBulkAsync(IEnumerable<Passenger> passengers)
    {
        var list = passengers.ToList();
        await _context.Passengers.AddRangeAsync(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
