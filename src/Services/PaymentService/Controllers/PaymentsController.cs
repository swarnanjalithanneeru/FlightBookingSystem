using FlightBookingSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>
    /// Record dummy payment outcome (Internal Booking / Simulation).
    /// </summary>
    [HttpPost("simulate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaymentSimulationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentSimulationResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SimulatePayment([FromBody] SimulatePaymentRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<PaymentSimulationResponseDto>.Fail("Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _paymentService.SimulatePaymentAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Record simulated refund outcome (Internal Booking / Simulation).
    /// </summary>
    [HttpPost("{bookingId:int}/refund-simulate")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaymentSimulationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SimulateRefund(int bookingId, [FromBody] SimulateRefundRequestDto dto)
    {
        var result = await _paymentService.SimulateRefundAsync(bookingId, dto);
        return Ok(result);
    }

    /// <summary>
    /// Get payment record by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _paymentService.GetPaymentByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Get payment record by Booking ID.
    /// </summary>
    [HttpGet("by-booking/{bookingId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PaymentDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByBookingId(int bookingId)
    {
        var result = await _paymentService.GetPaymentByBookingIdAsync(bookingId);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
