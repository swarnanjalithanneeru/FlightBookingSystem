namespace BookingService.Entities;

public class Booking
{
    public int BookingId { get; set; }
    public int UserId { get; set; }
    public int FlightId { get; set; }
    public DateTime BookingDate { get; set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; set; }
    public string BookingStatus { get; set; } = "Pending"; // Pending, Confirmed, Failed, Cancelled
    public string? PNR { get; set; }
}
