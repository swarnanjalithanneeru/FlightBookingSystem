using FlightService.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlightService.Data;

public class FlightDbContext : DbContext
{
    public FlightDbContext(DbContextOptions<FlightDbContext> options) : base(options)
    {
    }

    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<FlightSeat> FlightSeats => Set<FlightSeat>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Flight>(entity =>
        {
            entity.ToTable("Flight");
            entity.HasKey(f => f.FlightId);
            entity.Property(f => f.FlightId).ValueGeneratedOnAdd();

            entity.Property(f => f.FlightNumber)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(f => f.Airline)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(f => f.Source)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(f => f.Destination)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(f => f.DepartureTime)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(f => f.ArrivalTime)
                .HasColumnType("datetime")
                .IsRequired();

            entity.Property(f => f.TotalSeats)
                .IsRequired();

            entity.Property(f => f.BaseFare)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(f => f.IsActive)
                .HasDefaultValue(true);

            // Indexes
            entity.HasIndex(f => new { f.FlightNumber, f.DepartureTime })
                .IsUnique();

            entity.HasIndex(f => new { f.Source, f.Destination, f.DepartureTime });

            // Seed initial sample flights
            var baseDate = DateTime.UtcNow.Date.AddDays(1);
            entity.HasData(
                new Flight
                {
                    FlightId = 1,
                    FlightNumber = "AI-101",
                    Airline = "Air India",
                    Source = "DEL",
                    Destination = "BOM",
                    DepartureTime = baseDate.AddHours(8),
                    ArrivalTime = baseDate.AddHours(10).AddMinutes(15),
                    TotalSeats = 6,
                    BaseFare = 4500.00m,
                    IsActive = true
                },
                new Flight
                {
                    FlightId = 2,
                    FlightNumber = "6E-202",
                    Airline = "IndiGo",
                    Source = "BOM",
                    Destination = "BLR",
                    DepartureTime = baseDate.AddHours(12),
                    ArrivalTime = baseDate.AddHours(13).AddMinutes(45),
                    TotalSeats = 6,
                    BaseFare = 3200.00m,
                    IsActive = true
                },
                new Flight
                {
                    FlightId = 3,
                    FlightNumber = "UK-303",
                    Airline = "Vistara",
                    Source = "DEL",
                    Destination = "DXB",
                    DepartureTime = baseDate.AddHours(18),
                    ArrivalTime = baseDate.AddHours(21),
                    TotalSeats = 6,
                    BaseFare = 12000.00m,
                    IsActive = true
                }
            );
        });

        modelBuilder.Entity<FlightSeat>(entity =>
        {
            entity.ToTable("FlightSeat");
            entity.HasKey(s => s.SeatId);
            entity.Property(s => s.SeatId).ValueGeneratedOnAdd();

            entity.Property(s => s.SeatNumber)
                .HasMaxLength(5)
                .IsRequired();

            entity.Property(s => s.SeatClass)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(s => s.SeatStatus)
                .HasMaxLength(20)
                .IsRequired()
                .HasDefaultValue("Available");

            entity.Property(s => s.IsAvailable)
                .HasDefaultValue(true);

            entity.HasOne(s => s.Flight)
                .WithMany(f => f.Seats)
                .HasForeignKey(s => s.FlightId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.FlightId, s.SeatNumber })
                .IsUnique();

            // Seed seats for Flight 1, 2, 3
            var seats = new List<FlightSeat>();
            int seatIdCounter = 1;
            string[] seatNumbers = { "1A", "1B", "2A", "2B", "3A", "3B" };
            for (int fId = 1; fId <= 3; fId++)
            {
                for (int i = 0; i < seatNumbers.Length; i++)
                {
                    seats.Add(new FlightSeat
                    {
                        SeatId = seatIdCounter++,
                        FlightId = fId,
                        SeatNumber = seatNumbers[i],
                        SeatClass = i < 2 ? "Business" : "Economy",
                        IsAvailable = true,
                        SeatStatus = "Available"
                    });
                }
            }
            entity.HasData(seats);
        });
    }
}
