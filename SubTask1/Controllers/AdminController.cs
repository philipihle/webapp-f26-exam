using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webapp_f26_exam.DAL;
using webapp_f26_exam.ViewModels;
using webapp_f26_exam.Models;

namespace webapp_f26_exam.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly MyDbContext _context;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<IdentityUser> userManager,
        MyDbContext context,
        IBookingRepository bookingRepository,
        IRoomRepository roomRepository,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _context = context;
        _bookingRepository = bookingRepository;
        _roomRepository = roomRepository;
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Users()
    {
        var allUsers = await _userManager.Users.ToListAsync();
        var adminUserIds = (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id).ToHashSet();

        var userList = allUsers.Select(user => new UserItemViewModel
        {
            Id = user.Id,
            Name = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = adminUserIds.Contains(user.Id) ? "Admin" : "Student"
        }).ToList();

        return View(new UsersViewModel { Users = userList });
    }

    [HttpGet]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.Now;

        var totalUsers = await _userManager.Users.CountAsync();
        var totalRooms = await _context.Rooms.CountAsync();

        var currentlyOccupiedRoomIds = await _context.Bookings
            .Where(b => b.Start <= now && b.End > now)
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        var availableRooms = await _context.Rooms
            .CountAsync(r => r.IsActive && !currentlyOccupiedRoomIds.Contains(r.Id));

        var totalBookings = await _context.Bookings.CountAsync();
        var activeBookings = await _context.Bookings.CountAsync(b => b.End >= now);

        var topRooms = await _context.Bookings
            .Include(b => b.Room)
            .GroupBy(b => new { b.RoomId, Name = b.Room != null ? b.Room.Name : "Room " + b.RoomId, Code = b.Room != null ? b.Room.RoomCode : "-" })
            .Select(g => new TopRoomStat
            {
                RoomName = g.Key.Name,
                RoomCode = g.Key.Code,
                BookingCount = g.Count()
            })
            .OrderByDescending(r => r.BookingCount)
            .Take(5)
            .ToListAsync();

        var recentBookings = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .OrderByDescending(b => b.Start)
            .Take(6)
            .ToListAsync();

        var model = new StatsViewModel
        {
            TotalUsers = totalUsers,
            TotalRooms = totalRooms,
            AvailableRooms = availableRooms,
            TotalBookings = totalBookings,
            ActiveBookings = activeBookings,
            TopRooms = topRooms,
            RecentBookings = recentBookings
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Roles()
    {
        var allUsers = await _userManager.Users.ToListAsync();
        var adminUserIds = (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Id).ToHashSet();

        var userRoleList = allUsers.Select(user => new UserRoleItemViewModel
        {
            UserId = user.Id,
            Name = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            CurrentRole = adminUserIds.Contains(user.Id) ? "Admin" : "Student"
        }).ToList();

        return View(new RolesViewModel { Users = userRoleList });
    }

    [HttpPost]
    public async Task<IActionResult> SetUserRole(string userId, string newRole)
    {
        // Only allow switching between "Student" and "Admin"
        if (newRole != "Admin" && newRole != "Student")
        {
            _logger.LogWarning("Invalid role assignment attempt: {Role}", newRole);
            TempData["ErrorMessage"] = "Invalid role. Only 'Student' and 'Admin' roles are permitted.";
            return RedirectToAction("Roles");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("User with ID {UserId} was not found for role update.", userId);
            TempData["ErrorMessage"] = "User was not found.";
            return RedirectToAction("Roles");
        }

        // Prevent admin from removing their own Admin privileges
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == currentUserId && newRole != "Admin")
        {
            _logger.LogWarning("Admin {AdminEmail} tried to remove their own admin role.", User.Identity?.Name);
            TempData["ErrorMessage"] = "You cannot remove your own Admin role.";
            return RedirectToAction("Roles");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(newRole))
        {
            TempData["SuccessMessage"] = $"Role for {user.Email} is already '{newRole}'.";
            return RedirectToAction("Roles");
        }

        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                _logger.LogError("Failed removing existing roles for user {Email}.", user.Email);
                TempData["ErrorMessage"] = "Failed to update user role.";
                return RedirectToAction("Roles");
            }
        }

        var addResult = await _userManager.AddToRoleAsync(user, newRole);
        if (addResult.Succeeded)
        {
            _logger.LogInformation("Admin {Admin} changed role for user {Email} to {NewRole}.", User.Identity?.Name, user.Email, newRole);
            TempData["SuccessMessage"] = $"Role for {user.Email} successfully changed to '{newRole}'.";
        }
        else
        {
            _logger.LogError("Failed assigning role {NewRole} to user {Email}.", newRole, user.Email);
            TempData["ErrorMessage"] = "Failed to assign role to user.";
        }

        return RedirectToAction("Roles");
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User was not found.";
            return RedirectToAction("Users");
        }

        var viewModel = new UserEditViewModel
        {
            UserId = user.Id,
            Name = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> EditUser(UserEditViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        var user = await _userManager.FindByIdAsync(viewModel.UserId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User was not found.";
            return RedirectToAction("Users");
        }

        user.UserName = viewModel.Name;
        user.Email = viewModel.Email;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = "User updated successfully.";
            return RedirectToAction("Users");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        if (userId == _userManager.GetUserId(User))
        {
            TempData["ErrorMessage"] = "You cannot delete your own account.";
            return RedirectToAction("Users");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User was not found.";
            return RedirectToAction("Users");
        }

        await _bookingRepository.DeleteBookingsByUserIdAsync(user.Id);

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["SuccessMessage"] = $"User {user.Email} successfully deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete user.";
        }

        return RedirectToAction("Users");
    }

    [HttpGet]
    public async Task<IActionResult> Rooms(string? building, string? roomType, bool? isActive)
    {
        var allRooms = (await _roomRepository.GetAllRoomsAsync()).ToList();
        var filteredRooms = allRooms.AsEnumerable();
        if (!string.IsNullOrEmpty(building) && building != "All")
        {
            filteredRooms = filteredRooms.Where(r => r.Building == building);
        }
        if (!string.IsNullOrEmpty(roomType) && roomType != "All")
        {
            filteredRooms = filteredRooms.Where(r => r.RoomType == roomType);
        }
        if (isActive.HasValue)
        {
            filteredRooms = filteredRooms.Where(r => r.IsActive == isActive.Value);
        }

        var model = new RoomsListViewModel
        {
            SelectedBuilding = building ?? "All",
            SelectedRoomType = roomType ?? "All",
            SelectedIsActive = isActive,
            AvailableBuildings = allRooms.Select(r => r.Building).Distinct().OrderBy(b => b).ToList(),
            AvailableRoomTypes = allRooms.Select(r => r.RoomType).Where(t => !string.IsNullOrEmpty(t)).Select(t => t!).Distinct().OrderBy(t => t).ToList(),
            Rooms = filteredRooms.OrderBy(r => r.Building).ThenBy(r => r.RoomCode).Select(r => new RoomItemViewModel
            {
                Id = r.Id,
                Name = r.Name,
                Building = r.Building,
                RoomCode = r.RoomCode,
                Floor = r.Floor,
                Capacity = r.Capacity,
                RoomType = r.RoomType ?? string.Empty,
                IsActive = r.IsActive
            }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateRoom()
    {
        var model = new RoomFormViewModel
        {
            AvailableAmenities = await GetAvailableAmenitiesAsync()
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRoom(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = await GetAvailableAmenitiesAsync();
            return View(model);
        }

        var existingRooms = await _roomRepository.GetAllRoomsAsync();
        var room = new Room
        {
            RoomCode = GenerateRoomCode(model.Building, existingRooms)
        };
        ApplyRoomForm(room, model);

        var success = await _roomRepository.CreateRoomAsync(room);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? $"Room {room.RoomCode} created."
            : "Failed to create room.";

        return RedirectToAction("Rooms");
    }

    [HttpGet]
    public async Task<IActionResult> EditRoom(int id)
    {
        var room = await _roomRepository.GetRoomByIdAsync(id);
        if (room == null)
        {
            TempData["ErrorMessage"] = "Room was not found.";
            return RedirectToAction("Rooms");
        }

        var model = new RoomFormViewModel
        {
            Id = room.Id,
            Name = room.Name,
            RoomCode = room.RoomCode,
            Building = room.Building,
            Floor = room.Floor,
            Capacity = room.Capacity,
            RoomType = room.RoomType ?? string.Empty,
            Description = room.Description,
            IsActive = room.IsActive,
            Amenities = room.Amenities,
            AvailableAmenities = await GetAvailableAmenitiesAsync()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> EditRoom(RoomFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = await GetAvailableAmenitiesAsync();
            return View(model);
        }

        var room = await _roomRepository.GetRoomByIdAsync(model.Id);
        if (room == null)
        {
            TempData["ErrorMessage"] = "Room was not found.";
            return RedirectToAction("Rooms");
        }

        ApplyRoomForm(room, model);

        var success = await _roomRepository.UpdateRoomAsync(room);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? $"Room {room.RoomCode} updated."
            : "Failed to update room.";

        return RedirectToAction("Rooms");
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var room = await _roomRepository.GetRoomByIdAsync(id);
        if (room == null)
        {
            TempData["ErrorMessage"] = "Room was not found.";
            return RedirectToAction("Rooms");
        }

        if (await _context.Bookings.AnyAsync(b => b.RoomId == id))
        {
            TempData["ErrorMessage"] = $"Cannot delete room {room.RoomCode} because it has existing bookings. Deactivate it instead.";
            return RedirectToAction("Rooms");
        }

        var success = await _roomRepository.DeleteRoomAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success
            ? $"Room {room.RoomCode} deleted."
            : $"Failed to delete room {room.RoomCode}.";

        return RedirectToAction("Rooms");
    }

    private async Task<List<string>> GetAvailableAmenitiesAsync()
    {
        var defaults = new[] { "Whiteboard", "Screen", "Projector", "Camera", "Quiet zone", "Smart-TV" };
        var fromDb = (await _roomRepository.GetAllRoomsAsync()).SelectMany(r => r.Amenities);
        return defaults.Concat(fromDb).Distinct().OrderBy(a => a).ToList();
    }

    // Generates a unique room code using the first letter of the building name, its street/address number, e.g. "Pilestredet 32" -> "P32", and a random 3-digit suffix.
    private static string GenerateRoomCode(string building, IEnumerable<Room> existingRooms)
    {
        var trimmed = building.Trim();
        var letter = char.ToUpperInvariant(trimmed[0]);
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        var prefix = $"{letter}{digits}";
        var existingCodes = existingRooms.Select(r => r.RoomCode).ToHashSet(StringComparer.OrdinalIgnoreCase);

        string code;
        do
        {
            code = $"{prefix}-{Random.Shared.Next(100, 1000)}";
        }
        while (existingCodes.Contains(code));

        return code;
    }

    private static void ApplyRoomForm(Room room, RoomFormViewModel model)
    {
        room.Name = model.Name.Trim();
        room.Building = model.Building.Trim();
        room.Floor = model.Floor;
        room.Capacity = model.Capacity;
        room.RoomType = model.RoomType;
        room.Description = model.Description?.Trim() ?? string.Empty;
        room.IsActive = model.IsActive;
        room.Amenities = model.Amenities ?? new List<string>();
    }
}