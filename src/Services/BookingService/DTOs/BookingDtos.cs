using System.ComponentModel.DataAnnotations;

namespace BookingService.DTOs;

public class PassengerBookingRequestItemDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string PassengerName { get; set; } = string.Empty;

    [Range(0, 120)]
    public int Age { get; set; }

    [Required]
    [StringLength(10)]
    public string Gender { get; set; } = "Other";

    [Required]
    [StringLength(5)]
    public string SeatNumber { get; set; } = string.Empty;
}

public class CreateBookingRequestDto
{
    [Required]
    public int FlightId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Booking must contain at least one passenger.")]
    public List<PassengerBookingRequestItemDto> Passengers { get; set; } = new();

    [StringLength(20)]
    public string PaymentMethod { get; set; } = "CreditCard";

    /// <summary>
    /// For simulation testing: "Success" (default) or "Failed"
    /// </summary>
    public string SimulatePaymentOutcome { get; set; } = "Success";
}

public class PassengerRecordDto
{
    public int PassengerId { get; set; }
    public int BookingId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
}

public class BookingResponseDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public DateTime BookingDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string? PNR { get; set; }
    public List<PassengerRecordDto> Passengers { get; set; } = new();
    public string? FlightNumber { get; set; }
    public string? Message { get; set; }
}

public class CancelBookingResponseDto
{
    public int BookingId { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string RefundStatus { get; set; } = string.Empty;
    public List<string> ReleasedSeats { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class BookingValidationDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string? PNR { get; set; }
    public decimal TotalAmount { get; set; }
}

public class FlightInfoRemoteDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public decimal BaseFare { get; set; }
    public bool IsActive { get; set; }
}

public class SeatMapRemoteDto
{
    public int FlightId { get; set; }
    public List<FlightSeatRemoteDto> Seats { get; set; } = new();
}

public class FlightSeatRemoteDto
{
    public int SeatId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string SeatClass { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string SeatStatus { get; set; } = string.Empty;
}
