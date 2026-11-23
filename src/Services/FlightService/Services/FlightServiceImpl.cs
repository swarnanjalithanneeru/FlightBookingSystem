using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using FlightService.DTOs;
using FlightService.Entities;
using FlightService.Repositories;

namespace FlightService.Services;

public interface IFlightService
{
    Task<ApiResponse<List<FlightDto>>> SearchFlightsAsync(FlightSearchRequestDto request);
    Task<ApiResponse<List<FlightDto>>> GetAllFlightsAsync();
    Task<ApiResponse<FlightDto>> GetFlightByIdAsync(int flightId);
    Task<ApiResponse<SeatMapResponseDto>> GetSeatMapAsync(int flightId);
    Task<ApiResponse<FlightDto>> CreateFlightAsync(CreateFlightDto dto);
    Task<ApiResponse<FlightDto>> UpdateFlightAsync(int flightId, UpdateFlightDto dto);
    Task<ApiResponse<FlightDto>> DeactivateFlightAsync(int flightId);
    Task<ApiResponse<SeatOperationResponseDto>> HoldSeatsAsync(int flightId, HoldSeatsRequestDto dto);
    Task<ApiResponse<SeatOperationResponseDto>> ConfirmSeatsAsync(int flightId, ConfirmSeatsRequestDto dto);
    Task<ApiResponse<SeatOperationResponseDto>> ReleaseSeatsAsync(int flightId, ReleaseSeatsRequestDto dto);
}

public class FlightServiceImpl : IFlightService
{
    private readonly IFlightRepository _flightRepository;
    private readonly IFlightSeatRepository _seatRepository;

    public FlightServiceImpl(IFlightRepository flightRepository, IFlightSeatRepository seatRepository)
    {
        _flightRepository = flightRepository;
        _seatRepository = seatRepository;
    }

    public async Task<ApiResponse<List<FlightDto>>> SearchFlightsAsync(FlightSearchRequestDto request)
    {
        if (request.Source.Trim().Equals(request.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<List<FlightDto>>.Fail("Source and destination must differ.");
        }

        var flights = await _flightRepository.SearchFlightsAsync(request.Source.Trim(), request.Destination.Trim(), request.TravelDate);
        var dtos = flights.Select(MapToFlightDto).ToList();
        return ApiResponse<List<FlightDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<List<FlightDto>>> GetAllFlightsAsync()
    {
        var flights = await _flightRepository.GetAllAsync();
        var dtos = flights.Select(MapToFlightDto).ToList();
        return ApiResponse<List<FlightDto>>.Ok(dtos);
    }

    public async Task<ApiResponse<FlightDto>> GetFlightByIdAsync(int flightId)
    {
        var flight = await _flightRepository.GetByIdAsync(flightId);
        if (flight == null)
        {
            return ApiResponse<FlightDto>.Fail($"Flight with ID {flightId} not found.");
        }

        return ApiResponse<FlightDto>.Ok(MapToFlightDto(flight));
    }

    public async Task<ApiResponse<SeatMapResponseDto>> GetSeatMapAsync(int flightId)
    {
        var flight = await _flightRepository.GetByIdAsync(flightId);
        if (flight == null)
        {
            return ApiResponse<SeatMapResponseDto>.Fail($"Flight with ID {flightId} not found.");
        }

        var seats = await _seatRepository.GetSeatsByFlightIdAsync(flightId);
        var response = new SeatMapResponseDto
        {
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            Seats = seats.Select(s => new FlightSeatDto
            {
                SeatId = s.SeatId,
                FlightId = s.FlightId,
                SeatNumber = s.SeatNumber,
                SeatClass = s.SeatClass,
                IsAvailable = s.IsAvailable,
                SeatStatus = s.SeatStatus
            }).ToList()
        };

        return ApiResponse<SeatMapResponseDto>.Ok(response);
    }

    public async Task<ApiResponse<FlightDto>> CreateFlightAsync(CreateFlightDto dto)
    {
        if (dto.Source.Trim().Equals(dto.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<FlightDto>.Fail("Source and destination must differ.");
        }

        if (dto.ArrivalTime <= dto.DepartureTime)
        {
            return ApiResponse<FlightDto>.Fail("Arrival time must be later than departure time.");
        }

        if (dto.TotalSeats <= 0)
        {
            return ApiResponse<FlightDto>.Fail("Total capacity must be positive.");
        }

        if (await _flightRepository.ExistsFlightNumberAndDepartureAsync(dto.FlightNumber.Trim(), dto.DepartureTime))
        {
            return ApiResponse<FlightDto>.Fail($"A flight with number {dto.FlightNumber} departing at {dto.DepartureTime:s} already exists.");
        }

        var flight = new Flight
        {
            FlightNumber = dto.FlightNumber.Trim().ToUpperInvariant(),
            Airline = dto.Airline.Trim(),
            Source = dto.Source.Trim().ToUpperInvariant(),
            Destination = dto.Destination.Trim().ToUpperInvariant(),
            DepartureTime = dto.DepartureTime,
            ArrivalTime = dto.ArrivalTime,
            TotalSeats = dto.TotalSeats,
            BaseFare = dto.BaseFare,
            IsActive = true
        };

        var createdFlight = await _flightRepository.AddAsync(flight);

        // Generate Seat layout
        var seatsToCreate = new List<FlightSeat>();
        if (dto.CustomSeats != null && dto.CustomSeats.Any())
        {
            foreach (var seatNum in dto.CustomSeats)
            {
                seatsToCreate.Add(new FlightSeat
                {
                    FlightId = createdFlight.FlightId,
                    SeatNumber = seatNum.Trim().ToUpperInvariant(),
                    SeatClass = "Economy",
                    IsAvailable = true,
                    SeatStatus = SeatStatusConstants.Available
                });
            }
        }
        else
        {
            // Auto generate seat layout e.g. 1A, 1B, 1C, 1D, 2A, 2B...
            char[] seatLetters = { 'A', 'B', 'C', 'D', 'E', 'F' };
            int row = 1;
            int letterIdx = 0;
            for (int i = 0; i < dto.TotalSeats; i++)
            {
                string seatNum = $"{row}{seatLetters[letterIdx]}";
                seatsToCreate.Add(new FlightSeat
                {
                    FlightId = createdFlight.FlightId,
                    SeatNumber = seatNum,
                    SeatClass = row <= 2 ? "Business" : "Economy",
                    IsAvailable = true,
                    SeatStatus = SeatStatusConstants.Available
                });

                letterIdx++;
                if (letterIdx >= seatLetters.Length)
                {
                    letterIdx = 0;
                    row++;
                }
            }
        }

        await _seatRepository.AddSeatsAsync(seatsToCreate);

        createdFlight.Seats = seatsToCreate;
        return ApiResponse<FlightDto>.Ok(MapToFlightDto(createdFlight), "Flight created successfully.");
    }

    public async Task<ApiResponse<FlightDto>> UpdateFlightAsync(int flightId, UpdateFlightDto dto)
    {
        var flight = await _flightRepository.GetByIdAsync(flightId);
        if (flight == null)
        {
            return ApiResponse<FlightDto>.Fail($"Flight with ID {flightId} not found.");
        }

        if (dto.Source.Trim().Equals(dto.Destination.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<FlightDto>.Fail("Source and destination must differ.");
        }

        if (dto.ArrivalTime <= dto.DepartureTime)
        {
            return ApiResponse<FlightDto>.Fail("Arrival time must be later than departure time.");
        }

        flight.Airline = dto.Airline.Trim();
        flight.Source = dto.Source.Trim().ToUpperInvariant();
        flight.Destination = dto.Destination.Trim().ToUpperInvariant();
        flight.DepartureTime = dto.DepartureTime;
        flight.ArrivalTime = dto.ArrivalTime;
        flight.BaseFare = dto.BaseFare;
        flight.IsActive = dto.IsActive;

        await _flightRepository.UpdateAsync(flight);
        return ApiResponse<FlightDto>.Ok(MapToFlightDto(flight), "Flight updated successfully.");
    }

    public async Task<ApiResponse<FlightDto>> DeactivateFlightAsync(int flightId)
    {
        var flight = await _flightRepository.GetByIdAsync(flightId);
        if (flight == null)
        {
            return ApiResponse<FlightDto>.Fail($"Flight with ID {flightId} not found.");
        }

        flight.IsActive = false;
        await _flightRepository.UpdateAsync(flight);
        return ApiResponse<FlightDto>.Ok(MapToFlightDto(flight), "Flight deactivated successfully.");
    }

    public async Task<ApiResponse<SeatOperationResponseDto>> HoldSeatsAsync(int flightId, HoldSeatsRequestDto dto)
    {
        var flight = await _flightRepository.GetByIdAsync(flightId);
        if (flight == null || !flight.IsActive)
        {
            return ApiResponse<SeatOperationResponseDto>.Fail("Flight not found or is inactive.");
        }

        var distinctSeats = dto.SeatNumbers.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        var success = await _seatRepository.HoldSeatsAtomicAsync(flightId, distinctSeats);

        var result = new SeatOperationResponseDto
        {
            FlightId = flightId,
            ProcessedSeats = success ? distinctSeats : new List<string>(),
            FailedSeats = success ? new List<string>() : distinctSeats,
            Success = success,
            Message = success ? "Seats held successfully." : "One or more seats are already held, booked, or invalid."
        };

        return success
            ? ApiResponse<SeatOperationResponseDto>.Ok(result, "Seats held successfully.")
            : ApiResponse<SeatOperationResponseDto>.Fail("Failed to hold requested seats.", new List<string> { result.Message });
    }

    public async Task<ApiResponse<SeatOperationResponseDto>> ConfirmSeatsAsync(int flightId, ConfirmSeatsRequestDto dto)
    {
        var distinctSeats = dto.SeatNumbers.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        var success = await _seatRepository.ConfirmSeatsAtomicAsync(flightId, distinctSeats);

        var result = new SeatOperationResponseDto
        {
            FlightId = flightId,
            ProcessedSeats = success ? distinctSeats : new List<string>(),
            FailedSeats = success ? new List<string>() : distinctSeats,
            Success = success,
            Message = success ? "Seats confirmed as Booked." : "Failed to confirm seats."
        };

        return success
            ? ApiResponse<SeatOperationResponseDto>.Ok(result, "Seats confirmed.")
            : ApiResponse<SeatOperationResponseDto>.Fail("Failed to confirm seats.", new List<string> { result.Message });
    }

    public async Task<ApiResponse<SeatOperationResponseDto>> ReleaseSeatsAsync(int flightId, ReleaseSeatsRequestDto dto)
    {
        var distinctSeats = dto.SeatNumbers.Select(s => s.Trim().ToUpperInvariant()).Distinct().ToList();
        var success = await _seatRepository.ReleaseSeatsAtomicAsync(flightId, distinctSeats);

        var result = new SeatOperationResponseDto
        {
            FlightId = flightId,
            ProcessedSeats = distinctSeats,
            Success = success,
            Message = "Seats released to Available."
        };

        return ApiResponse<SeatOperationResponseDto>.Ok(result, "Seats released.");
    }

    private static FlightDto MapToFlightDto(Flight flight)
    {
        return new FlightDto
        {
            FlightId = flight.FlightId,
            FlightNumber = flight.FlightNumber,
            Airline = flight.Airline,
            Source = flight.Source,
            Destination = flight.Destination,
            DepartureTime = flight.DepartureTime,
            ArrivalTime = flight.ArrivalTime,
            TotalSeats = flight.TotalSeats,
            AvailableSeats = flight.Seats?.Count(s => s.IsAvailable && s.SeatStatus == SeatStatusConstants.Available) ?? 0,
            BaseFare = flight.BaseFare,
            IsActive = flight.IsActive
        };
    }
}
