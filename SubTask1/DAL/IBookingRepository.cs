using webapp_f26_exam.Models;

namespace webapp_f26_exam.DAL;

public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetAllBookingsAsync();
    Task<Booking?> GetBookingByIdAsync(int id);
    Task<IEnumerable<Booking>> GetBookingsByUserIdAsync(string userId);
    Task<IEnumerable<Booking>> GetActiveBookingsAsync(DateTime currentTime);
    Task<bool> HasConflictAsync(int roomId, DateTime start, DateTime end, int? excludeBookingId = null);
    Task<bool> CreateBookingAsync(Booking booking);
    Task<bool> DeleteBookingAsync(int id);
    Task<bool> DeleteBookingsByUserIdAsync(string userId);
}
