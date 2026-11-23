using Microsoft.EntityFrameworkCore;
using PaymentService.Entities;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payment");
            entity.HasKey(p => p.PaymentId);
            entity.Property(p => p.PaymentId).ValueGeneratedOnAdd();

            entity.Property(p => p.BookingId)
                .IsRequired();

            entity.Property(p => p.Amount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(p => p.PaymentMethod)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.TransactionId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(p => p.PaymentStatus)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(p => p.PaymentDate)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(p => p.BookingId);
            entity.HasIndex(p => p.TransactionId);
        });
    }
}
