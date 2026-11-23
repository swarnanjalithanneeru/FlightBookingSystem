using System.Net.Http.Json;
using FlightBookingSystem.Shared.Models;
using BookingService.DTOs;

namespace BookingService.Clients;

public interface IFlightServiceClient
{
    Task<FlightInfoRemoteDto?> GetFlightAsync(int flightId);
    Task<SeatMapRemoteDto?> GetSeatMapAsync(int flightId);
    Task<bool> HoldSeatsAsync(int flightId, List<string> seats, int bookingId);
    Task<bool> ConfirmSeatsAsync(int flightId, List<string> seats, int bookingId);
    Task<bool> ReleaseSeatsAsync(int flightId, List<string> seats, int bookingId);
}

public class FlightServiceClient : IFlightServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FlightServiceClient> _logger;

    public FlightServiceClient(HttpClient httpClient, ILogger<FlightServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<FlightInfoRemoteDto?> GetFlightAsync(int flightId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<FlightInfoRemoteDto>>($"/api/flights/{flightId}");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting flight {FlightId} from FlightService", flightId);
            return null;
        }
    }

    public async Task<SeatMapRemoteDto?> GetSeatMapAsync(int flightId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<SeatMapRemoteDto>>($"/api/flights/{flightId}/seats");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting seat map for flight {FlightId}", flightId);
            return null;
        }
    }

    public async Task<bool> HoldSeatsAsync(int flightId, List<string> seats, int bookingId)
    {
        try
        {
            var body = new { SeatNumbers = seats, BookingId = bookingId };
            var response = await _httpClient.PostAsJsonAsync($"/api/flights/{flightId}/seats/hold", body);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error holding seats for flight {FlightId}", flightId);
            return false;
        }
    }

    public async Task<bool> ConfirmSeatsAsync(int flightId, List<string> seats, int bookingId)
    {
        try
        {
            var body = new { SeatNumbers = seats, BookingId = bookingId };
            var response = await _httpClient.PostAsJsonAsync($"/api/flights/{flightId}/seats/confirm", body);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming seats for flight {FlightId}", flightId);
            return false;
        }
    }

    public async Task<bool> ReleaseSeatsAsync(int flightId, List<string> seats, int bookingId)
    {
        try
        {
            var body = new { SeatNumbers = seats, BookingId = bookingId };
            var response = await _httpClient.PostAsJsonAsync($"/api/flights/{flightId}/seats/release", body);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing seats for flight {FlightId}", flightId);
            return false;
        }
    }
}
