using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class RoomDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20, ErrorMessage = "Room code can be at most 20 characters.")]
    public string RoomCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50, ErrorMessage = "Building can be at most 50 characters.")]
    public string Building { get; set; } = string.Empty;

    [Range(0, 20, ErrorMessage = "Floor must be between 0 and 20.")]
    public int Floor { get; set; }

    [Range(1, 50, ErrorMessage = "Capacity must be between 1 and 50.")]
    public int Capacity { get; set; }

    [StringLength(30, ErrorMessage = "Room type can be at most 30 characters.")]
    public string? RoomType { get; set; }

    [StringLength(500, ErrorMessage = "Description can be at most 500 characters.")]
    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public List<string> Amenities { get; set; } = new();
}
