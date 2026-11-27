using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.DAL;

public class BookingRepository : IBookingRepository
{
    private readonly MyDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(MyDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Booking>?> GetAllBookingsAsync()
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .OrderByDescending(b => b.Start)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to get all bookings");
            return null;
        }
    }

    public async Task<Booking?> GetBookingByIdAsync(int id)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to get booking {BookingId}", id);
            return null;
        }
    }

    public async Task<IEnumerable<Booking>?> GetBookingsByUserIdAsync(string userId)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.Room)
                .Include(b => b.User)
                .Where(b => b.UserId == userId)
                .OrderBy(b => b.Start)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to get bookings for user {UserId}", userId);
            return null;
        }
    }

    // Bookings for one room that have not ended yet, earliest first
    public async Task<IEnumerable<Booking>?> GetUpcomingBookingsByRoomIdAsync(int roomId)
    {
        try
        {
            return await _context.Bookings
                .Include(b => b.User)
                .Where(b => b.RoomId == roomId && b.End > DateTime.Now)
                .OrderBy(b => b.Start)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to get upcoming bookings for room {RoomId}", roomId);
            return null;
        }
    }

    // True if the room already has a booking that overlaps the given time slot
    public async Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, int? excludeBookingId = null)
    {
        try
        {
            return await _context.Bookings.AnyAsync(b =>
                b.RoomId == roomId &&
                (excludeBookingId == null || b.Id != excludeBookingId) &&
                b.Start < end && start < b.End);
        }
        catch (Exception ex)
        {
            // If the check fails we treat it as a conflict, so no double booking can slip through
            _logger.LogError(ex, "[BookingRepository] Failed to check conflicts for room {RoomId}", roomId);
            return true;
        }
    }

    public async Task<bool> CreateBookingAsync(Booking booking)
    {
        try
        {
            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to create booking for room {RoomId}", booking.RoomId);
            return false;
        }
    }

    public async Task<bool> UpdateBookingAsync(Booking booking)
    {
        try
        {
            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to update booking {BookingId}", booking.Id);
            return false;
        }
    }

    public async Task<bool> DeleteBookingAsync(int id)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                _logger.LogWarning("[BookingRepository] Booking {BookingId} not found for deletion", id);
                return false;
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to delete booking {BookingId}", id);
            return false;
        }
    }
}
