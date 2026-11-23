using BookingService.Data;
using BookingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Repositories;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(int bookingId);
    Task<Booking?> GetByPnrAsync(string pnr);
    Task<List<Booking>> GetByUserIdAsync(int userId);
    Task<List<Booking>> GetByFlightIdAsync(int flightId);
    Task<List<Booking>> GetAllAsync();
    Task<bool> ExistsPnrAsync(string pnr);
    Task<Booking> AddAsync(Booking booking);
    Task UpdateAsync(Booking booking);
    Task SaveChangesAsync();
}

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(int bookingId)
    {
        return await _context.Bookings.FindAsync(bookingId);
    }

    public async Task<Booking?> GetByPnrAsync(string pnr)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.PNR != null && b.PNR.ToUpper() == pnr.ToUpper());
    }

    public async Task<List<Booking>> GetByUserIdAsync(int userId)
    {
        return await _context.Bookings
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetByFlightIdAsync(int flightId)
    {
        return await _context.Bookings
            .Where(b => b.FlightId == flightId)
            .OrderByDescending(b => b.BookingDate)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetAllAsync()
    {
        return await _context.Bookings
            .OrderByDescending(b => b.BookingDate)
            .Take(100)
            .ToListAsync();
    }

    public async Task<bool> ExistsPnrAsync(string pnr)
    {
        return await _context.Bookings.AnyAsync(b => b.PNR != null && b.PNR.ToUpper() == pnr.ToUpper());
    }

    public async Task<Booking> AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
