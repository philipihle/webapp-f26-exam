using webapp_f26_exam.Models;

namespace webapp_f26_exam.ViewModels;

public class BookingViewModel
{
    public Booking Booking { get; set; } = new();
    public Room Room { get; set; } = new();
}