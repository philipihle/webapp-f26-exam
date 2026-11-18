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
    Task<bool> UpdateBookingAsync(Booking booking);
    Task<IEnumerable<Booking>> GetJoinedBookingsByUserIdAsync(string userId);
    Task<bool> RemoveParticipantAsync(int bookingId, string userId);
    Task<bool> AddParticipantAsync(int bookingId, string userId);
    Task<IEnumerable<BookingChat>> GetMessagesByBookingIdAsync(int bookingId);
    Task<BookingChat?> GetMessageByIdAsync(int messageId);
    Task<bool> AddMessageAsync(BookingChat message);
    Task<bool> UpdateMessageAsync(BookingChat message);
    Task<bool> DeleteMessageAsync(int messageId);

}
