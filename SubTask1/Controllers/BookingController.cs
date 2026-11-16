using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;

namespace webapp_f26_exam.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingController> _logger;
    private readonly UserManager<IdentityUser> _userManager;

    public BookingController(
        IRoomRepository roomRepository,
        IBookingRepository bookingRepository,
        ILogger<BookingController> logger,
        UserManager<IdentityUser> userManager)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int roomId, DateTime? start, DateTime? end)
    {
        var room = await _roomRepository.GetRoomByIdAsync(roomId);
        if (room == null)
        {
            _logger.LogWarning("Booking form requested for non-existing room {RoomId}", roomId);
            return NotFound();
        }

        var booking = new Booking
        {
            RoomId = room.Id,
            Room = room,
            UserId = _userManager.GetUserId(User) ?? string.Empty,
            Start = start ?? DateTime.Today.AddHours(DateTime.Now.Hour + 1),
            End = end ?? (start?.AddHours(1) ?? DateTime.Today.AddHours(DateTime.Now.Hour + 2))
        };

        var allBookings = await _bookingRepository.GetAllBookingsAsync();
        ViewBag.UpcomingBookings = allBookings.Where(b => b.RoomId == roomId && b.End >= DateTime.Now)
            .OrderBy(b => b.Start).ToList();

        return View(booking);
    }

    [HttpPost]
    public async Task<IActionResult> Create(Booking booking, string? bookingDate = null, string? startTime = null, string? endTime = null)
    {
        var room = await _roomRepository.GetRoomByIdAsync(booking.RoomId);
        if (room == null)
        {
            _logger.LogWarning("Booking rejected: room {RoomId} not found", booking.RoomId);
            ModelState.AddModelError("RoomId", "Room not found");
            return View(booking);
        }

        booking.Room = room;

        if (!string.IsNullOrEmpty(bookingDate) && !string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
        {
            booking.Start = DateTime.Parse($"{bookingDate} {startTime}");
            booking.End = DateTime.Parse($"{bookingDate} {endTime}");
        }

        var allBookings = await _bookingRepository.GetAllBookingsAsync();
        ViewBag.UpcomingBookings = allBookings
            .Where(b => b.RoomId == booking.RoomId && b.End >= DateTime.Now)
            .OrderBy(b => b.Start)
            .ToList();

        if (booking.Start < DateTime.Now)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: start time {Start} is in the past", room.RoomCode, booking.Start);
            ModelState.AddModelError("Start", "Start time cannot be in the past.");
            return View(booking);
        }

        if (booking.Start >= booking.End)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: end {End} is not after start {Start}", room.RoomCode, booking.End, booking.Start);
            ModelState.AddModelError("End", "End time must be after start time.");
            return View(booking);
        }

        if (booking.End > booking.Start.AddHours(4))
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: duration exceeds 4 hours", room.RoomCode);
            ModelState.AddModelError(string.Empty, "Booking duration cannot exceed 4 hours.");
            return View(booking);
        }

        if (booking.Start.Minute % 15 != 0 || booking.Start.Second != 0 || booking.End.Minute % 15 != 0 || booking.End.Second != 0)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: times must be in 15-minute intervals", room.RoomCode);
            ModelState.AddModelError(string.Empty, "Bookings must start and end on a 15-minute interval (e.g. 10:00, 10:15, 10:30, etc.");
            return View(booking);
        }

        // Check for overlapping bookings
        if (await _bookingRepository.HasConflictAsync(booking.RoomId, booking.Start, booking.End))
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: time slot {Start} - {End} is already booked", room.RoomCode, booking.Start, booking.End);
            ModelState.AddModelError(string.Empty, "This room is already booked for the selected time slot.");
            return View(booking);
        }

        booking.UserId = _userManager.GetUserId(User) ?? string.Empty;

        var success = await _bookingRepository.CreateBookingAsync(booking);
        if (!success)
        {
            _logger.LogWarning("Booking for room {RoomCode} could not be saved", room.RoomCode);
            ModelState.AddModelError(string.Empty, "Could not save booking. Please try again.");
            return View(booking);
        }

        _logger.LogInformation("Booking created for room {RoomCode} by user {User}", room.RoomCode, booking.UserId);

        TempData["Message"] = "Room booked successfully!";
        return RedirectToAction("BookingConfirmed", "Booking", new { id = booking.Id });
    }

    public async Task<IActionResult> BookingConfirmed(int id)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("Booking confirmation requested for non-existing booking {BookingId}", id);
            return NotFound();
        }

        return View(booking);
    }

    public async Task<IActionResult> MyBookings()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var bookings = await _bookingRepository.GetBookingsByUserIdAsync(userId);
        var joinedBookings = await _bookingRepository.GetJoinedBookingsByUserIdAsync(userId);

        var allBookings = bookings
            .Concat(joinedBookings)
            .OrderBy(b => b.Start)
            .ToList();

        return View(allBookings);
    }

    [HttpPost]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("Cancel rejected: booking {BookingId} not found", id);
            return NotFound();
        }
        if (booking.UserId != _userManager.GetUserId(User))
        {
            _logger.LogWarning("Cancel rejected: user {User} tried to cancel booking {BookingId} owned by another user", User.Identity?.Name, id);
            return Unauthorized();
        }
        if (booking.Start < DateTime.Now)
        {
            _logger.LogWarning("Cancel rejected: booking {BookingId} has already started", id);
            TempData["Error"] = "Cannot cancel past or ongoing bookings.";
            return RedirectToAction("MyBookings", "Booking");
        }

        await _bookingRepository.DeleteBookingAsync(id);

        _logger.LogInformation("Booking cancelled for room {RoomCode} by user {User}", booking.Room?.RoomCode, booking.User?.Email ?? booking.UserId);
        TempData["Message"] = "Booking cancelled successfully!";
        return RedirectToAction("MyBookings", "Booking");
    }

    [HttpPost]
    public async Task<IActionResult> Join(int id)
    {
        var userID = _userManager.GetUserId(User) ?? string.Empty;
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("Join rejected: booking {BookingId} not found", id);
            return NotFound();
        }
        if (booking.UserId == userID)
        {
            _logger.LogWarning("Join rejected: user {User} tried to join booking {BookingId} owned by themselves", User.Identity?.Name, id);
            return Unauthorized();
        }
        if (booking.Start < DateTime.Now)
        {
            _logger.LogWarning("Join rejected: booking {BookingId} has already started", id);
            TempData["Error"] = "Cannot join past or ongoing bookings.";
            return RedirectToAction("MyBookings", "Booking");
        }
        if (booking.JoinFlag == false)
        {
            _logger.LogWarning("Join rejected: booking {BookingId} is not joinable", id);
            TempData["Error"] = "This booking is not joinable.";
            return RedirectToAction("MyBookings", "Booking");
        }

        var success = await _bookingRepository.AddParticipantAsync(id, userID);
        if (!success)
        {
            _logger.LogWarning("Join failed for booking {BookingId} by user {User}", id, userID);
            TempData["Error"] = "Could not join booking. Please try again.";
            return RedirectToAction("MyBookings", "Booking");
        }
        _logger.LogInformation("Booking joined for room {RoomCode} by user {User}", booking.Room?.RoomCode, User.Identity?.Name ?? userID);
        TempData["Message"] = "Booking joined successfully!";
        return RedirectToAction("MyBookings", "Booking");
    }

    [HttpPost]
    public async Task<IActionResult> Leave(int id)
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("Leave rejected: booking {BookingId} not found", id);
            return NotFound();
        }
        var success = await _bookingRepository.RemoveParticipantAsync(id, userId);
        if (!success)
        {
            _logger.LogWarning("Leave failed for booking {BookingId} by user {User}", id, userId);
            TempData["Error"] = "Could not leave booking. Please try again.";
            return RedirectToAction("MyBookings", "Booking");
        }
        _logger.LogInformation("User {User} left booking {BookingId} for room {RoomCode}", User.Identity?.Name ?? userId, id, booking.Room?.RoomCode);
        TempData["Message"] = "You have left the session successfully.";
        return RedirectToAction("MyBookings", "Booking");
    }

    [HttpPost]
    public async Task<IActionResult> RemoveParticipant(int bookingId, string participantUserId)
    {
        var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
        {
            _logger.LogWarning("RemoveParticipant rejected: booking {BookingId} not found", bookingId);
            return NotFound();
        }

        // Only the organizer/owner can remove participants
        if (booking.UserId != currentUserId)
        {
            _logger.LogWarning("RemoveParticipant unauthorized: user {User} is not the organizer of booking {BookingId}", currentUserId, bookingId);
            return Unauthorized();
        }

        var success = await _bookingRepository.RemoveParticipantAsync(bookingId, participantUserId);
        if (!success)
        {
            _logger.LogWarning("RemoveParticipant failed for participant {ParticipantId} from booking {BookingId}", participantUserId, bookingId);
            TempData["Error"] = "Could not remove participant. Please try again.";
        }
        else
        {
            _logger.LogInformation("Organizer {Organizer} removed participant {ParticipantId} from booking {BookingId}", currentUserId, participantUserId, bookingId);
            TempData["Message"] = "Participant removed successfully.";
        }

        return RedirectToAction("BookingConfirmed", "Booking", new { id = bookingId });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleJoinFlag(int id)
    {
        var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("ToggleJoinFlag rejected: booking {BookingId} not found", id);
            return NotFound();
        }

        if (booking.UserId != currentUserId)
        {
            _logger.LogWarning("ToggleJoinFlag unauthorized: user {User} is not the organizer of booking {BookingId}", currentUserId, id);
            return Unauthorized();
        }

        booking.JoinFlag = !booking.JoinFlag;
        var success = await _bookingRepository.UpdateBookingAsync(booking);
        if (!success)
        {
            _logger.LogWarning("ToggleJoinFlag failed to update booking {BookingId}", id);
            TempData["Error"] = "Could not update session status. Please try again.";
        }
        else
        {
            var statusText = booking.JoinFlag ? "open for others to join" : "private";
            _logger.LogInformation("Booking {BookingId} join status changed to {Status} by user {User}", id, statusText, currentUserId);
            TempData["Message"] = $"Session is now {statusText}.";
        }

        return RedirectToAction("BookingConfirmed", "Booking", new { id = id });
    }
}
