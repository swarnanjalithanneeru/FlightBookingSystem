using Microsoft.EntityFrameworkCore;
using PassengerService.Entities;

namespace PassengerService.Data;

public class PassengerDbContext : DbContext
{
    public PassengerDbContext(DbContextOptions<PassengerDbContext> options) : base(options)
    {
    }

    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Passenger>(entity =>
        {
            entity.ToTable("Passenger");
            entity.HasKey(p => p.PassengerId);
            entity.Property(p => p.PassengerId).ValueGeneratedOnAdd();

            entity.Property(p => p.BookingId)
                .IsRequired();

            entity.Property(p => p.PassengerName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(p => p.Age)
                .IsRequired();

            entity.Property(p => p.Gender)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(p => p.SeatNumber)
                .HasMaxLength(5)
                .IsRequired();

            entity.HasIndex(p => p.BookingId);

            entity.HasOne(p => p.CheckIn)
                .WithOne(c => c.Passenger)
                .HasForeignKey<CheckIn>(c => c.PassengerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.ToTable("CheckIn");
            entity.HasKey(c => c.CheckInId);
            entity.Property(c => c.CheckInId).ValueGeneratedOnAdd();

            entity.Property(c => c.BookingId)
                .IsRequired();

            entity.Property(c => c.PassengerId)
                .IsRequired();

            entity.Property(c => c.CheckInDate)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(c => c.CheckInStatus)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("CheckedIn");

            // 1:1 unique constraint on PassengerId for CheckIn
            entity.HasIndex(c => c.PassengerId)
                .IsUnique();

            entity.HasIndex(c => c.BookingId);
        });
    }
}
