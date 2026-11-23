using System.ComponentModel.DataAnnotations;

namespace FlightService.DTOs;

public class FlightDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int TotalSeats { get; set; }
    public int AvailableSeats { get; set; }
    public decimal BaseFare { get; set; }
    public bool IsActive { get; set; }
}

public class FlightSeatDto
{
    public int SeatId { get; set; }
    public int FlightId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string SeatClass { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public string SeatStatus { get; set; } = string.Empty;
}

public class SeatMapResponseDto
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public List<FlightSeatDto> Seats { get; set; } = new();
}

public class CreateFlightDto
{
    [Required]
    [StringLength(10)]
    public string FlightNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Airline { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Range(1, 1000)]
    public int TotalSeats { get; set; }

    [Range(1, 1000000)]
    public decimal BaseFare { get; set; }

    public List<string>? CustomSeats { get; set; }
}

public class UpdateFlightDto
{
    [Required]
    [StringLength(50)]
    public string Airline { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Source { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateTime DepartureTime { get; set; }

    [Required]
    public DateTime ArrivalTime { get; set; }

    [Range(1, 1000000)]
    public decimal BaseFare { get; set; }

    public bool IsActive { get; set; } = true;
}

public class FlightSearchRequestDto
{
    [Required]
    public string Source { get; set; } = string.Empty;

    [Required]
    public string Destination { get; set; } = string.Empty;

    [Required]
    public DateTime TravelDate { get; set; }
}

public class HoldSeatsRequestDto
{
    [Required]
    [MinLength(1)]
    public List<string> SeatNumbers { get; set; } = new();

    public int BookingId { get; set; }
}

public class ConfirmSeatsRequestDto
{
    [Required]
    [MinLength(1)]
    public List<string> SeatNumbers { get; set; } = new();

    public int BookingId { get; set; }
}

public class ReleaseSeatsRequestDto
{
    [Required]
    [MinLength(1)]
    public List<string> SeatNumbers { get; set; } = new();

    public int BookingId { get; set; }
}

public class SeatOperationResponseDto
{
    public int FlightId { get; set; }
    public List<string> ProcessedSeats { get; set; } = new();
    public List<string> FailedSeats { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
