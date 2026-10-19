using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using webapp_f26_exam.DAL;
using webapp_f26_exam.Models;

namespace webapp_f26_exam.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IRoomRepository _roomRepository;

    public HomeController(ILogger<HomeController> logger, IRoomRepository roomRepository)
    {
        _logger = logger;
        _roomRepository = roomRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var rooms = await _roomRepository.GetActiveRoomsAsync();
        _logger.LogInformation("Home page accessed by {User}.", User.Identity?.Name ?? "Anonymous");
        return View(rooms);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
