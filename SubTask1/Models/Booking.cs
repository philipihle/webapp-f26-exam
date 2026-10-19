using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace webapp_f26_exam.Models;

public class Booking
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }

    public string UserId { get; set; } = string.Empty;
    [ForeignKey("UserId")]
    public IdentityUser? User { get; set; }

    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Purpose { get; set; } = string.Empty;
}
