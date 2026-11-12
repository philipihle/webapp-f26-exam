using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace webapp_f26_exam.Models;

public class BookingParticipant
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public Booking? Booking { get; set; }

    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public IdentityUser? User { get; set; }

    public DateTime JoinedAtDateTime { get; set; } = DateTime.Now;
}

