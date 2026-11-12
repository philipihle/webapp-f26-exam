using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.DAL;
using webapp_f26_exam.ViewModels;

namespace webapp_f26_exam.Controllers;

[Authorize]
public class RoomsController : Controller
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;

    public RoomsController(IRoomRepository roomRepository, IBookingRepository bookingRepository)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? building,
        string? roomType,
        List<string>? amenities,
        DateTime? start,
        DateTime? end,
        string? date = null,
        string? from = null,
        string? to = null)
    {
        var rooms = (await _roomRepository.GetActiveRoomsAsync()).ToList();

        var availableBuildings = rooms
            .Select(r => r.Building)
            .Distinct()
            .OrderBy(b => b)
            .ToList();

        var availableRoomTypes = rooms
            .Select(r => r.RoomType)
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var availableAmenities = rooms
            .SelectMany(r => r.Amenities)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        var filteredRooms = rooms.AsEnumerable();

        if (!string.IsNullOrEmpty(building) && building != "All")
        {
            filteredRooms = filteredRooms.Where(r => r.Building == building);
        }

        if (!string.IsNullOrEmpty(roomType) && roomType != "All")
        {
            filteredRooms = filteredRooms.Where(r => r.RoomType == roomType);
        }

        var selectedAmenities = amenities ?? new List<string>();
        if (selectedAmenities.Any())
        {
            filteredRooms = filteredRooms.Where(r => selectedAmenities.All(a => r.Amenities.Contains(a)));
        }

        DateTime filterStart;
        DateTime filterEnd;

        if (!string.IsNullOrEmpty(date))
        {
            var fromStr = !string.IsNullOrEmpty(from) ? from : DateTime.Now.ToString("HH:00");
            var toStr = !string.IsNullOrEmpty(to) ? to : DateTime.Now.AddHours(1).ToString("HH:00");

            if (!DateTime.TryParse($"{date}T{fromStr}", out filterStart))
            {
                filterStart = start ?? DateTime.Now;
            }

            if (!DateTime.TryParse($"{date}T{toStr}", out filterEnd))
            {
                filterEnd = end ?? filterStart.AddHours(1);
            }
        }
        else
        {
            filterStart = start ?? DateTime.Now;
            filterEnd = end ?? DateTime.Now.AddHours(1);
        }

        var bookings = await _bookingRepository.GetAllBookingsAsync();
        var conflictingBookings = bookings.Where(b => b.Start < filterEnd && b.End > filterStart).ToList();

        var roomItems = filteredRooms.Select(r =>
        {
            var isAvailable = !conflictingBookings.Any(b => b.RoomId == r.Id);

            return new RoomItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                RoomCode = r.RoomCode,
                Building = r.Building,
                Floor = r.Floor,
                Capacity = r.Capacity,
                RoomType = r.RoomType ?? string.Empty,
                Description = r.Description,
                IsAvailableNow = isAvailable,
                Amenities = r.Amenities ?? new List<string>()
            };
        }).ToList();

        var viewModel = new RoomsListViewModel
        {
            SelectedBuilding = building ?? "All",
            SelectedRoomType = roomType ?? "All",
            SelectedAmenities = selectedAmenities,
            AvailableAmenities = availableAmenities,
            AvailableBuildings = availableBuildings,
            AvailableRoomTypes = availableRoomTypes,
            Rooms = roomItems,
            Start = filterStart,
            End = filterEnd
        };

        return View(viewModel);
    }
}
