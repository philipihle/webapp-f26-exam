namespace webapp_f26_exam.Models;

public class Room
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int Capacity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public List<string> Amenities { get; set; } = new();
}
