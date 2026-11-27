using api.Models;

namespace api.DAL;

public interface IBookingParticipantRepository
{
    // Read methods return null if the database call fails
    Task<IEnumerable<BookingParticipant>?> GetParticipantsByBookingIdAsync(int bookingId);
    Task<IEnumerable<Booking>?> GetJoinedBookingsByUserIdAsync(string userId);
    Task<bool> AddParticipantAsync(int bookingId, string userId);
    Task<bool> RemoveParticipantAsync(int bookingId, string userId);
}
