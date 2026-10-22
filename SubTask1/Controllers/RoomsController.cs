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
    public async Task<IActionResult> Index(string? building, string? capacity, List<string>? amenities, DateTime? start, DateTime? end)
    {
        var rooms = (await _roomRepository.GetActiveRoomsAsync()).ToList();

        var availableBuildings = rooms
            .Select(r => r.Building)
            .Distinct()
            .OrderBy(b => b)
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

        if (!string.IsNullOrEmpty(capacity) && capacity != "All")
        {
            filteredRooms = capacity switch
            {
                "1-2" => filteredRooms.Where(r => r.Capacity <= 2),
                "3-6" => filteredRooms.Where(r => r.Capacity >= 3 && r.Capacity <= 6),
                "7+" => filteredRooms.Where(r => r.Capacity >= 7),
                _ => filteredRooms
            };
        }

        var selectedAmenities = amenities ?? new List<string>();
        if (selectedAmenities.Any())
        {
            filteredRooms = filteredRooms.Where(r => selectedAmenities.All(a => r.Amenities.Contains(a)));
        }

        var filterStart = start ?? DateTime.Now;
        var filterEnd = end ?? DateTime.Now.AddHours(1);

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
                Description = r.Description,
                IsAvailableNow = isAvailable,
                Amenities = r.Amenities ?? new List<string>()
            };
        }).ToList();

        var viewModel = new RoomsListViewModel
        {
            SelectedBuilding = building ?? "All",
            SelectedCapacity = capacity ?? "All",
            SelectedAmenities = selectedAmenities,
            AvailableAmenities = availableAmenities,
            AvailableBuildings = availableBuildings,
            Rooms = roomItems,
            Start = filterStart,
            End = filterEnd
        };

        return View(viewModel);
    }
}
