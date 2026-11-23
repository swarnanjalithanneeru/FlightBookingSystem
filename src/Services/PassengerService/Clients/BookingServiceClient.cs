using System.Net.Http.Json;
using FlightBookingSystem.Shared.Models;
using PassengerService.DTOs;

namespace PassengerService.Clients;

public interface IBookingServiceClient
{
    Task<BookingRemoteValidationDto?> GetBookingByPnrAsync(string pnr);
}

public class BookingServiceClient : IBookingServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BookingServiceClient> _logger;

    public BookingServiceClient(HttpClient httpClient, ILogger<BookingServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BookingRemoteValidationDto?> GetBookingByPnrAsync(string pnr)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ApiResponse<BookingRemoteValidationDto>>($"/api/bookings/by-pnr/{pnr}");
            return response?.Success == true ? response.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating booking with PNR {PNR} from BookingService", pnr);
            return null;
        }
    }
}
