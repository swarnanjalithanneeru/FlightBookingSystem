namespace FlightBookingSystem.Shared.Constants;

public static class RoleConstants
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

public static class BookingStatusConstants
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public static class SeatStatusConstants
{
    public const string Available = "Available";
    public const string Held = "Held";
    public const string Booked = "Booked";
}

public static class PaymentStatusConstants
{
    public const string Success = "Success";
    public const string Failed = "Failed";
    public const string Refunded = "Refunded";
}

public static class CheckInStatusConstants
{
    public const string CheckedIn = "CheckedIn";
}
