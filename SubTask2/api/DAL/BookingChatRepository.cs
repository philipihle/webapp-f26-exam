using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.DAL;

public class BookingChatRepository : IBookingChatRepository
{
    private readonly MyDbContext _context;
    private readonly ILogger<BookingChatRepository> _logger;

    public BookingChatRepository(MyDbContext context, ILogger<BookingChatRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<BookingChat>?> GetChatsByBookingIdAsync(int bookingId)
    {
        try
        {
            return await _context.BookingChats
                .Include(chat => chat.User)
                .Where(chat => chat.BookingId == bookingId)
                .OrderByDescending(chat => chat.Timestamp)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingChatRepository] Failed to get chats for booking {BookingId}", bookingId);
            return null;
        }
    }

    public async Task<bool> AddChatAsync(BookingChat chat)
    {
        try
        {
            _context.BookingChats.Add(chat);
            await _context.SaveChangesAsync();
            await _context.Entry(chat).Reference(c => c.User).LoadAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingChatRepository] Failed to add chat for booking {BookingId}", chat.BookingId);
            return false;
        }
    }

    public async Task<BookingChat?> GetChatByIdAsync(int chatId)
    {
        try
        {
            return await _context.BookingChats
                .Include(chat => chat.User)
                .FirstOrDefaultAsync(chat => chat.Id == chatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingChatRepository] Failed to get chat {ChatId}", chatId);
            return null;
        }
    }

    public async Task<bool> DeleteChatAsync(int chatId)
    {
        try
        {
            var chat = await GetChatByIdAsync(chatId);
            if (chat == null) return false;

            _context.BookingChats.Remove(chat);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BookingChatRepository] Failed to delete chat {ChatId}", chatId);
            return false;
        }
    }
}