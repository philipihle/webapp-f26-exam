using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class BookingChatDto
{
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    [Required]
    [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
    public string Message { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}