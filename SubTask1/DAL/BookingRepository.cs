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
            .Include(b => b.Participants)
                .ThenInclude(p => p.User)
            .OrderByDescending(b => b.Start)
            .ToListAsync();
    }

    public async Task<Booking?> GetBookingByIdAsync(int id)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .Include(b => b.Participants)
            .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(string userId)
    {
        return await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .Include(b => b.Participants)
                .ThenInclude(p => p.User)
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

    public async Task<bool> AddParticipantAsync(int bookingId, string userId)
    {
        try
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
            {
                _logger.LogWarning("[BookingRepository] Booking not found for id {BookingId}", bookingId);
                return false;
            }

            var alreadyJoined = await _context.BookingParticipants
                .AnyAsync(p => p.BookingId == bookingId && p.UserId == userId);
            if (alreadyJoined)
            {
                _logger.LogInformation("[BookingRepository] User {UserId} is already a participant in booking {BookingId}", userId, bookingId);
                return true;
            }

            var participant = new BookingParticipant
            {
                BookingId = bookingId,
                UserId = userId,
                JoinedAtDateTime = DateTime.Now
            };

            await _context.BookingParticipants.AddAsync(participant);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to add participant {UserId} to booking {BookingId}", userId, bookingId);
            return false;
        }
    }

    public async Task<bool> RemoveParticipantAsync(int bookingId, string userId)
    {
        try
        {
            var participant = await _context.BookingParticipants
                .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.UserId == userId);
            if (participant == null)
            {
                _logger.LogWarning("[BookingRepository] Participant {UserId} not found for booking {BookingId}", userId, bookingId);
                return false;
            }

            _context.BookingParticipants.Remove(participant);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to remove participant {UserId} from booking {BookingId}", userId, bookingId);
            return false;
        }
    }

    public async Task<IEnumerable<Booking>> GetJoinedBookingsByUserIdAsync(string userId)
    {
        try
        {
            return await _context.BookingParticipants
                .Where(p => p.UserId == userId)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Room)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.User)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Participants)
                        .ThenInclude(part => part.User)
                .Select(p => p.Booking!)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingRepository] Failed to get joined bookings for user {UserId}", userId);
            return Enumerable.Empty<Booking>();
        }
    }
}
