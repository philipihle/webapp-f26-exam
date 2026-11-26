using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class BookingDto
{
    public int Id { get; set; }

    [Required]
    public int RoomId { get; set; }

    // Filled in by the API when sending a booking to the frontend (not sent by the client)
    public string? RoomName { get; set; }
    public string? UserName { get; set; }

    [Required]
    public DateTime Start { get; set; }

    [Required]
    public DateTime End { get; set; }

    [Required]
    [StringLength(50, ErrorMessage = "Purpose can be at most 50 characters.")]
    public string Purpose { get; set; } = "Study";

    public bool JoinFlag { get; set; } = false;
}
