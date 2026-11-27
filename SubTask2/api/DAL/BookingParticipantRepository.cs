using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.DAL;

public class BookingParticipantRepository : IBookingParticipantRepository
{
    private readonly MyDbContext _context;
    private readonly ILogger<BookingParticipantRepository> _logger;

    public BookingParticipantRepository(MyDbContext context, ILogger<BookingParticipantRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<BookingParticipant>?> GetParticipantsByBookingIdAsync(int bookingId)
    {
        try
        {
            return await _context.BookingParticipants
                .Include(p => p.User)
                .Where(p => p.BookingId == bookingId)
                .OrderBy(p => p.JoinedAtDateTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingParticipantRepository] Failed to get participants for booking {BookingId}", bookingId);
            return null;
        }
    }

    // Bookings the user has joined (not bookings the user owns)
    public async Task<IEnumerable<Booking>?> GetJoinedBookingsByUserIdAsync(string userId)
    {
        try
        {
            return await _context.BookingParticipants
                .Where(p => p.UserId == userId)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.Room)
                .Include(p => p.Booking)
                    .ThenInclude(b => b!.User)
                .Select(p => p.Booking!)
                .OrderBy(b => b.Start)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingParticipantRepository] Failed to get joined bookings for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> AddParticipantAsync(int bookingId, string userId)
    {
        try
        {
            var alreadyJoined = await _context.BookingParticipants
                .AnyAsync(p => p.BookingId == bookingId && p.UserId == userId);
            if (alreadyJoined)
            {
                _logger.LogInformation("[BookingParticipantRepository] User {UserId} is already a participant in booking {BookingId}", userId, bookingId);
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
            _logger.LogError(ex, "[BookingParticipantRepository] Failed to add participant {UserId} to booking {BookingId}", userId, bookingId);
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
                _logger.LogWarning("[BookingParticipantRepository] Participant {UserId} not found for booking {BookingId}", userId, bookingId);
                return false;
            }

            _context.BookingParticipants.Remove(participant);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingParticipantRepository] Failed to remove participant {UserId} from booking {BookingId}", userId, bookingId);
            return false;
        }
    }
}