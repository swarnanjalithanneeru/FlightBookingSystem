using System.Net.Http.Json;
using FlightBookingSystem.Shared.Models;
using BookingService.DTOs;

namespace BookingService.Clients;

public interface IPassengerServiceClient
{
    Task<List<PassengerRecordDto>> CreatePassengersBulkAsync(int bookingId, List<PassengerBookingRequestItemDto> passengers);
    Task<List<PassengerRecordDto>> GetPassengersByBookingIdAsync(int bookingId);
}

public class PassengerServiceClient : IPassengerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PassengerServiceClient> _logger;

    public PassengerServiceClient(HttpClient httpClient, ILogger<PassengerServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<PassengerRecordDto>> CreatePassengersBulkAsync(int bookingId, List<PassengerBookingRequestItemDto> passengers)
    {
        try
        {
            var body = new
            {
                BookingId = bookingId,
                Passengers = passengers
            };

            var response = await _httpClient.PostAsJsonAsync("/api/passengers/bulk", body);
            if (!response.IsSuccessStatusCode) return new List<PassengerRecordDto>();

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<List<PassengerRecordDto>>>();
            return result?.Data ?? new List<PassengerRecordDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk passengers for booking {BookingId}", bookingId);
            return new List<PassengerRecordDto>();
        }
    }

    public async Task<List<PassengerRecordDto>> GetPassengersByBookingIdAsync(int bookingId)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<PassengerRecordDto>>>($"/api/passengers/by-booking/{bookingId}");
            return response?.Data ?? new List<PassengerRecordDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching passengers for booking {BookingId}", bookingId);
            return new List<PassengerRecordDto>();
        }
    }
}
