using Microsoft.AspNetCore.Mvc;
using api.DAL;
using api.DTOs;

namespace api.Controllers;

// REST API for rooms, used by the React frontend. Base URL: /api/roomapi
[ApiController]
[Route("api/[controller]")]
public class RoomAPIController : ControllerBase // ControllerBase is enough for an API, no Views needed
{
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<RoomAPIController> _logger;

    public RoomAPIController(IRoomRepository roomRepository, ILogger<RoomAPIController> logger)
    {
        _roomRepository = roomRepository;
        _logger = logger;
    }

    [HttpGet("roomlist")]
    public async Task<IActionResult> RoomList()
    {
        var rooms = await _roomRepository.GetAllRoomsAsync();
        if (rooms == null)
        {
            _logger.LogError("[RoomAPIController] Room list not found while executing GetAllRoomsAsync()");
            return NotFound("Room list not found");
        }
        var roomDtos = rooms.Select(room => new RoomDto
        {
            Id = room.Id,
            Name = room.Name,
            RoomCode = room.RoomCode,
            Building = room.Building,
            Floor = room.Floor,
            Capacity = room.Capacity,
            RoomType = room.RoomType,
            Description = room.Description,
            IsActive = room.IsActive,
            Amenities = room.Amenities
        });
        return Ok(roomDtos);
    }
}
