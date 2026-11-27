using api.Models;

namespace api.DAL;

public interface IBookingRepository
{
    // Read methods return null if the database call fails
    Task<IEnumerable<Booking>?> GetAllBookingsAsync();
    Task<Booking?> GetBookingByIdAsync(int id);
    Task<IEnumerable<Booking>?> GetBookingsByUserIdAsync(string userId);
    Task<IEnumerable<Booking>?> GetUpcomingBookingsByRoomIdAsync(int roomId);
    Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, int? excludeBookingId = null);
    Task<bool> CreateBookingAsync(Booking booking);
    Task<bool> UpdateBookingAsync(Booking booking);
    Task<bool> DeleteBookingAsync(int id);
}