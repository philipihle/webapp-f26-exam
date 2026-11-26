using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = "Admin")] // only admins can change rooms, as in SubTask1
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

    //HTTP GET endpoint to retrieve one room by its id. Returns 404 if the room does not exist.
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

    //HTTP PUT endpoint to update an existing room. The new room data is sent in the request body as JSON and mapped to a RoomDto object. Returns 404 if the room does not exist.
    [Authorize(Roles = "Admin")] // only admins can change rooms, as in SubTask1
    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] RoomDto roomDto)
    {
        if (roomDto == null)
        {
            return BadRequest("Room data cannot be null");
        }
        var existingRoom = await _roomRepository.GetRoomByIdAsync(id);
        if (existingRoom == null)
        {
            return NotFound("Room not found");
        }
        existingRoom.Name = roomDto.Name;
        existingRoom.RoomCode = roomDto.RoomCode;
        existingRoom.Building = roomDto.Building;
        existingRoom.Floor = roomDto.Floor;
        existingRoom.Capacity = roomDto.Capacity;
        existingRoom.RoomType = roomDto.RoomType;
        existingRoom.Description = roomDto.Description;
        existingRoom.IsActive = roomDto.IsActive;
        existingRoom.Amenities = roomDto.Amenities;
        bool updateSuccessful = await _roomRepository.UpdateRoomAsync(existingRoom);
        if (updateSuccessful)
        {
            return Ok(existingRoom);
        }

        //throws 500 if fails
        _logger.LogWarning("[RoomAPIController] Room update failed {@room}", existingRoom);
        return StatusCode(500, "Internal server error");
    }

    //HTTP DELETE endpoint to delete a room by its id. Returns 204 No Content if deleted, 400 if it fails.
    [Authorize(Roles = "Admin")] // only admins can change rooms, as in SubTask1
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        bool returnOk = await _roomRepository.DeleteRoomAsync(id);
        if (!returnOk)
        {
            _logger.LogError("[RoomAPIController] Room deletion failed for the RoomId {RoomId:0000}", id);
            return BadRequest("Room deletion failed");
        }
        return NoContent();
    }
}
