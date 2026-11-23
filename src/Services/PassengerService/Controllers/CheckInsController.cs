using FlightBookingSystem.Shared.Constants;
using FlightBookingSystem.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassengerService.DTOs;
using PassengerService.Services;

namespace PassengerService.Controllers;

[ApiController]
[Route("api/checkins")]
public class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkInService;

    public CheckInsController(ICheckInService checkInService)
    {
        _checkInService = checkInService;
    }

    /// <summary>
    /// Create check-in using PNR and PassengerId (Customer).
    /// </summary>
    [HttpPost]
    [AllowAnonymous] // or Authorize
    [ProducesResponseType(typeof(ApiResponse<CheckInResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CheckInResponseDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCheckIn([FromBody] CreateCheckInRequestDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<CheckInResponseDto>.Fail("Validation failed",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
        }

        var result = await _checkInService.CreateCheckInAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Retrieve check-in record by check-in ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CheckInResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CheckInResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _checkInService.GetCheckInByIdAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// Retrieve check-in record by passenger ID.
    /// </summary>
    [HttpGet("by-passenger/{passengerId:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CheckInResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CheckInResponseDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByPassengerId(int passengerId)
    {
        var result = await _checkInService.GetCheckInByPassengerIdAsync(passengerId);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
