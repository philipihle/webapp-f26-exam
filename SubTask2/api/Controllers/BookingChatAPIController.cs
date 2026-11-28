using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.DAL;
using api.DTOs;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingChatAPIController : ControllerBase
{
    private readonly IBookingChatRepository _bookingChatRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<BookingChatAPIController> _logger;

    public BookingChatAPIController(
        IBookingChatRepository bookingChatRepository,
        IBookingRepository bookingRepository,
        ILogger<BookingChatAPIController> logger)
    {
        _bookingChatRepository = bookingChatRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    [HttpGet("booking/{bookingId}")]
    public async Task<IActionResult> GetChatsByBookingId(int bookingId)
    {
        var chats = await _bookingChatRepository.GetChatsByBookingIdAsync(bookingId);
        if (chats == null)
        {
            _logger.LogError("[BookingChatAPIController] Failed to retrieve chats for booking {BookingId}", bookingId);
            return StatusCode(500, "Failed to retrieve chat messages.");
        }

        return Ok(chats.Select(ToDto));
    }

    [HttpPost("booking/{bookingId}")]
    public async Task<IActionResult> AddChat(int bookingId, [FromBody] BookingChatDto chatDto)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
            return Unauthorized();

        var booking = await _bookingRepository.GetBookingByIdAsync(bookingId);
        if (booking == null)
            return NotFound("Booking not found.");

        var chat = new BookingChat
        {
            BookingId = bookingId,
            UserId = CurrentUserId,
            Message = chatDto.Message.Trim(),
            Timestamp = DateTime.UtcNow
        };

        var result = await _bookingChatRepository.AddChatAsync(chat);
        if (!result)
        {
            _logger.LogError("[BookingChatAPIController] Failed to add chat for booking {BookingId}", bookingId);
            return StatusCode(500, "Failed to save chat message.");
        }

        return Ok(ToDto(chat));
    }

    private static BookingChatDto ToDto(BookingChat chat) => new()
    {
        Id = chat.Id,
        BookingId = chat.BookingId,
        UserId = chat.UserId,
        UserName = chat.User?.UserName ?? string.Empty,
        Message = chat.Message,
        Timestamp = chat.Timestamp
    };

    [HttpDelete("{chatId}")]
    public async Task<IActionResult> DeleteChat(int chatId)
    {
        var chat = await _bookingChatRepository.GetChatByIdAsync(chatId);
        if (chat == null)
            return NotFound("Chat message not found.");

        if (chat.UserId != CurrentUserId && !User.IsInRole("Admin"))
            return Forbid();

        var result = await _bookingChatRepository.DeleteChatAsync(chatId);
        if (!result)
        {
            _logger.LogError("[BookingChatAPIController] Failed to delete chat {ChatId}", chatId);
            return StatusCode(500, "Failed to delete chat message.");
        }

        return NoContent();
    }
}
