using Microsoft.AspNetCore.Mvc;
using api.DAL;
using api.DTOs;
using api.Models;

namespace api.Controllers;

// REST API for rooms, used by the React frontend. Base URL: /api/roomapi
[ApiController]
[Route("api/[controller]")]
public class RoomAPIController : ControllerBase
{
    private readonly IRoomRepository _roomRepository;
    private readonly ILogger<RoomAPIController> _logger;

    public RoomAPIController(IRoomRepository roomRepository, ILogger<RoomAPIController> logger)
    {
        _roomRepository = roomRepository;
        _logger = logger;
    }

    //HTTP GET endpoint to retrieve a list of all rooms.
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

    //HTTP POST endpoint to create a new room. The room data is sent in the request body as JSON and mapped to a RoomDto object. The method creates a new Room entity
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] RoomDto roomDto)
    {
        if (roomDto == null)
        {
            return BadRequest("Room cannot be null");
        }
        var newRoom = new Room
        {
            Name = roomDto.Name,
            RoomCode = roomDto.RoomCode,
            Building = roomDto.Building,
            Floor = roomDto.Floor,
            Capacity = roomDto.Capacity,
            RoomType = roomDto.RoomType,
            Description = roomDto.Description,
            IsActive = roomDto.IsActive,
            Amenities = roomDto.Amenities
        };
        bool returnOk = await _roomRepository.CreateRoomAsync(newRoom);
        if (returnOk)
            return CreatedAtAction(nameof(RoomList), new { id = newRoom.Id }, newRoom);

        //throws 500 if fails
        _logger.LogWarning("[RoomAPIController] Room creation failed {@room}", newRoom);
        return StatusCode(500, "Internal server error");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoom(int id)
    {
        var room = await _roomRepository.GetRoomByIdAsync(id);
        if (room == null)
        {
            _logger.LogError("[RoomAPIController] Room not found for the RoomId {RoomId:0000}", id);
            return NotFound("Room not found for the RoomId");
        }
        return Ok(room);
    }
}
