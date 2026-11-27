using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.DAL;
using api.DTOs;
using api.Models;

namespace api.Controllers;

// REST API for bookings. All endpoints require a logged-in user. Base URL: /api/bookingapi
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingAPIController : ControllerBase
{
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<BookingAPIController> _logger;

    public BookingAPIController(IBookingRepository bookingRepository, IRoomRepository roomRepository, ILogger<BookingAPIController> logger)
    {
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _logger = logger;
    }

    // The logged-in user's id, read from the JWT (never from the request body)
    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    //HTTP GET endpoint to retrieve all bookings. Only for admins.
    [Authorize(Roles = "Admin")]
    [HttpGet("bookinglist")]
    public async Task<IActionResult> BookingList()
    {
        var bookings = await _bookingRepository.GetAllBookingsAsync();
        if (bookings == null)
        {
            _logger.LogError("[BookingAPIController] Booking list not found while executing GetAllBookingsAsync()");
            return NotFound("Booking list not found");
        }
        return Ok(bookings.Select(ToDto));
    }

    //HTTP GET endpoint to retrieve the logged-in user's own bookings.
    [HttpGet("mybookings")]
    public async Task<IActionResult> MyBookings()
    {
        var bookings = await _bookingRepository.GetBookingsByUserIdAsync(CurrentUserId);
        if (bookings == null)
        {
            _logger.LogError("[BookingAPIController] Bookings not found for user {UserId}", CurrentUserId);
            return NotFound("Bookings not found");
        }
        return Ok(bookings.Select(ToDto));
    }

    //HTTP GET endpoint to retrieve upcoming bookings for one room, so students can see and join them.
    [HttpGet("room/{roomId}")]
    public async Task<IActionResult> RoomBookings(int roomId)
    {
        var bookings = await _bookingRepository.GetUpcomingBookingsByRoomIdAsync(roomId);
        if (bookings == null)
        {
            _logger.LogError("[BookingAPIController] Upcoming bookings not found for room {RoomId}", roomId);
            return NotFound("Bookings not found");
        }
        return Ok(bookings.Select(ToDto));
    }

    //HTTP GET endpoint to retrieve one booking by its id. Returns 404 if the booking does not exist.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(int id)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogError("[BookingAPIController] Booking not found for the BookingId {BookingId:0000}", id);
            return NotFound("Booking not found for the BookingId");
        }
        return Ok(ToDto(booking));
    }

    //HTTP POST endpoint to create a booking. The domain rule is checked first; returns 400 with a message if it is broken.
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] BookingDto bookingDto)
    {
        var error = await ValidateBooking(bookingDto, null);
        if (error != null)
        {
            return BadRequest(error);
        }

        var newBooking = new Booking
        {
            RoomId = bookingDto.RoomId,
            UserId = CurrentUserId,
            Start = bookingDto.Start,
            End = bookingDto.End,
            Purpose = bookingDto.Purpose,
            JoinFlag = bookingDto.JoinFlag
        };
        bool returnOk = await _bookingRepository.CreateBookingAsync(newBooking);
        if (returnOk)
        {
            _logger.LogInformation("[BookingAPIController] Booking created for room {RoomId} by user {UserId}", newBooking.RoomId, newBooking.UserId);
            return CreatedAtAction(nameof(GetBooking), new { id = newBooking.Id }, new { newBooking.Id });
        }

        //throws 500 if fails
        _logger.LogWarning("[BookingAPIController] Booking creation failed for room {RoomId}", newBooking.RoomId);
        return StatusCode(500, "Internal server error");
    }

    //HTTP PUT endpoint to update a booking. Only the owner or an admin can update it, and the domain rule is checked again.
    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BookingDto bookingDto)
    {
        var existingBooking = await _bookingRepository.GetBookingByIdAsync(id);
        if (existingBooking == null)
        {
            return NotFound("Booking not found");
        }
        if (existingBooking.UserId != CurrentUserId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("[BookingAPIController] User {UserId} tried to update booking {BookingId} owned by another user", CurrentUserId, id);
            return Forbid();
        }

        var error = await ValidateBooking(bookingDto, id);
        if (error != null)
        {
            return BadRequest(error);
        }

        existingBooking.RoomId = bookingDto.RoomId;
        existingBooking.Start = bookingDto.Start;
        existingBooking.End = bookingDto.End;
        existingBooking.Purpose = bookingDto.Purpose;
        existingBooking.JoinFlag = bookingDto.JoinFlag;
        bool updateSuccessful = await _bookingRepository.UpdateBookingAsync(existingBooking);
        if (updateSuccessful)
        {
            // Success log: booking updated
            _logger.LogInformation("[BookingAPIController] Booking {BookingId} updated by user {UserId}", id, CurrentUserId);
            return Ok(ToDto(existingBooking));
        }

        //throws 500 if fails
        _logger.LogWarning("[BookingAPIController] Booking update failed for BookingId {BookingId:0000}", id);
        return StatusCode(500, "Internal server error");
    }

    //HTTP PUT endpoint to switch JoinFlag on/off (open for others or private). Only the owner, like ToggleJoinFlag in SubTask1.
    [HttpPut("togglejoinflag/{id}")]
    public async Task<IActionResult> ToggleJoinFlag(int id)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] ToggleJoinFlag rejected: booking {BookingId} not found", id);
            return NotFound("Booking not found");
        }
        if (booking.UserId != CurrentUserId)
        {
            _logger.LogWarning("[BookingAPIController] ToggleJoinFlag rejected: user {UserId} is not the owner of booking {BookingId}", CurrentUserId, id);
            return Forbid();
        }

        booking.JoinFlag = !booking.JoinFlag;
        bool updateSuccessful = await _bookingRepository.UpdateBookingAsync(booking);
        if (updateSuccessful)
        {
            _logger.LogInformation("[BookingAPIController] Booking {BookingId} JoinFlag changed to {JoinFlag} by user {UserId}", id, booking.JoinFlag, CurrentUserId);
            return Ok(ToDto(booking));
        }

        //throws 500 if fails
        _logger.LogWarning("[BookingAPIController] ToggleJoinFlag failed for BookingId {BookingId:0000}", id);
        return StatusCode(500, "Internal server error");
    }

    //HTTP DELETE endpoint to cancel a booking. Only the owner or an admin, and not after the booking has started.
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("[BookingAPIController] Cancel rejected: booking {BookingId} not found", id);
            return NotFound("Booking not found");
        }
        if (booking.UserId != CurrentUserId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("[BookingAPIController] Cancel rejected: user {UserId} tried to cancel booking {BookingId} owned by another user", CurrentUserId, id);
            return Forbid();
        }
        if (booking.Start < DateTime.Now)
        {
            _logger.LogWarning("[BookingAPIController] Cancel rejected: booking {BookingId} has already started", id);
            return BadRequest("Cannot cancel past or ongoing bookings.");
        }

        bool returnOk = await _bookingRepository.DeleteBookingAsync(id);
        if (!returnOk)
        {
            _logger.LogError("[BookingAPIController] Booking deletion failed for the BookingId {BookingId:0000}", id);
            return BadRequest("Booking deletion failed");
        }
        // Success log: booking cancelled
        _logger.LogInformation("[BookingAPIController] Booking {BookingId} cancelled by user {UserId}", id, CurrentUserId);
        return NoContent();
    }

    // Domain rule for bookings (same rules as SubTask1). Returns an error message, or null if the booking is valid.
    private async Task<string?> ValidateBooking(BookingDto dto, int? excludeBookingId)
    {
        var room = await _roomRepository.GetRoomByIdAsync(dto.RoomId);
        if (room == null)
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected: room {RoomId} not found", dto.RoomId);
            return "Room not found.";
        }
        if (!room.IsActive)
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected for room {RoomCode}: room is not active", room.RoomCode);
            return "This room cannot be booked right now.";
        }
        if (dto.Start < DateTime.Now)
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected for room {RoomCode}: start time {Start} is in the past", room.RoomCode, dto.Start);
            return "Start time cannot be in the past.";
        }
        if (dto.Start >= dto.End)
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected for room {RoomCode}: end {End} is not after start {Start}", room.RoomCode, dto.End, dto.Start);
            return "End time must be after start time.";
        }
        if (dto.End > dto.Start.AddHours(4))
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected for room {RoomCode}: duration exceeds 4 hours", room.RoomCode);
            return "Booking duration cannot exceed 4 hours.";
        }
        if (dto.Start.Minute % 15 != 0 || dto.Start.Second != 0 || dto.End.Minute % 15 != 0 || dto.End.Second != 0)
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected for room {RoomCode}: times must be in 15-minute intervals", room.RoomCode);
            return "Bookings must start and end on a 15-minute interval (e.g. 10:00, 10:15, 10:30).";
        }
        if (await _bookingRepository.HasConflictAsync(dto.RoomId, dto.Start, dto.End, excludeBookingId))
        {
            _logger.LogWarning("[BookingAPIController] Booking rejected for room {RoomCode}: time slot {Start} - {End} is already booked", room.RoomCode, dto.Start, dto.End);
            return "This room is already booked for the selected time slot.";
        }
        return null;
    }

    // Maps a Booking entity to the DTO sent to the frontend
    private static BookingDto ToDto(Booking booking) => new BookingDto
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
