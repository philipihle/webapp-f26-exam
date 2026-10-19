using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.Models;

namespace webapp_f26_exam.DAL;

public class BookingRepository : IBookingRepository
{
    private readonly MyDbContext _context;
    private readonly ILogger<BookingRepository> _logger;

    public BookingRepository(MyDbContext context, ILogger<BookingRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Booking>> GetAllBookingsAsync()
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .OrderByDescending(b => b.Start)
            .ToListAsync();
    }

    public async Task<Booking?> GetBookingByIdAsync(int id)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .Where(b => b.UserId == userId)
            .OrderBy(b => b.Start)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetActiveBookingsAsync(DateTime currentTime)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Where(b => b.Start <= currentTime && b.End > currentTime)
            .ToListAsync();
    }

    public async Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, int? excludeBookingId = null)
    {
        return await _context.Bookings.AnyAsync(b =>
            b.RoomId == roomId &&
            (excludeBookingId == null || b.Id != excludeBookingId) &&
            b.Start < end && start < b.End);
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
            _logger.LogError(ex, "Failed to create booking for room {RoomId}", booking.RoomId);
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
                return false;
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete booking {BookingId}", id);
            return false;
        }
    }

    public async Task<bool> DeleteBookingsByUserIdAsync(string userId)
    {
        try
        {
            var userBookings = await _context.Bookings
                .Where(b => b.UserId == userId)
                .ToListAsync();

            if (userBookings.Any())
            {
                _context.Bookings.RemoveRange(userBookings);
                await _context.SaveChangesAsync();
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete bookings for user {UserId}", userId);
            return false;
        }
    }
}
