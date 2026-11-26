using Microsoft.EntityFrameworkCore;
using api.Models;

namespace api.DAL;

public class RoomRepository : IRoomRepository
{
    private readonly MyDbContext _context;
    private readonly ILogger<RoomRepository> _logger;

    public RoomRepository(MyDbContext context, ILogger<RoomRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>?> GetAllRoomsAsync()
    {
        try
        {
            return await _context.Rooms.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all rooms");
            return null;
        }
    }

    public async Task<IEnumerable<Room>?> GetActiveRoomsAsync()
    {
        try
        {
            return await _context.Rooms
                .Where(r => r.IsActive)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active rooms");
            return null;
        }
    }

    public async Task<Room?> GetRoomByIdAsync(int id)
    {
        try
        {
            return await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get room {RoomId}", id);
            return null;
        }
    }

    public async Task<bool> CreateRoomAsync(Room room)
    {
        try
        {
            await _context.Rooms.AddAsync(room);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create room {RoomCode}", room.RoomCode);
            return false;
        }
    }

    public async Task<bool> UpdateRoomAsync(Room room)
    {
        try
        {
            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update room {RoomId}", room.Id);
            return false;
        }
    }

    public async Task<bool> DeleteRoomAsync(int id)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                _logger.LogWarning("Room {RoomId} not found for deletion", id);
                return false;
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete room {RoomId}", id);
            return false;
        }
    }
}
