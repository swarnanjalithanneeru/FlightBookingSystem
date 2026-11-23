using Microsoft.EntityFrameworkCore;
using UserService.Entities;

namespace UserService.Data;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");
            entity.HasKey(r => r.RoleId);
            entity.Property(r => r.RoleId).ValueGeneratedOnAdd();
            entity.Property(r => r.RoleName)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasData(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Customer" }
            );
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.UserId).ValueGeneratedOnAdd();

            entity.Property(u => u.FullName)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(u => u.Email)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(u => u.Email)
                .IsUnique();

            entity.Property(u => u.PasswordHash)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(u => u.PhoneNumber)
                .HasMaxLength(15);

            entity.Property(u => u.CreatedAt)
                .HasColumnType("datetime")
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Pre-seed an initial Administrator (Password: Admin@123456)
            entity.HasData(new User
            {
                UserId = 1,
                RoleId = 1,
                FullName = "System Administrator",
                Email = "admin@flightbooking.com",
                PasswordHash = "$2a$11$91/GfXbF15aK61z/i3M/z.M0c0Kx7b.R18e1EaJ5eFvj1mZ20wLfa", // BCrypt of Admin@123456
                PhoneNumber = "+1234567890",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
