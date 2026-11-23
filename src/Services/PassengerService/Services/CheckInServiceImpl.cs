using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using PassengerService.Clients;
using PassengerService.DTOs;
using PassengerService.Entities;
using PassengerService.Repositories;

namespace PassengerService.Services;

public interface ICheckInService
{
    Task<ApiResponse<CheckInResponseDto>> CreateCheckInAsync(CreateCheckInRequestDto dto);
    Task<ApiResponse<CheckInResponseDto>> GetCheckInByIdAsync(int checkInId);
    Task<ApiResponse<CheckInResponseDto>> GetCheckInByPassengerIdAsync(int passengerId);
}

public class CheckInServiceImpl : ICheckInService
{
    private readonly ICheckInRepository _checkInRepository;
    private readonly IPassengerRepository _passengerRepository;
    private readonly IBookingServiceClient _bookingServiceClient;
    private readonly ILogger<CheckInServiceImpl> _logger;

    public CheckInServiceImpl(
        ICheckInRepository checkInRepository,
        IPassengerRepository passengerRepository,
        IBookingServiceClient bookingServiceClient,
        ILogger<CheckInServiceImpl> logger)
    {
        _checkInRepository = checkInRepository;
        _passengerRepository = passengerRepository;
        _bookingServiceClient = bookingServiceClient;
        _logger = logger;
    }

    public async Task<ApiResponse<CheckInResponseDto>> CreateCheckInAsync(CreateCheckInRequestDto dto)
    {
        // 1. Validate passenger exists
        var passenger = await _passengerRepository.GetByIdAsync(dto.PassengerId);
        if (passenger == null)
        {
            return ApiResponse<CheckInResponseDto>.Fail($"Passenger with ID {dto.PassengerId} not found.");
        }

        // 2. Check for duplicate check-in
        if (await _checkInRepository.IsPassengerCheckedInAsync(dto.PassengerId))
        {
            return ApiResponse<CheckInResponseDto>.Fail($"Passenger {passenger.PassengerName} is already checked in.");
        }

        // 3. Validate booking with BookingService via PNR
        var booking = await _bookingServiceClient.GetBookingByPnrAsync(dto.PNR.Trim());
        if (booking == null)
        {
            return ApiResponse<CheckInResponseDto>.Fail($"No booking found for PNR: {dto.PNR}.");
        }

        if (!booking.BookingStatus.Equals(BookingStatusConstants.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<CheckInResponseDto>.Fail($"Check-in is only allowed for Confirmed bookings. Current booking status: {booking.BookingStatus}.");
        }

        // 4. Verify passenger belongs to the booking associated with this PNR
        if (passenger.BookingId != booking.BookingId)
        {
            return ApiResponse<CheckInResponseDto>.Fail("Passenger does not belong to the booking associated with this PNR.");
        }

        // 5. Create CheckIn record, retaining the pre-selected seat
        var checkIn = new CheckIn
        {
            BookingId = booking.BookingId,
            PassengerId = passenger.PassengerId,
            CheckInDate = DateTime.UtcNow,
            CheckInStatus = CheckInStatusConstants.CheckedIn
        };

        var created = await _checkInRepository.AddAsync(checkIn);

        var response = new CheckInResponseDto
        {
            CheckInId = created.CheckInId,
            BookingId = created.BookingId,
            PassengerId = created.PassengerId,
            PassengerName = passenger.PassengerName,
            SeatNumber = passenger.SeatNumber, // Retained selected seat
            CheckInDate = created.CheckInDate,
            CheckInStatus = created.CheckInStatus,
            Message = $"Check-in successful for {passenger.PassengerName}. Seat: {passenger.SeatNumber}"
        };

        return ApiResponse<CheckInResponseDto>.Ok(response, "Check-in successful.");
    }

    public async Task<ApiResponse<CheckInResponseDto>> GetCheckInByIdAsync(int checkInId)
    {
        var checkIn = await _checkInRepository.GetByIdAsync(checkInId);
        if (checkIn == null)
        {
            return ApiResponse<CheckInResponseDto>.Fail($"Check-in record with ID {checkInId} not found.");
        }

        var response = new CheckInResponseDto
        {
            CheckInId = checkIn.CheckInId,
            BookingId = checkIn.BookingId,
            PassengerId = checkIn.PassengerId,
            PassengerName = checkIn.Passenger?.PassengerName ?? string.Empty,
            SeatNumber = checkIn.Passenger?.SeatNumber ?? string.Empty,
            CheckInDate = checkIn.CheckInDate,
            CheckInStatus = checkIn.CheckInStatus
        };

        return ApiResponse<CheckInResponseDto>.Ok(response);
    }

    public async Task<ApiResponse<CheckInResponseDto>> GetCheckInByPassengerIdAsync(int passengerId)
    {
        var checkIn = await _checkInRepository.GetByPassengerIdAsync(passengerId);
        if (checkIn == null)
        {
            return ApiResponse<CheckInResponseDto>.Fail($"Check-in record for passenger ID {passengerId} not found.");
        }

        var response = new CheckInResponseDto
        {
            CheckInId = checkIn.CheckInId,
            BookingId = checkIn.BookingId,
            PassengerId = checkIn.PassengerId,
            PassengerName = checkIn.Passenger?.PassengerName ?? string.Empty,
            SeatNumber = checkIn.Passenger?.SeatNumber ?? string.Empty,
            CheckInDate = checkIn.CheckInDate,
            CheckInStatus = checkIn.CheckInStatus
        };

        return ApiResponse<CheckInResponseDto>.Ok(response);
    }
}
