namespace FlightService.Entities;

public class FlightSeat
{
    public int SeatId { get; set; }
    public int FlightId { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string SeatClass { get; set; } = "Economy";
    public bool IsAvailable { get; set; } = true;
    public string SeatStatus { get; set; } = "Available"; // Available, Held, Booked

    public Flight Flight { get; set; } = null!;
}
