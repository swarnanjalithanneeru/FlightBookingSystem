using System.Security.Claims;
using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using BookingService.DTOs;
using BookingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingService.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>
    /// Create booking with passenger and seat selections (Customer).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{RoleConstants.Customer},{RoleConstants.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<BookingResponseDto>.Fail("Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var userId = GetCurrentUserId();
        var result = await _bookingService.CreateBookingAsync(userId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Retrieve own booking (Owner or Admin).
    /// </summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{RoleConstants.Customer},{RoleConstants.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<BookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(RoleConstants.Admin);

        var result = await _bookingService.GetBookingByIdAsync(id, userId, isAdmin);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Search / list all bookings (Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ApiResponse<List<BookingResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _bookingService.GetAllBookingsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Retrieve own bookings for the authenticated user.
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = $"{RoleConstants.Customer},{RoleConstants.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<List<BookingResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings()
    {
        var userId = GetCurrentUserId();
        var result = await _bookingService.GetUserBookingsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// View bookings associated with a flight (Admin only).
    /// </summary>
    [HttpGet("by-flight/{flightId:int}")]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ApiResponse<List<BookingResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByFlight(int flightId)
    {
        var result = await _bookingService.GetBookingsByFlightIdAsync(flightId);
        return Ok(result);
    }

    /// <summary>
    /// Cancel own confirmed booking (Owner).
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = $"{RoleConstants.Customer},{RoleConstants.Admin}")]
    [ProducesResponseType(typeof(ApiResponse<CancelBookingResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CancelBookingResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole(RoleConstants.Admin);

        var result = await _bookingService.CancelBookingAsync(id, userId, isAdmin);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Validate booking for check-in by PNR (Authorized Service / Customer / Admin).
    /// </summary>
    [HttpGet("by-pnr/{pnr}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<BookingValidationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BookingValidationDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPnr(string pnr)
    {
        var result = await _bookingService.GetBookingByPnrAsync(pnr);
        return result.Success ? Ok(result) : NotFound(result);
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
