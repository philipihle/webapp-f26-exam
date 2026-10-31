using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;
using Microsoft.AspNetCore.Identity;


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

        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Booking booking)
    {
        var room = await _roomRepository.GetRoomByIdAsync(booking.RoomId);
        if (room == null)
        {
            _logger.LogWarning("Booking rejected: room {RoomId} not found", booking.RoomId);
            ModelState.AddModelError("RoomId", "Room not found");
            return View(booking);
        }

        // Reattach room so view can access @Model.Room
        booking.Room = room;

        if (booking.Start >= booking.End)
        {
            _logger.LogWarning("Booking rejected for room {RoomCode}: end {End} is not after start {Start}", room.RoomCode, booking.End, booking.Start);
            ModelState.AddModelError("End", "End time must be after start time.");
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
        return View(bookings);
    }

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
}