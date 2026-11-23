using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using FlightService.DTOs;
using FlightService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlightService.Controllers;

[ApiController]
[Route("api/flights")]
public class FlightsController : ControllerBase
{
    private readonly IFlightService _flightService;

    public FlightsController(IFlightService flightService)
    {
        _flightService = flightService;
    }

    /// <summary>
    /// Search flights by source, destination and travel date.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<FlightDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<FlightDto>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string source, [FromQuery] string destination, [FromQuery] DateTime date)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination))
        {
            return BadRequest(ApiResponse<List<FlightDto>>.Fail("Source and destination are required."));
        }

        var request = new FlightSearchRequestDto
        {
            Source = source,
            Destination = destination,
            TravelDate = date
        };

        var result = await _flightService.SearchFlightsAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// List all flights (for Admin or exploration).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<FlightDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var result = await _flightService.GetAllFlightsAsync();
        return Ok(result);
    }

    /// <summary>
    /// Get flight details by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _flightService.GetFlightByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Retrieve seat map with Available, Held and Booked seats.
    /// </summary>
    [HttpGet("{id:int}/seats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SeatMapResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SeatMapResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSeatMap(int id)
    {
        var result = await _flightService.GetSeatMapAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Create flight and seat layout (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFlight([FromBody] CreateFlightDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<FlightDto>.Fail("Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _flightService.CreateFlightAsync(dto);
        return result.Success
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.FlightId }, result)
            : BadRequest(result);
    }

    /// <summary>
    /// Update flight details (Admin only).
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFlight(int id, [FromBody] UpdateFlightDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<FlightDto>.Fail("Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _flightService.UpdateFlightAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Deactivate flight (Admin only).
    /// </summary>
    [HttpPost("{id:int}/deactivate")]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FlightDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateFlight(int id)
    {
        var result = await _flightService.DeactivateFlightAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Atomically hold selected seats (Internal Booking).
    /// </summary>
    [HttpPost("{id:int}/seats/hold")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SeatOperationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SeatOperationResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HoldSeats(int id, [FromBody] HoldSeatsRequestDto dto)
    {
        var result = await _flightService.HoldSeatsAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Confirm held seats as Booked (Internal Booking).
    /// </summary>
    [HttpPost("{id:int}/seats/confirm")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SeatOperationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SeatOperationResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmSeats(int id, [FromBody] ConfirmSeatsRequestDto dto)
    {
        var result = await _flightService.ConfirmSeatsAsync(id, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Release held/booked seats back to Available (Internal Booking).
    /// </summary>
    [HttpPost("{id:int}/seats/release")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SeatOperationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReleaseSeats(int id, [FromBody] ReleaseSeatsRequestDto dto)
    {
        var result = await _flightService.ReleaseSeatsAsync(id, dto);
        return Ok(result);
    }
}
