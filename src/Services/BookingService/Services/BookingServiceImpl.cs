using System.Security.Cryptography;
using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using BookingService.Clients;
using BookingService.DTOs;
using BookingService.Entities;
using BookingService.Repositories;

namespace BookingService.Services;

public interface IBookingService
{
    Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(int userId, CreateBookingRequestDto dto);
    Task<ApiResponse<BookingResponseDto>> GetBookingByIdAsync(int bookingId, int requestUserId, bool isAdmin);
    Task<ApiResponse<List<BookingResponseDto>>> GetUserBookingsAsync(int userId);
    Task<ApiResponse<List<BookingResponseDto>>> GetAllBookingsAsync();
    Task<ApiResponse<List<BookingResponseDto>>> GetBookingsByFlightIdAsync(int flightId);
    Task<ApiResponse<CancelBookingResponseDto>> CancelBookingAsync(int bookingId, int requestUserId, bool isAdmin);
    Task<ApiResponse<BookingValidationDto>> GetBookingByPnrAsync(string pnr);
}

public class BookingServiceImpl : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IFlightServiceClient _flightServiceClient;
    private readonly IPassengerServiceClient _passengerServiceClient;
    private readonly IPaymentServiceClient _paymentServiceClient;
    private readonly ILogger<BookingServiceImpl> _logger;

    public BookingServiceImpl(
        IBookingRepository bookingRepository,
        IFlightServiceClient flightServiceClient,
        IPassengerServiceClient passengerServiceClient,
        IPaymentServiceClient paymentServiceClient,
        ILogger<BookingServiceImpl> logger)
    {
        _bookingRepository = bookingRepository;
        _flightServiceClient = flightServiceClient;
        _passengerServiceClient = passengerServiceClient;
        _paymentServiceClient = paymentServiceClient;
        _logger = logger;
    }

    public async Task<ApiResponse<BookingResponseDto>> CreateBookingAsync(int userId, CreateBookingRequestDto dto)
    {
        // 1. Validate passenger count
        if (dto.Passengers == null || !dto.Passengers.Any())
        {
            return ApiResponse<BookingResponseDto>.Fail("Booking must contain at least one passenger.");
        }

        // 2. Validate distinct seats per passenger
        var requestedSeatNumbers = dto.Passengers
            .Select(p => p.SeatNumber.Trim().ToUpperInvariant())
            .ToList();

        if (requestedSeatNumbers.Distinct().Count() != requestedSeatNumbers.Count)
        {
            return ApiResponse<BookingResponseDto>.Fail("Each passenger must select a distinct seat. Duplicate seats are rejected.");
        }

        // 3. Validate flight existence & active state
        var flight = await _flightServiceClient.GetFlightAsync(dto.FlightId);
        if (flight == null || !flight.IsActive)
        {
            return ApiResponse<BookingResponseDto>.Fail("Selected flight does not exist or is inactive.");
        }

        // 4. Validate seat existence and current availability from flight seat map
        var seatMap = await _flightServiceClient.GetSeatMapAsync(dto.FlightId);
        if (seatMap == null)
        {
            return ApiResponse<BookingResponseDto>.Fail("Could not verify flight seat map.");
        }

        foreach (var seatNum in requestedSeatNumbers)
        {
            var matchSeat = seatMap.Seats.FirstOrDefault(s => s.SeatNumber.Equals(seatNum, StringComparison.OrdinalIgnoreCase));
            if (matchSeat == null)
            {
                return ApiResponse<BookingResponseDto>.Fail($"Seat {seatNum} does not exist for this flight.");
            }
            if (!matchSeat.IsAvailable || matchSeat.SeatStatus != SeatStatusConstants.Available)
            {
                return ApiResponse<BookingResponseDto>.Fail($"Seat {seatNum} is not available.");
            }
        }

        // 5. Authoritative server calculation for total amount
        decimal authoritativeTotal = flight.BaseFare * dto.Passengers.Count;

        // 6. Create initial Pending booking
        var booking = new Booking
        {
            UserId = userId,
            FlightId = dto.FlightId,
            BookingDate = DateTime.UtcNow,
            TotalAmount = authoritativeTotal,
            BookingStatus = BookingStatusConstants.Pending,
            PNR = null
        };

        var createdBooking = await _bookingRepository.AddAsync(booking);

        // 7. Request atomic seat hold from Flight Service
        var holdSuccess = await _flightServiceClient.HoldSeatsAsync(dto.FlightId, requestedSeatNumbers, createdBooking.BookingId);
        if (!holdSuccess)
        {
            createdBooking.BookingStatus = BookingStatusConstants.Failed;
            await _bookingRepository.UpdateAsync(createdBooking);
            return ApiResponse<BookingResponseDto>.Fail("Failed to hold selected seats. One or more seats may have been taken.");
        }

        // 8. Simulate Payment
        var (paymentSuccess, transactionId, paymentMsg) = await _paymentServiceClient.SimulatePaymentAsync(
            createdBooking.BookingId,
            authoritativeTotal,
            dto.PaymentMethod ?? "CreditCard",
            dto.SimulatePaymentOutcome ?? "Success"
        );

        if (!paymentSuccess)
        {
            // Payment failed: mark booking Failed and release held seats (reliability / compensating transaction)
            createdBooking.BookingStatus = BookingStatusConstants.Failed;
            await _bookingRepository.UpdateAsync(createdBooking);
            await _flightServiceClient.ReleaseSeatsAsync(dto.FlightId, requestedSeatNumbers, createdBooking.BookingId);

            return ApiResponse<BookingResponseDto>.Fail($"Payment failed: {paymentMsg}. Held seats have been released.");
        }

        // 9. Payment succeeded: Generate unique PNR and confirm booking
        string pnr = await GenerateUniquePnrAsync();
        createdBooking.PNR = pnr;
        createdBooking.BookingStatus = BookingStatusConstants.Confirmed;
        await _bookingRepository.UpdateAsync(createdBooking);

        // Confirm seats in Flight Service
        await _flightServiceClient.ConfirmSeatsAsync(dto.FlightId, requestedSeatNumbers, createdBooking.BookingId);

        // Persist passenger records in Passenger Service
        var passengerRecords = await _passengerServiceClient.CreatePassengersBulkAsync(createdBooking.BookingId, dto.Passengers);

        var response = new BookingResponseDto
        {
            BookingId = createdBooking.BookingId,
            UserId = createdBooking.UserId,
            FlightId = createdBooking.FlightId,
            BookingDate = createdBooking.BookingDate,
            TotalAmount = createdBooking.TotalAmount,
            BookingStatus = createdBooking.BookingStatus,
            PNR = createdBooking.PNR,
            FlightNumber = flight.FlightNumber,
            Passengers = passengerRecords,
            Message = "Booking confirmed successfully."
        };

        return ApiResponse<BookingResponseDto>.Ok(response, "Booking confirmed.");
    }

    public async Task<ApiResponse<BookingResponseDto>> GetBookingByIdAsync(int bookingId, int requestUserId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return ApiResponse<BookingResponseDto>.Fail($"Booking with ID {bookingId} not found.");
        }

        // Customer can only retrieve own booking
        if (!isAdmin && booking.UserId != requestUserId)
        {
            return ApiResponse<BookingResponseDto>.Fail("Unauthorized: You can only view your own bookings.");
        }

        var passengers = await _passengerServiceClient.GetPassengersByBookingIdAsync(booking.BookingId);
        var flight = await _flightServiceClient.GetFlightAsync(booking.FlightId);

        var response = new BookingResponseDto
        {
            BookingId = booking.BookingId,
            UserId = booking.UserId,
            FlightId = booking.FlightId,
            BookingDate = booking.BookingDate,
            TotalAmount = booking.TotalAmount,
            BookingStatus = booking.BookingStatus,
            PNR = booking.PNR,
            FlightNumber = flight?.FlightNumber,
            Passengers = passengers
        };

        return ApiResponse<BookingResponseDto>.Ok(response);
    }

    public async Task<ApiResponse<List<BookingResponseDto>>> GetUserBookingsAsync(int userId)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        var list = new List<BookingResponseDto>();

        foreach (var b in bookings)
        {
            var passengers = await _passengerServiceClient.GetPassengersByBookingIdAsync(b.BookingId);
            list.Add(new BookingResponseDto
            {
                BookingId = b.BookingId,
                UserId = b.UserId,
                FlightId = b.FlightId,
                BookingDate = b.BookingDate,
                TotalAmount = b.TotalAmount,
                BookingStatus = b.BookingStatus,
                PNR = b.PNR,
                Passengers = passengers
            });
        }

        return ApiResponse<List<BookingResponseDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<BookingResponseDto>>> GetAllBookingsAsync()
    {
        var bookings = await _bookingRepository.GetAllAsync();
        var list = new List<BookingResponseDto>();

        foreach (var b in bookings)
        {
            list.Add(new BookingResponseDto
            {
                BookingId = b.BookingId,
                UserId = b.UserId,
                FlightId = b.FlightId,
                BookingDate = b.BookingDate,
                TotalAmount = b.TotalAmount,
                BookingStatus = b.BookingStatus,
                PNR = b.PNR
            });
        }

        return ApiResponse<List<BookingResponseDto>>.Ok(list);
    }

    public async Task<ApiResponse<List<BookingResponseDto>>> GetBookingsByFlightIdAsync(int flightId)
    {
        var bookings = await _bookingRepository.GetByFlightIdAsync(flightId);
        var list = new List<BookingResponseDto>();

        foreach (var b in bookings)
        {
            var passengers = await _passengerServiceClient.GetPassengersByBookingIdAsync(b.BookingId);
            list.Add(new BookingResponseDto
            {
                BookingId = b.BookingId,
                UserId = b.UserId,
                FlightId = b.FlightId,
                BookingDate = b.BookingDate,
                TotalAmount = b.TotalAmount,
                BookingStatus = b.BookingStatus,
                PNR = b.PNR,
                Passengers = passengers
            });
        }

        return ApiResponse<List<BookingResponseDto>>.Ok(list);
    }

    public async Task<ApiResponse<CancelBookingResponseDto>> CancelBookingAsync(int bookingId, int requestUserId, bool isAdmin)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return ApiResponse<CancelBookingResponseDto>.Fail($"Booking with ID {bookingId} not found.");
        }

        if (!isAdmin && booking.UserId != requestUserId)
        {
            return ApiResponse<CancelBookingResponseDto>.Fail("Unauthorized: You can only cancel your own bookings.");
        }

        if (booking.BookingStatus != BookingStatusConstants.Confirmed)
        {
            return ApiResponse<CancelBookingResponseDto>.Fail($"Only confirmed bookings can be cancelled. Current status: {booking.BookingStatus}");
        }

        // 1. Mark booking as Cancelled
        booking.BookingStatus = BookingStatusConstants.Cancelled;
        await _bookingRepository.UpdateAsync(booking);

        // 2. Simulate refund in Payment Service
        var refundSuccess = await _paymentServiceClient.SimulateRefundAsync(booking.BookingId, booking.TotalAmount);

        // 3. Release held/booked seats in Flight Service
        var passengers = await _passengerServiceClient.GetPassengersByBookingIdAsync(booking.BookingId);
        var seatsToRelease = passengers.Select(p => p.SeatNumber).ToList();

        if (seatsToRelease.Any())
        {
            await _flightServiceClient.ReleaseSeatsAsync(booking.FlightId, seatsToRelease, booking.BookingId);
        }

        var response = new CancelBookingResponseDto
        {
            BookingId = booking.BookingId,
            BookingStatus = booking.BookingStatus,
            RefundStatus = refundSuccess ? PaymentStatusConstants.Refunded : "RefundPending",
            ReleasedSeats = seatsToRelease,
            Message = "Booking cancelled successfully. Simulated refund processed and seats released."
        };

        return ApiResponse<CancelBookingResponseDto>.Ok(response, "Booking cancelled.");
    }

    public async Task<ApiResponse<BookingValidationDto>> GetBookingByPnrAsync(string pnr)
    {
        if (string.IsNullOrWhiteSpace(pnr))
        {
            return ApiResponse<BookingValidationDto>.Fail("PNR is required.");
        }

        var booking = await _bookingRepository.GetByPnrAsync(pnr.Trim());
        if (booking == null)
        {
            return ApiResponse<BookingValidationDto>.Fail($"Booking with PNR {pnr} not found.");
        }

        var dto = new BookingValidationDto
        {
            BookingId = booking.BookingId,
            UserId = booking.UserId,
            FlightId = booking.FlightId,
            BookingStatus = booking.BookingStatus,
            PNR = booking.PNR,
            TotalAmount = booking.TotalAmount
        };

        return ApiResponse<BookingValidationDto>.Ok(dto);
    }

    private async Task<string> GenerateUniquePnrAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (int i = 0; i < 10; i++)
        {
            var pnr = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
                .ToArray());

            if (!await _bookingRepository.ExistsPnrAsync(pnr))
            {
                return pnr;
            }
        }

        return $"FL{DateTime.UtcNow.Ticks % 10000:D4}";
    }
}
