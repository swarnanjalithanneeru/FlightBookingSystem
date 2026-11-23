namespace FlightService.Entities;

public class Flight
{
    public int FlightId { get; set; }
    public string FlightNumber { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime { get; set; }
    public int TotalSeats { get; set; }
    public decimal BaseFare { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FlightSeat> Seats { get; set; } = new List<FlightSeat>();
}
