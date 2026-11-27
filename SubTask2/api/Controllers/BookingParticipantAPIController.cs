using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.DAL;
using api.DTOs;
using api.Models;

namespace api.Controllers;

// REST API for booking participants. All endpoints require a logged-in user. Base URL: /api/bookingparticipantapi
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingParticipantAPIController : ControllerBase
{
    private readonly IBookingParticipantRepository _participantRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingParticipantAPIController> _logger;

    public BookingParticipantAPIController(IBookingParticipantRepository participantRepository, IBookingRepository bookingRepository, ILogger<BookingParticipantAPIController> logger)
    {
        _participantRepository = participantRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    // The logged-in user's id, read from the JWT (never from the request body)
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    //HTTP GET endpoint to retrieve all participants of one booking.
    [HttpGet("booking/{bookingId}")]
    public async Task<IActionResult> ParticipantList(int bookingId)
    {
        var participants = await _participantRepository.GetParticipantsByBookingIdAsync(bookingId);
        if (participants == null)
        {
            _logger.LogError("[BookingParticipantAPIController] Participant list not found for booking {BookingId}", bookingId);
            return NotFound("Participant list not found");
        }
        return Ok(participants.Select(ToDto));
    }

    //HTTP GET endpoint to retrieve the bookings the logged-in user has joined.
    [HttpGet("joinedbookings")]
    public async Task<IActionResult> JoinedBookings()
    {
        var bookings = await _participantRepository.GetJoinedBookingsByUserIdAsync(CurrentUserId);
        if (bookings == null)
        {
            _logger.LogError("[BookingParticipantAPIController] Joined bookings not found for user {UserId}", CurrentUserId);
            return NotFound("Joined bookings not found");
        }
        return Ok(bookings.Select(BookingToDto));
    }

    //HTTP POST endpoint to join a booking. Rules from SubTask1 are checked first; returns 400 with a message if one is broken.
    [HttpPost("join/{bookingId}")]
    public async Task<IActionResult> Join(int bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            _logger.LogWarning("[BookingParticipantAPIController] Join rejected: booking {BookingId} not found", bookingId);
            return NotFound("Booking not found");
        }
        if (booking.UserId == CurrentUserId)
        {
            _logger.LogWarning("[BookingParticipantAPIController] Join rejected: user {UserId} tried to join their own booking {BookingId}", CurrentUserId, bookingId);
            return BadRequest("You cannot join your own booking.");
        }
        if (booking.Start < DateTime.Now)
        {
            _logger.LogWarning("[BookingParticipantAPIController] Join rejected: booking {BookingId} has already started", bookingId);
            return BadRequest("Cannot join past or ongoing bookings.");
        }
        if (!booking.JoinFlag)
        {
            _logger.LogWarning("[BookingParticipantAPIController] Join rejected: booking {BookingId} is not joinable", bookingId);
            return BadRequest("This booking is not joinable.");
        }

        bool returnOk = await _participantRepository.AddParticipantAsync(bookingId, CurrentUserId);
        if (returnOk)
        {
            _logger.LogInformation("[BookingParticipantAPIController] User {UserId} joined booking {BookingId}", CurrentUserId, bookingId);
            return Ok();
        }

        //throws 500 if fails
        _logger.LogWarning("[BookingParticipantAPIController] Join failed for booking {BookingId} by user {UserId}", bookingId, CurrentUserId);
        return StatusCode(500, "Internal server error");
    }

    //HTTP DELETE endpoint to leave a booking the logged-in user has joined.
    [HttpDelete("leave/{bookingId}")]
    public async Task<IActionResult> Leave(int bookingId)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            _logger.LogWarning("[BookingParticipantAPIController] Leave rejected: booking {BookingId} not found", bookingId);
            return NotFound("Booking not found");
        }

        bool returnOk = await _participantRepository.RemoveParticipantAsync(bookingId, CurrentUserId);
        if (!returnOk)
        {
            _logger.LogWarning("[BookingParticipantAPIController] Leave failed for booking {BookingId} by user {UserId}", bookingId, CurrentUserId);
            return BadRequest("Could not leave booking");
        }
        _logger.LogInformation("[BookingParticipantAPIController] User {UserId} left booking {BookingId}", CurrentUserId, bookingId);
        return NoContent();
    }

    // Maps a BookingParticipant entity to the DTO sent to the frontend
    private static BookingParticipantDto ToDto(BookingParticipant participant) => new BookingParticipantDto
    {
        Id = participant.Id,
        BookingId = participant.BookingId,
        UserName = participant.User?.UserName,
        JoinedAtDateTime = participant.JoinedAtDateTime
    };

    // Maps a joined Booking to BookingDto, same fields as in BookingAPIController
    private static BookingDto BookingToDto(Booking booking) => new BookingDto
    {
        Id = booking.Id,
        RoomId = booking.RoomId,
        RoomName = booking.Room?.Name,
        UserName = booking.User?.UserName,
        Start = booking.Start,
        End = booking.End,
        Purpose = booking.Purpose,
        JoinFlag = booking.JoinFlag
    };
}
