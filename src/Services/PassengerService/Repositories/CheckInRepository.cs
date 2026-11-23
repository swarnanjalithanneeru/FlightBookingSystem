using Microsoft.EntityFrameworkCore;
using PassengerService.Data;
using PassengerService.Entities;

namespace PassengerService.Repositories;

public interface ICheckInRepository
{
    Task<CheckIn?> GetByIdAsync(int checkInId);
    Task<CheckIn?> GetByPassengerIdAsync(int passengerId);
    Task<bool> IsPassengerCheckedInAsync(int passengerId);
    Task<CheckIn> AddAsync(CheckIn checkIn);
    Task SaveChangesAsync();
}

public class CheckInRepository : ICheckInRepository
{
    private readonly PassengerDbContext _context;

    public CheckInRepository(PassengerDbContext context)
    {
        _context = context;
    }

    public async Task<CheckIn?> GetByIdAsync(int checkInId)
    {
        return await _context.CheckIns
            .Include(c => c.Passenger)
            .FirstOrDefaultAsync(c => c.CheckInId == checkInId);
    }

    public async Task<CheckIn?> GetByPassengerIdAsync(int passengerId)
    {
        return await _context.CheckIns
            .Include(c => c.Passenger)
            .FirstOrDefaultAsync(c => c.PassengerId == passengerId);
    }

    public async Task<bool> IsPassengerCheckedInAsync(int passengerId)
    {
        return await _context.CheckIns.AnyAsync(c => c.PassengerId == passengerId);
    }

    public async Task<CheckIn> AddAsync(CheckIn checkIn)
    {
        await _context.CheckIns.AddAsync(checkIn);
        await _context.SaveChangesAsync();
        return checkIn;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
