using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace api.Models;

public class BookingChat
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    // The user who sent the message
    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public IdentityUser? User { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Required]
    [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
    public string Message { get; set; } = string.Empty;
}