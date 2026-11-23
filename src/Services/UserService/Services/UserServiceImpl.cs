using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using UserService.DTOs;
using UserService.Entities;
using UserService.Repositories;

namespace UserService.Services;

public interface IUserService
{
    Task<ApiResponse<AuthResponseDto>> RegisterCustomerAsync(RegisterCustomerDto dto);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId);
    Task<ApiResponse<AuthResponseDto>> ProvisionAdminAsync(ProvisionAdminDto dto);
}

public class UserServiceImpl : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public UserServiceImpl(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterCustomerAsync(RegisterCustomerDto dto)
    {
        if (await _userRepository.ExistsByEmailAsync(dto.Email))
        {
            return ApiResponse<AuthResponseDto>.Fail("A user with this email already exists.");
        }

        var customerRole = await _roleRepository.GetByNameAsync(RoleConstants.Customer);
        if (customerRole == null)
        {
            return ApiResponse<AuthResponseDto>.Fail("Customer role is not configured.");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            RoleId = customerRole.RoleId,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user);
        createdUser.Role = customerRole;

        var (token, expiresAt) = _tokenService.GenerateToken(createdUser, customerRole.RoleName);

        var response = new AuthResponseDto
        {
            UserId = createdUser.UserId,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            Role = customerRole.RoleName,
            Token = token,
            ExpiresAt = expiresAt
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Registration successful.");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userRepository.GetByEmailAsync(dto.Email.Trim());
        if (user == null || !_passwordHasher.VerifyPassword(dto.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponseDto>.Fail("Invalid email or password.");
        }

        var roleName = user.Role?.RoleName ?? RoleConstants.Customer;
        var (token, expiresAt) = _tokenService.GenerateToken(user, roleName);

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = roleName,
            Token = token,
            ExpiresAt = expiresAt
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Login successful.");
    }

    public async Task<ApiResponse<UserProfileDto>> GetProfileAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse<UserProfileDto>.Fail("User not found.");
        }

        var profile = new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.RoleName ?? RoleConstants.Customer,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt
        };

        return ApiResponse<UserProfileDto>.Ok(profile);
    }

    public async Task<ApiResponse<AuthResponseDto>> ProvisionAdminAsync(ProvisionAdminDto dto)
    {
        var expectedSecret = _configuration["AdminProvisioningSecret"] ?? "FlightBookingAdminProvisioningSecretKey2026!";
        if (dto.AdminSecret != expectedSecret)
        {
            return ApiResponse<AuthResponseDto>.Fail("Unauthorized: Invalid admin provisioning secret.");
        }

        if (await _userRepository.ExistsByEmailAsync(dto.Email))
        {
            return ApiResponse<AuthResponseDto>.Fail("A user with this email already exists.");
        }

        var adminRole = await _roleRepository.GetByNameAsync(RoleConstants.Admin);
        if (adminRole == null)
        {
            return ApiResponse<AuthResponseDto>.Fail("Admin role is not configured.");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PhoneNumber = dto.PhoneNumber?.Trim() ?? string.Empty,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            RoleId = adminRole.RoleId,
            CreatedAt = DateTime.UtcNow
        };

        var createdUser = await _userRepository.AddAsync(user);
        createdUser.Role = adminRole;

        var (token, expiresAt) = _tokenService.GenerateToken(createdUser, adminRole.RoleName);

        var response = new AuthResponseDto
        {
            UserId = createdUser.UserId,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            Role = adminRole.RoleName,
            Token = token,
            ExpiresAt = expiresAt
        };

        return ApiResponse<AuthResponseDto>.Ok(response, "Admin account provisioned successfully.");
    }
}
