using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}

public class SimulatePaymentRequestDto
{
    [Required]
    public int BookingId { get; set; }

    [Range(0.01, 10000000)]
    public decimal Amount { get; set; }

    [StringLength(20)]
    public string PaymentMethod { get; set; } = "CreditCard";

    /// <summary>
    /// Desired simulation outcome: "Success" or "Failed"
    /// </summary>
    [Required]
    public string SimulateStatus { get; set; } = "Success";
}

public class SimulateRefundRequestDto
{
    [Range(0.01, 10000000)]
    public decimal Amount { get; set; }
}

public class PaymentSimulationResponseDto
{
    public int PaymentId { get; set; }
    public int BookingId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
