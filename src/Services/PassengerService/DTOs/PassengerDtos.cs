using System.ComponentModel.DataAnnotations;

namespace PassengerService.DTOs;

public class PassengerDto
{
    public int PassengerId { get; set; }
    public int BookingId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public bool IsCheckedIn { get; set; }
    public int? CheckInId { get; set; }
}

public class PassengerCreateItemDto
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

public class CreatePassengersBulkRequestDto
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    [MinLength(1)]
    public List<PassengerCreateItemDto> Passengers { get; set; } = new();
}

public class CreateCheckInRequestDto
{
    [Required]
    [StringLength(10, MinimumLength = 4)]
    public string PNR { get; set; } = string.Empty;

    [Required]
    public int PassengerId { get; set; }
}

public class CheckInResponseDto
{
    public int CheckInId { get; set; }
    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string SeatNumber { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public string CheckInStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class BookingRemoteValidationDto
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string? PNR { get; set; }
}
