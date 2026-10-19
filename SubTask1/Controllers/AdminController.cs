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
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly MyDbContext _context;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        RoleManager<IdentityRole> roleManager,
        UserManager<IdentityUser> userManager,
        MyDbContext context,
        IBookingRepository bookingRepository,
        ILogger<AdminController> logger)
    {
        _roleManager = roleManager;
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
        var userList = new List<UserItemViewModel>();

        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new UserItemViewModel
            {
                Id = user.Id,
                Name = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Role = string.Join(", ", roles)
            });
        }

        var viewModel = new UsersViewModel
        {
            Users = userList
        };

        return View(viewModel);
    }

    public IActionResult StudyRooms()
    {
        return View();
    }

    public IActionResult Bookings()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Stats()
    {
        var now = DateTime.Now;

        // User statistics
        var allUsers = await _userManager.Users.ToListAsync();
        int adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
        int studentCount = allUsers.Count - adminCount;
        if (studentCount < 0) studentCount = 0;

        // Room statistics
        var totalRooms = await _context.Rooms.CountAsync();
        var activeRoomsCount = await _context.Rooms.CountAsync(r => r.IsActive);

        // Find rooms currently occupied right now (Start <= now < End)
        var currentlyOccupiedRoomIds = await _context.Bookings
            .Where(b => b.Start <= now && b.End > now)
            .Select(b => b.RoomId)
            .Distinct()
            .ToListAsync();

        var availableRooms = await _context.Rooms
            .CountAsync(r => r.IsActive && !currentlyOccupiedRoomIds.Contains(r.Id));

        // Booking statistics
        var totalBookings = await _context.Bookings.CountAsync();
        var activeBookings = await _context.Bookings.CountAsync(b => b.End >= now);

        // Calculate utilization rate (percentage of active rooms currently occupied)
        double utilizationRate = activeRoomsCount > 0
            ? Math.Round(((double)currentlyOccupiedRoomIds.Count / activeRoomsCount) * 100.0, 1)
            : 0.0;

        // Most popular rooms
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

        // Recent bookings activity
        var recentBookings = await _context.Bookings
            .Include(b => b.Room)
            .Include(b => b.User)
            .OrderByDescending(b => b.Start)
            .Take(6) // Eqvivalent to LIMIT 6 in SQL
            .ToListAsync();

        var model = new StatsViewModel
        {
            TotalUsers = allUsers.Count,
            AdminUsers = adminCount,
            StudentUsers = studentCount,
            TotalRooms = totalRooms,
            AvailableRooms = availableRooms,
            TotalBookings = totalBookings,
            ActiveBookings = activeBookings,
            UtilizationRate = utilizationRate,
            TopRooms = topRooms,
            RecentBookings = recentBookings
        };

        _logger.LogInformation("Stats dashboard accessed by {AdminUser}.", User.Identity?.Name);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Roles()
    {
        // Ensure standard roles exist in database
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Admin"));
            _logger.LogInformation("Created missing role 'Admin'.");
        }
        if (!await _roleManager.RoleExistsAsync("Student"))
        {
            await _roleManager.CreateAsync(new IdentityRole("Student"));
            _logger.LogInformation("Created missing role 'Student'.");
        }

        var allUsers = await _userManager.Users.ToListAsync();
        var userRoleList = new List<UserRoleItemViewModel>();
        int adminCount = 0;
        int studentCount = 0;

        foreach (var user in allUsers)
        {
            var userRoles = await _userManager.GetRolesAsync(user);
            string roleName = "Student";

            if (userRoles.Contains("Admin"))
            {
                roleName = "Admin";
                adminCount++;
            }
            else
            {
                studentCount++;
            }

            userRoleList.Add(new UserRoleItemViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? "No Email",
                CurrentRole = roleName
            });
        }

        var viewModel = new RolesViewModel
        {
            AdminCount = adminCount,
            StudentCount = studentCount,
            TotalUsers = allUsers.Count,
            Users = userRoleList
        };

        return View(viewModel);
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
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
        {
            _logger.LogError("Failed removing existing roles for user {Email}.", user.Email);
            TempData["ErrorMessage"] = "Could not update user roles.";
            return RedirectToAction("Roles");
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
            TempData["ErrorMessage"] = string.Join(", ", addResult.Errors.Select(e => e.Description));
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
            TempData["ErrorMessage"] = string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction("Users");
    }
}
