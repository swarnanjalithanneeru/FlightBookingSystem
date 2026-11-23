namespace PassengerService.Entities;

public class CheckIn
{
    public int CheckInId { get; set; }
    public int BookingId { get; set; }
    public int PassengerId { get; set; }
    public DateTime CheckInDate { get; set; } = DateTime.UtcNow;
    public string CheckInStatus { get; set; } = "CheckedIn";

    public Passenger Passenger { get; set; } = null!;
}
