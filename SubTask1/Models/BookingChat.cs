namespace webapp_f26_exam.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

public class BookingChat
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking? Booking { get; set; }
    public string UserId { get; set; } = string.Empty;
    public IdentityUser? User { get; set; }

    [Required]
    [StringLength(500, ErrorMessage = "Message cannot exceed 500 characters.")]
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
