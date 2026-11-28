using api.Models;

namespace api.DAL;

public interface IBookingChatRepository
{
    Task<IEnumerable<BookingChat>?> GetChatsByBookingIdAsync(int bookingId);
    Task<bool> AddChatAsync(BookingChat chat);
    Task<BookingChat?> GetChatByIdAsync(int chatId);
    Task<bool> DeleteChatAsync(int chatId);
}