using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.DAL;
using webapp_f26_exam.ViewModels;

namespace webapp_f26_exam.Controllers;

public class RoomsController : Controller
{
    private readonly IRoomRepository _roomRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<RoomsController> _logger;

    public RoomsController(IRoomRepository roomRepository, IBookingRepository bookingRepository, ILogger<RoomsController> logger)
    {
        _roomRepository = roomRepository;
        _bookingRepository = bookingRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? building, string? capacity, string? query, List<string>? amenities)
    {
        var allDbRooms = (await _roomRepository.GetActiveRoomsAsync()).ToList();
        var dbRooms = allDbRooms.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(building) && building != "All")
        {
            dbRooms = dbRooms.Where(r => r.Building.Contains(building, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(capacity) && capacity != "All")
        {
            if (capacity == "1-2") dbRooms = dbRooms.Where(r => r.Capacity <= 2);
            else if (capacity == "3-6") dbRooms = dbRooms.Where(r => r.Capacity >= 3 && r.Capacity <= 6);
            else if (capacity == "7+") dbRooms = dbRooms.Where(r => r.Capacity >= 7);
        }

        // Dynamically collect all available distinct amenities and buildings from active rooms
        var allAvailableBuildings = allDbRooms
            .Select(r => r.Building)
            .Where(b => !string.IsNullOrWhiteSpace(b))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(b => b)
            .ToList();

        var allAvailableAmenities = allDbRooms
            .SelectMany(r => r.Amenities ?? new List<string>())
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(a => a)
            .ToList();

        if (!allAvailableAmenities.Any())
        {
            allAvailableAmenities = new List<string> { "Screen", "Whiteboard", "Camera", "Quiet zone", "Projector" };
        }

        var selectedAmenities = amenities ?? new List<string>();

        // Multi-select amenity filtering: room must include all selected amenities
        if (selectedAmenities.Any())
        {
            dbRooms = dbRooms.Where(r =>
                r.Amenities != null &&
                selectedAmenities.All(sel => r.Amenities.Any(a => a.Contains(sel, StringComparison.OrdinalIgnoreCase)))
            );
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.Trim();
            dbRooms = dbRooms.Where(r =>
                r.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.RoomCode.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(q, StringComparison.OrdinalIgnoreCase)
            );
        }

        var filteredRooms = dbRooms.ToList();

        var now = DateTime.Now;
        var activeBookings = (await _bookingRepository.GetActiveBookingsAsync(now)).ToList();

        var roomItems = filteredRooms.Select(r =>
        {
            var currentBooking = activeBookings.FirstOrDefault(b => b.RoomId == r.Id);
            var isAvailable = currentBooking == null;

            return new RoomItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                RoomCode = r.RoomCode,
                Building = r.Building,
                Floor = r.Floor,
                Capacity = r.Capacity,
                Description = r.Description,
                ImageUrl = $"/media/{r.RoomCode}.jpg",
                IsAvailableNow = isAvailable,
                StatusText = isAvailable ? "Available now" : "In use",
                AvailabilityDetail = isAvailable ? "Ready for immediate booking" : $"Occupied until {currentBooking!.End:HH:mm}",
                Amenities = r.Amenities ?? new List<string>()
            };
        }).ToList();

        var viewModel = new RoomsListViewModel
        {
            SelectedBuilding = building ?? "All",
            SelectedCapacity = capacity ?? "All",
            SearchQuery = query,
            SelectedAmenities = selectedAmenities,
            AvailableAmenities = allAvailableAmenities,
            AvailableBuildings = allAvailableBuildings,
            Rooms = roomItems
        };

        _logger.LogInformation("Loaded {Count} rooms from database for catalog view.", roomItems.Count);
        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Details(int id)
    {
        return RedirectToAction("Create", "Booking", new { roomId = id });
    }
}
