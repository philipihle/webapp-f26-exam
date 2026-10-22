namespace webapp_f26_exam.ViewModels;

public class RoomItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public int Floor { get; set; }
    public int Capacity { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsAvailableNow { get; set; }
    public List<string> Amenities { get; set; } = new();
}

public class RoomsListViewModel
{
    public string? SelectedBuilding { get; set; }
    public string? SelectedCapacity { get; set; }
    public List<string> SelectedAmenities { get; set; } = new();
    public List<string> AvailableAmenities { get; set; } = new();
    public List<string> AvailableBuildings { get; set; } = new();
    public List<RoomItemViewModel> Rooms { get; set; } = new();
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
}
