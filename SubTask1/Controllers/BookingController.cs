using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;
using webapp_f26_exam.ViewModels;

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

        var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
        var model = new BookingCreateViewModel
        {
            RoomId = room.Id,
            Start = start ?? DateTime.Today.AddHours(DateTime.Now.Hour + 1),
            End = end ?? (start?.AddHours(1) ?? DateTime.Today.AddHours(DateTime.Now.Hour + 2))
        };

        await LoadRoomDetailsAsync(model, room, currentUserId);
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BookingCreateViewModel model, string? bookingDate = null, string? startTime = null, string? endTime = null)
    {
        var room = await _roomRepository.GetRoomByIdAsync(model.RoomId);
        if (room == null)
        {
            _logger.LogWarning("Booking rejected: room {RoomId} not found", model.RoomId);
            ModelState.AddModelError("RoomId", "Room not found");
            return View(model);
        }

        var currentUserId = _userManager.GetUserId(User) ?? string.Empty;
        await LoadRoomDetailsAsync(model, room, currentUserId);

        if (!string.IsNullOrEmpty(bookingDate) && !string.IsNullOrEmpty(startTime) && !string.IsNullOrEmpty(endTime))
        {
            if (!DateTime.TryParse($"{bookingDate} {startTime}", out var parsedStart) ||
                !DateTime.TryParse($"{bookingDate} {endTime}", out var parsedEnd))
            {
                ModelState.AddModelError(string.Empty, "Invalid date or time format.");
                return View(model);
            }

            model.Start = parsedStart;
            model.End = parsedEnd;
        }

        if (model.Start < DateTime.Now)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: start time {Start} is in the past", room.RoomCode, model.Start);
            ModelState.AddModelError("Start", "Start time cannot be in the past.");
            return View(model);
        }

        if (model.Start >= model.End)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: end {End} is not after start {Start}", room.RoomCode, model.End, model.Start);
            ModelState.AddModelError("End", "End time must be after start time.");
            return View(model);
        }

        if (model.End > model.Start.AddHours(4))
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: duration exceeds 4 hours", room.RoomCode);
            ModelState.AddModelError(string.Empty, "Booking duration cannot exceed 4 hours.");
            return View(model);
        }

        if (model.Start.Minute % 15 != 0 || model.Start.Second != 0 || model.End.Minute % 15 != 0 || model.End.Second != 0)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: times must be in 15-minute intervals", room.RoomCode);
            ModelState.AddModelError(string.Empty, "Bookings must start and end on a 15-minute interval (e.g. 10:00, 10:15, 10:30, etc.).");
            return View(model);
        }

        if (await _bookingRepository.HasConflictAsync(model.RoomId, model.Start, model.End))
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: time slot {Start} - {End} is already booked", room.RoomCode, model.Start, model.End);
            ModelState.AddModelError(string.Empty, "This room is already booked for the selected time slot.");
            return View(model);
        }

        var booking = new Booking
        {
            RoomId = model.RoomId,
            UserId = currentUserId,
            Start = model.Start,
            End = model.End,
            Purpose = model.Purpose,
            JoinFlag = model.JoinFlag
        };

        var success = await _bookingRepository.CreateBookingAsync(booking);
        if (!success)
        {
            _logger.LogWarning("Booking for room {RoomCode} could not be saved", room.RoomCode);
            ModelState.AddModelError(string.Empty, "Could not save booking. Please try again.");
            return View(model);
        }

        _logger.LogInformation("Booking created for room {RoomCode} by user {User}", room.RoomCode, booking.UserId);

        TempData["Message"] = "Room booked successfully!";
        return RedirectToAction("BookingConfirmed", "Booking", new { id = booking.Id });
    }

    private async Task LoadRoomDetailsAsync(BookingCreateViewModel model, Room room, string currentUserId)
    {
        model.Room = room;
        model.CurrentUserId = currentUserId;

        var allBookings = await _bookingRepository.GetAllBookingsAsync();
        model.UpcomingBookings = allBookings
            .Where(b => b.RoomId == room.Id && b.End >= DateTime.Now)
            .OrderBy(b => b.Start)
            .ToList();
    }

    public async Task<IActionResult> BookingConfirmed(int id)
    {
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("Booking confirmation requested for non-existing booking {BookingId}", id);
            return NotFound();
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var viewModel = new BookingDetailsViewModel
        {
            Booking = booking,
            CurrentUserId = currentUserId,
            IsOwner = booking.UserId == currentUserId,
            IsParticipant = IsUserParticipating(booking, currentUserId),
            ChatMessages = await _bookingRepository.GetMessagesByBookingIdAsync(id)
        };

        return View(viewModel);
    }

    public async Task<IActionResult> MyBookings()
    {
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var bookings = await _bookingRepository.GetBookingsByUserIdAsync(userId);
        var joinedBookings = await _bookingRepository.GetJoinedBookingsByUserIdAsync(userId);

        var now = DateTime.Now;
        var items = bookings.Concat(joinedBookings).Select(b => new BookingItemViewModel
        {
            Id = b.Id,
            RoomName = b.Room?.Name ?? string.Empty,
            RoomCode = b.Room?.RoomCode ?? string.Empty,
            Building = b.Room?.Building ?? string.Empty,
            Floor = b.Room?.Floor ?? 0,
            Start = b.Start,
            End = b.End,
            Purpose = b.Purpose,
            IsOrganizer = b.UserId == userId
        }).ToList();

        var viewModel = new MyBookingsViewModel
        {
            UpcomingBookings = items.Where(b => b.End >= now).OrderBy(b => b.Start).ToList(),
            PastBookings = items.Where(b => b.End < now).OrderByDescending(b => b.Start).ToList()
        };

        return View(viewModel);
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
        var userId = _userManager.GetUserId(User) ?? string.Empty;
        var booking = await _bookingRepository.GetBookingByIdAsync(id);
        if (booking == null)
        {
            _logger.LogWarning("Join rejected: booking {BookingId} not found", id);
            return NotFound();
        }
        if (booking.UserId == userId)
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

        var success = await _bookingRepository.AddParticipantAsync(id, userId);
        if (!success)
        {
            _logger.LogWarning("Join failed for booking {BookingId} by user {User}", id, userId);
            TempData["Error"] = "Could not join booking. Please try again.";
            return RedirectToAction("MyBookings", "Booking");
        }
        _logger.LogInformation("Booking joined for room {RoomCode} by user {User}", booking.Room?.RoomCode, User.Identity?.Name ?? userId);
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

    private bool IsUserParticipating(Booking booking, string? userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;
        if (booking.UserId == userId) return true;
        return booking.Participants.Any(p => p.UserId == userId);
    }

    [HttpPost]
    public async Task<IActionResult> SendChatMessage(int bookingId, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["ChatError"] = "Message cannot be empty.";
            return RedirectToAction("BookingConfirmed", new { id = bookingId });
        }

        if (message.Trim().Length > 500)
        {
            TempData["ChatError"] = "Message cannot exceed 500 characters.";
            return RedirectToAction("BookingConfirmed", new { id = bookingId });
        }

        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null) return NotFound();
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!IsUserParticipating(booking, currentUserId))
        {
            return Forbid();
        }
        var chatMessage = new BookingChat
        {
            BookingId = bookingId,
            UserId = currentUserId!,
            Message = message.Trim(),
            SentAt = DateTime.Now
        };
        var success = await _bookingRepository.AddMessageAsync(chatMessage);
        if (!success)
        {
            TempData["ChatError"] = "Failed to send message.";
        }
        return RedirectToAction("BookingConfirmed", new { id = bookingId });
    }
    [HttpPost]
    public async Task<IActionResult> EditChatMessage(int messageId, string newMessage)
    {
        var message = await _bookingRepository.GetMessageByIdAsync(messageId);
        if (message == null) return NotFound();

        if (string.IsNullOrWhiteSpace(newMessage))
        {
            TempData["ChatError"] = "Message cannot be empty.";
            return RedirectToAction("BookingConfirmed", new { id = message.BookingId });
        }

        if (newMessage.Trim().Length > 500)
        {
            TempData["ChatError"] = "Message cannot exceed 500 characters.";
            return RedirectToAction("BookingConfirmed", new { id = message.BookingId });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (message.UserId != currentUserId)
        {
            return Forbid();
        }
        message.Message = newMessage.Trim();
        message.UpdatedAt = DateTime.Now;
        await _bookingRepository.UpdateMessageAsync(message);
        return RedirectToAction("BookingConfirmed", new { id = message.BookingId });
    }
    [HttpPost]
    public async Task<IActionResult> DeleteChatMessage(int messageId)
    {
        var message = await _bookingRepository.GetMessageByIdAsync(messageId);
        if (message == null) return NotFound();
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var booking = await _bookingRepository.GetBookingByIdAsync(message.BookingId);
        bool isAuthor = message.UserId == currentUserId;
        bool isOrganizer = booking != null && booking.UserId == currentUserId;
        bool isAdmin = User.IsInRole("Admin");

        if (!isAuthor && !isOrganizer && !isAdmin)
        {
            return Forbid();
        }
        await _bookingRepository.DeleteMessageAsync(messageId);
        return RedirectToAction("BookingConfirmed", new { id = message.BookingId });
    }
}
