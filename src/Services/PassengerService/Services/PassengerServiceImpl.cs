using FlightBookingSystem.Shared.Models;
using PassengerService.DTOs;
using PassengerService.Entities;
using PassengerService.Repositories;

namespace PassengerService.Services;

public interface IPassengerService
{
    Task<ApiResponse<PassengerDto>> GetPassengerByIdAsync(int passengerId);
    Task<ApiResponse<List<PassengerDto>>> GetPassengersByBookingIdAsync(int bookingId);
    Task<ApiResponse<List<PassengerDto>>> CreatePassengersBulkAsync(CreatePassengersBulkRequestDto request);
}

public class PassengerServiceImpl : IPassengerService
{
    private readonly IPassengerRepository _passengerRepository;

    public PassengerServiceImpl(IPassengerRepository passengerRepository)
    {
        _passengerRepository = passengerRepository;
    }

    public async Task<ApiResponse<PassengerDto>> GetPassengerByIdAsync(int passengerId)
    {
        var passenger = await _passengerRepository.GetByIdAsync(passengerId);
        if (passenger == null)
        {
            return ApiResponse<PassengerDto>.Fail($"Passenger with ID {passengerId} not found.");
        }

        var dto = new PassengerDto
        {
            PassengerId = passenger.PassengerId,
            BookingId = passenger.BookingId,
            PassengerName = passenger.PassengerName,
            Age = passenger.Age,
            Gender = passenger.Gender,
            SeatNumber = passenger.SeatNumber,
            IsCheckedIn = passenger.CheckIn != null,
            CheckInId = passenger.CheckIn?.CheckInId
        };

        return ApiResponse<PassengerDto>.Ok(dto);
    }

    public async Task<ApiResponse<List<PassengerDto>>> GetPassengersByBookingIdAsync(int bookingId)
    {
        var passengers = await _passengerRepository.GetByBookingIdAsync(bookingId);
        var dtos = passengers.Select(p => new PassengerDto
        {
            PassengerId = p.PassengerId,
            BookingId = p.BookingId,
            PassengerName = p.PassengerName,
            Age = p.Age,
            Gender = p.Gender,
            SeatNumber = p.SeatNumber,
            IsCheckedIn = p.CheckIn != null,
            CheckInId = p.CheckIn?.CheckInId
        }).ToList();

        return ApiResponse<List<PassengerDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<List<PassengerDto>>> CreatePassengersBulkAsync(CreatePassengersBulkRequestDto request)
    {
        var entities = request.Passengers.Select(p => new Passenger
        {
            BookingId = request.BookingId,
            PassengerName = p.PassengerName.Trim(),
            Age = p.Age,
            Gender = p.Gender.Trim(),
            SeatNumber = p.SeatNumber.Trim().ToUpperInvariant()
        }).ToList();

        var created = await _passengerRepository.AddBulkAsync(entities);
        var dtos = created.Select(p => new PassengerDto
        {
            PassengerId = p.PassengerId,
            BookingId = p.BookingId,
            PassengerName = p.PassengerName,
            Age = p.Age,
            Gender = p.Gender,
            SeatNumber = p.SeatNumber,
            IsCheckedIn = false
        }).ToList();

        return ApiResponse<List<PassengerDto>>.Ok(dtos, "Passengers added successfully.");
    }
}
