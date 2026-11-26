namespace webapp_f26_exam.ViewModels;

public class BookingItemViewModel
{
    public int Id { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public string Building { get; set; } = string.Empty;
    public int Floor { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public bool IsOrganizer { get; set; }
}

public class MyBookingsViewModel
{
    public List<BookingItemViewModel> UpcomingBookings { get; set; } = new();
    public List<BookingItemViewModel> PastBookings { get; set; } = new();
}
