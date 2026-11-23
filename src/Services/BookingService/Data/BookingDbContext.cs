using BookingService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Data;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options)
    {
    }

    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("Booking");
            entity.HasKey(b => b.BookingId);
            entity.Property(b => b.BookingId).ValueGeneratedOnAdd();

            entity.Property(b => b.UserId)
                .IsRequired();

            entity.Property(b => b.FlightId)
                .IsRequired();

            entity.Property(b => b.BookingDate)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(b => b.TotalAmount)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(b => b.BookingStatus)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("Pending");

            entity.Property(b => b.PNR)
                .HasMaxLength(10);

            entity.HasIndex(b => b.UserId);
            entity.HasIndex(b => b.FlightId);
            entity.HasIndex(b => b.PNR);
        });
    }
}
