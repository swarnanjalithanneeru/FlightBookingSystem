using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Entities;

namespace PaymentService.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int paymentId);
    Task<Payment?> GetByBookingIdAsync(int bookingId);
    Task<Payment?> GetByTransactionIdAsync(string transactionId);
    Task<Payment> AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task SaveChangesAsync();
}

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(int paymentId)
    {
        return await _context.Payments.FindAsync(paymentId);
    }

    public async Task<Payment?> GetByBookingIdAsync(int bookingId)
    {
        return await _context.Payments
            .OrderByDescending(p => p.PaymentDate)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);
    }

    public async Task<Payment?> GetByTransactionIdAsync(string transactionId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);
    }

    public async Task<Payment> AddAsync(Payment payment)
    {
        await _context.Payments.AddAsync(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
