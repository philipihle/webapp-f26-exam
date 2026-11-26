using webapp_f26_exam.Models;

namespace webapp_f26_exam.ViewModels;

public class BookingCreateViewModel
{
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Purpose { get; set; } = "Study";
    public bool JoinFlag { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    public List<Booking> UpcomingBookings { get; set; } = new();
}

public class BookingDetailsViewModel
{
    public Booking Booking { get; set; } = new();
    public string? CurrentUserId { get; set; }
    public bool IsOwner { get; set; }
    public bool IsParticipant { get; set; }
    public IEnumerable<BookingChat> ChatMessages { get; set; } = Enumerable.Empty<BookingChat>();
}
