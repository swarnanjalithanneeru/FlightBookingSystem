using FlightBookingSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassengerService.DTOs;
using PassengerService.Services;

namespace PassengerService.Controllers;

[ApiController]
[Route("api/passengers")]
public class PassengersController : ControllerBase
{
    private readonly IPassengerService _passengerService;

    public PassengersController(IPassengerService passengerService)
    {
        _passengerService = passengerService;
    }

    /// <summary>
    /// Retrieve passenger record by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PassengerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PassengerDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _passengerService.GetPassengerByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Retrieve all passengers for a booking.
    /// </summary>
    [HttpGet("by-booking/{bookingId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<PassengerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByBookingId(int bookingId)
    {
        var result = await _passengerService.GetPassengersByBookingIdAsync(bookingId);
        return Ok(result);
    }

    /// <summary>
    /// Bulk create passenger records for a booking (Internal Booking Service).
    /// </summary>
    [HttpPost("bulk")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<PassengerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<PassengerDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBulk([FromBody] CreatePassengersBulkRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<List<PassengerDto>>.Fail("Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _passengerService.CreatePassengersBulkAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
