using System.Net.Http.Json;
using FlightBookingSystem.Shared.Models;

namespace BookingService.Clients;

public class PaymentSimulateResult
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}

public interface IPaymentServiceClient
{
    Task<(bool Success, string TransactionId, string Message)> SimulatePaymentAsync(int bookingId, decimal amount, string paymentMethod, string simulateStatus);
    Task<bool> SimulateRefundAsync(int bookingId, decimal amount);
}

public class PaymentServiceClient : IPaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentServiceClient> _logger;

    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(bool Success, string TransactionId, string Message)> SimulatePaymentAsync(int bookingId, decimal amount, string paymentMethod, string simulateStatus)
    {
        try
        {
            var body = new
            {
                BookingId = bookingId,
                Amount = amount,
                PaymentMethod = paymentMethod,
                SimulateStatus = simulateStatus
            };

            var response = await _httpClient.PostAsJsonAsync("/api/payments/simulate", body);
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaymentSimulateResult>>();

            if (result?.Success == true && result.Data?.PaymentStatus == "Success")
            {
                return (true, result.Data.TransactionId, "Payment successful.");
            }

            return (false, string.Empty, result?.Message ?? "Payment simulation failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PaymentService for booking {BookingId}", bookingId);
            return (false, string.Empty, "Error communicating with payment gateway.");
        }
    }

    public async Task<bool> SimulateRefundAsync(int bookingId, decimal amount)
    {
        try
        {
            var body = new { Amount = amount };
            var response = await _httpClient.PostAsJsonAsync($"/api/payments/{bookingId}/refund-simulate", body);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
            return result?.Success == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for booking {BookingId}", bookingId);
            return false;
        }
    }
}
