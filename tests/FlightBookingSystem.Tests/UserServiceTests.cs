using FlightBookingSystem.Shared.Auth;
using FlightBookingSystem.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using UserService.Data;
using UserService.DTOs;
using UserService.Repositories;
using UserService.Services;

namespace FlightBookingSystem.Tests;

[TestFixture]
public class UserServiceTests
{
    private UserDbContext _dbContext = null!;
    private IUserRepository _userRepository = null!;
    private IRoleRepository _roleRepository = null!;
    private IPasswordHasher _passwordHasher = null!;
    private ITokenService _tokenService = null!;
    private IUserService _userService = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new UserDbContext(options);
        _dbContext.Database.EnsureCreated();

        _userRepository = new UserRepository(_dbContext);
        _roleRepository = new RoleRepository(_dbContext);
        _passwordHasher = new PasswordHasher();

        var jwtSettings = new JwtSettings
        {
            SecretKey = "SuperSecretKeyForTestingJwtTokenGeneration12345!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryInMinutes = 60
        };
        _tokenService = new TokenService(jwtSettings);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "AdminProvisioningSecret", "TestAdminSecret123!" }
            })
            .Build();

        _userService = new UserServiceImpl(
            _userRepository,
            _roleRepository,
            _passwordHasher,
            _tokenService,
            config
        );
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public async Task RegisterCustomer_WithValidDetails_ReturnsSuccessAndToken()
    {
        var dto = new RegisterCustomerDto
        {
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Password = "Password123!",
            PhoneNumber = "+1987654321"
        };

        var response = await _userService.RegisterCustomerAsync(dto);

        Assert.That(response.Success, Is.True);
        Assert.That(response.Data, Is.Not.Null);
        Assert.That(response.Data!.Email, Is.EqualTo("john.doe@example.com"));
        Assert.That(response.Data.Role, Is.EqualTo(RoleConstants.Customer));
        Assert.That(response.Data.Token, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task RegisterCustomer_WithDuplicateEmail_ReturnsFail()
    {
        var dto = new RegisterCustomerDto
        {
            FullName = "First Customer",
            Email = "duplicate@example.com",
            Password = "Password123!"
        };

        var firstResult = await _userService.RegisterCustomerAsync(dto);
        Assert.That(firstResult.Success, Is.True);

        var secondResult = await _userService.RegisterCustomerAsync(dto);
        Assert.That(secondResult.Success, Is.False);
        Assert.That(secondResult.Message, Does.Contain("already exists"));
    }

    [Test]
    public async Task Login_WithValidCredentials_ReturnsSuccessAndJwtToken()
    {
        var registerDto = new RegisterCustomerDto
        {
            FullName = "Alice Smith",
            Email = "alice@example.com",
            Password = "SecretPassword123!"
        };
        await _userService.RegisterCustomerAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = "alice@example.com",
            Password = "SecretPassword123!"
        };

        var loginResult = await _userService.LoginAsync(loginDto);

        Assert.That(loginResult.Success, Is.True);
        Assert.That(loginResult.Data, Is.Not.Null);
        Assert.That(loginResult.Data!.Token, Is.Not.Empty);
        Assert.That(loginResult.Data.Role, Is.EqualTo(RoleConstants.Customer));
    }

    [Test]
    public async Task Login_WithInvalidPassword_ReturnsFail()
    {
        var registerDto = new RegisterCustomerDto
        {
            FullName = "Bob Brown",
            Email = "bob@example.com",
            Password = "CorrectPassword123!"
        };
        await _userService.RegisterCustomerAsync(registerDto);

        var loginDto = new LoginDto
        {
            Email = "bob@example.com",
            Password = "WrongPassword999!"
        };

        var loginResult = await _userService.LoginAsync(loginDto);

        Assert.That(loginResult.Success, Is.False);
        Assert.That(loginResult.Message, Does.Contain("Invalid email or password"));
    }

    [Test]
    public async Task ProvisionAdmin_WithValidSecret_CreatesAdminAccount()
    {
        var dto = new ProvisionAdminDto
        {
            FullName = "Admin Superuser",
            Email = "superadmin@flightbooking.com",
            Password = "SuperAdminPassword123!",
            AdminSecret = "TestAdminSecret123!"
        };

        var result = await _userService.ProvisionAdminAsync(dto);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!.Role, Is.EqualTo(RoleConstants.Admin));
        Assert.That(result.Data.Token, Is.Not.Empty);
    }

    [Test]
    public async Task ProvisionAdmin_WithInvalidSecret_Fails()
    {
        var dto = new ProvisionAdminDto
        {
            FullName = "Hacker",
            Email = "hacker@test.com",
            Password = "Password123!",
            AdminSecret = "WrongSecretKey!"
        };

        var result = await _userService.ProvisionAdminAsync(dto);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Unauthorized"));
    }

    [Test]
    public async Task GetProfile_ForExistingUser_ReturnsProfile()
    {
        var registerDto = new RegisterCustomerDto
        {
            FullName = "Profile User",
            Email = "profile@example.com",
            Password = "Password123!",
            PhoneNumber = "+9876543210"
        };
        var regResult = await _userService.RegisterCustomerAsync(registerDto);

        var profileResult = await _userService.GetProfileAsync(regResult.Data!.UserId);

        Assert.That(profileResult.Success, Is.True);
        Assert.That(profileResult.Data, Is.Not.Null);
        Assert.That(profileResult.Data!.Email, Is.EqualTo("profile@example.com"));
        Assert.That(profileResult.Data.FullName, Is.EqualTo("Profile User"));
    }
}
