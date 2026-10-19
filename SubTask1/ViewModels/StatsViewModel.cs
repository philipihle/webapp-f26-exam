using webapp_f26_exam.Models;

namespace webapp_f26_exam.ViewModels;

public class StatsViewModel
{
    public int TotalUsers { get; set; }
    public int AdminUsers { get; set; }
    public int StudentUsers { get; set; }

    public int TotalRooms { get; set; }
    public int AvailableRooms { get; set; }

    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public double UtilizationRate { get; set; }

    public List<TopRoomStat> TopRooms { get; set; } = new();
    public List<Booking> RecentBookings { get; set; } = new();
}

public class TopRoomStat
{
    public string RoomName { get; set; } = string.Empty;
    public string RoomCode { get; set; } = string.Empty;
    public int BookingCount { get; set; }
}
