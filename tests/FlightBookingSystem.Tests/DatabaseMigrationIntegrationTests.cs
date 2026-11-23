using BookingService.Data;
using FlightService.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using PassengerService.Data;
using PaymentService.Data;
using UserService.Data;

namespace FlightBookingSystem.Tests;

[TestFixture]
[Category("DatabaseIntegration")]
public class DatabaseMigrationIntegrationTests
{
    private const string BaseConnectionString = "Data Source=(localdb)\\MSSQLLocalDB;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name=\"FlightBookingSystem-Test\";Command Timeout=30;";

    [Test]
    [Explicit("Run explicitly when LocalDB is active to create all 5 databases via EF Core Migrations")]
    public async Task MigrateAllDatabasesToLocalDb_Succeeds()
    {
        // 1. UserDB
        var userDbOptions = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer($"{BaseConnectionString}Initial Catalog=FlightBooking_UserDB;")
            .Options;
        using (var userDb = new UserDbContext(userDbOptions))
        {
            await userDb.Database.EnsureDeletedAsync();
            await userDb.Database.MigrateAsync();
            var roles = await userDb.Roles.ToListAsync();
            Assert.That(roles.Count, Is.GreaterThanOrEqualTo(2));
        }

        // 2. FlightDB
        var flightDbOptions = new DbContextOptionsBuilder<FlightDbContext>()
            .UseSqlServer($"{BaseConnectionString}Initial Catalog=FlightBooking_FlightDB;")
            .Options;
        using (var flightDb = new FlightDbContext(flightDbOptions))
        {
            await flightDb.Database.EnsureDeletedAsync();
            await flightDb.Database.MigrateAsync();
            var flights = await flightDb.Flights.ToListAsync();
            Assert.That(flights.Count, Is.GreaterThanOrEqualTo(3));
        }

        // 3. BookingDB
        var bookingDbOptions = new DbContextOptionsBuilder<BookingDbContext>()
            .UseSqlServer($"{BaseConnectionString}Initial Catalog=FlightBooking_BookingDB;")
            .Options;
        using (var bookingDb = new BookingDbContext(bookingDbOptions))
        {
            await bookingDb.Database.EnsureDeletedAsync();
            await bookingDb.Database.MigrateAsync();
            Assert.That(await bookingDb.Database.CanConnectAsync(), Is.True);
        }

        // 4. PassengerDB
        var passengerDbOptions = new DbContextOptionsBuilder<PassengerDbContext>()
            .UseSqlServer($"{BaseConnectionString}Initial Catalog=FlightBooking_PassengerDB;")
            .Options;
        using (var passengerDb = new PassengerDbContext(passengerDbOptions))
        {
            await passengerDb.Database.EnsureDeletedAsync();
            await passengerDb.Database.MigrateAsync();
            Assert.That(await passengerDb.Database.CanConnectAsync(), Is.True);
        }

        // 5. PaymentDB
        var paymentDbOptions = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseSqlServer($"{BaseConnectionString}Initial Catalog=FlightBooking_PaymentDB;")
            .Options;
        using (var paymentDb = new PaymentDbContext(paymentDbOptions))
        {
            await paymentDb.Database.EnsureDeletedAsync();
            await paymentDb.Database.MigrateAsync();
            Assert.That(await paymentDb.Database.CanConnectAsync(), Is.True);
        }
    }
}
