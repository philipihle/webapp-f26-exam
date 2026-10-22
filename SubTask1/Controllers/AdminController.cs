using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webapp_f26_exam.DAL;
using webapp_f26_exam.ViewModels;

namespace webapp_f26_exam.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly MyDbContext _context;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        UserManager<IdentityUser> userManager,
        MyDbContext context,
        IBookingRepository bookingRepository,
        ILogger<AdminController> logger)
    {
        _userManager = userManager;
        _context = context;
        _bookingRepository = bookingRepository;
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
    [ValidateAntiForgeryToken]
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
    [ValidateAntiForgeryToken]
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
}
