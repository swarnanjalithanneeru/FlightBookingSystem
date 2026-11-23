using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using PaymentService.DTOs;
using PaymentService.Entities;
using PaymentService.Repositories;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<ApiResponse<PaymentSimulationResponseDto>> SimulatePaymentAsync(SimulatePaymentRequestDto dto);
    Task<ApiResponse<PaymentSimulationResponseDto>> SimulateRefundAsync(int bookingId, SimulateRefundRequestDto dto);
    Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(int paymentId);
    Task<ApiResponse<PaymentDto>> GetPaymentByBookingIdAsync(int bookingId);
}

public class PaymentServiceImpl : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;

    public PaymentServiceImpl(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<ApiResponse<PaymentSimulationResponseDto>> SimulatePaymentAsync(SimulatePaymentRequestDto dto)
    {
        // 1. Validate simulation status
        var status = dto.SimulateStatus.Trim();
        if (!status.Equals(PaymentStatusConstants.Success, StringComparison.OrdinalIgnoreCase) &&
            !status.Equals(PaymentStatusConstants.Failed, StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<PaymentSimulationResponseDto>.Fail("Only 'Success' or 'Failed' outcomes are accepted in the payment simulation.");
        }

        // 2. Idempotency check: if a transaction already exists for this booking
        var existing = await _paymentRepository.GetByBookingIdAsync(dto.BookingId);
        if (existing != null)
        {
            var existingResponse = new PaymentSimulationResponseDto
            {
                PaymentId = existing.PaymentId,
                BookingId = existing.BookingId,
                Amount = existing.Amount,
                PaymentMethod = existing.PaymentMethod,
                TransactionId = existing.TransactionId,
                PaymentStatus = existing.PaymentStatus,
                PaymentDate = existing.PaymentDate,
                Message = $"Idempotent response: payment already processed with status {existing.PaymentStatus}."
            };

            return existing.PaymentStatus == PaymentStatusConstants.Success
                ? ApiResponse<PaymentSimulationResponseDto>.Ok(existingResponse, "Existing payment record retrieved.")
                : ApiResponse<PaymentSimulationResponseDto>.Fail("Payment previously failed for this booking.",
                    new List<string> { existingResponse.Message });
        }

        // 3. Create simulated transaction
        var normalizedStatus = status.Equals(PaymentStatusConstants.Success, StringComparison.OrdinalIgnoreCase)
            ? PaymentStatusConstants.Success
            : PaymentStatusConstants.Failed;

        var transactionId = $"TXN-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}";

        var payment = new Payment
        {
            BookingId = dto.BookingId,
            Amount = dto.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "CreditCard" : dto.PaymentMethod.Trim(),
            TransactionId = transactionId,
            PaymentStatus = normalizedStatus,
            PaymentDate = DateTime.UtcNow
        };

        var created = await _paymentRepository.AddAsync(payment);

        var response = new PaymentSimulationResponseDto
        {
            PaymentId = created.PaymentId,
            BookingId = created.BookingId,
            Amount = created.Amount,
            PaymentMethod = created.PaymentMethod,
            TransactionId = created.TransactionId,
            PaymentStatus = created.PaymentStatus,
            PaymentDate = created.PaymentDate,
            Message = normalizedStatus == PaymentStatusConstants.Success
                ? "Simulated payment successful."
                : "Simulated payment failed as requested."
        };

        return normalizedStatus == PaymentStatusConstants.Success
            ? ApiResponse<PaymentSimulationResponseDto>.Ok(response, "Payment succeeded.")
            : ApiResponse<PaymentSimulationResponseDto>.Fail("Payment simulation failed.", new List<string> { response.Message });
    }

    public async Task<ApiResponse<PaymentSimulationResponseDto>> SimulateRefundAsync(int bookingId, SimulateRefundRequestDto dto)
    {
        var existing = await _paymentRepository.GetByBookingIdAsync(bookingId);
        if (existing == null)
        {
            // If no payment record found, create a refunded record
            var refundPayment = new Payment
            {
                BookingId = bookingId,
                Amount = dto.Amount,
                PaymentMethod = "Refund",
                TransactionId = $"REF-{Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()}",
                PaymentStatus = PaymentStatusConstants.Refunded,
                PaymentDate = DateTime.UtcNow
            };

            var created = await _paymentRepository.AddAsync(refundPayment);
            var res = new PaymentSimulationResponseDto
            {
                PaymentId = created.PaymentId,
                BookingId = created.BookingId,
                Amount = created.Amount,
                PaymentMethod = created.PaymentMethod,
                TransactionId = created.TransactionId,
                PaymentStatus = created.PaymentStatus,
                PaymentDate = created.PaymentDate,
                Message = "Simulated refund processed."
            };
            return ApiResponse<PaymentSimulationResponseDto>.Ok(res);
        }

        existing.PaymentStatus = PaymentStatusConstants.Refunded;
        await _paymentRepository.UpdateAsync(existing);

        var response = new PaymentSimulationResponseDto
        {
            PaymentId = existing.PaymentId,
            BookingId = existing.BookingId,
            Amount = existing.Amount,
            PaymentMethod = existing.PaymentMethod,
            TransactionId = existing.TransactionId,
            PaymentStatus = existing.PaymentStatus,
            PaymentDate = DateTime.UtcNow,
            Message = "Simulated refund processed successfully."
        };

        return ApiResponse<PaymentSimulationResponseDto>.Ok(response, "Refund processed.");
    }

    public async Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(int paymentId)
    {
        var payment = await _paymentRepository.GetByIdAsync(paymentId);
        if (payment == null)
        {
            return ApiResponse<PaymentDto>.Fail($"Payment with ID {paymentId} not found.");
        }

        return ApiResponse<PaymentDto>.Ok(MapToDto(payment));
    }

    public async Task<ApiResponse<PaymentDto>> GetPaymentByBookingIdAsync(int bookingId)
    {
        var payment = await _paymentRepository.GetByBookingIdAsync(bookingId);
        if (payment == null)
        {
            return ApiResponse<PaymentDto>.Fail($"No payment found for booking ID {bookingId}.");
        }

        return ApiResponse<PaymentDto>.Ok(MapToDto(payment));
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            PaymentId = payment.PaymentId,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod,
            TransactionId = payment.TransactionId,
            PaymentStatus = payment.PaymentStatus,
            PaymentDate = payment.PaymentDate
        };
    }
}
