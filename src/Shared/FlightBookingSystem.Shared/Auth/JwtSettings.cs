namespace FlightBookingSystem.Shared.Auth;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";
    public string SecretKey { get; set; } = "FlightBookingSystem_Super_Secret_Key_2026_Minimum_32_Chars!";
    public string Issuer { get; set; } = "FlightBookingSystem";
    public string Audience { get; set; } = "FlightBookingSystemClients";
    public int ExpiryInMinutes { get; set; } = 120;
}
