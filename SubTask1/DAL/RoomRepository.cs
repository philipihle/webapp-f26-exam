using Microsoft.EntityFrameworkCore;
using webapp_f26_exam.Models;

namespace webapp_f26_exam.DAL;

public class RoomRepository : IRoomRepository
{
    private readonly MyDbContext _context;
    private readonly ILogger<RoomRepository> _logger;

    public RoomRepository(MyDbContext context, ILogger<RoomRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Room>> GetAllRoomsAsync()
    {
        return await _context.Rooms.ToListAsync();
    }

    public async Task<IEnumerable<Room>> GetActiveRoomsAsync()
    {
        return await _context.Rooms
            .Where(r => r.IsActive)
            .ToListAsync();
    }

    public async Task<Room?> GetRoomByIdAsync(int id)
    {
        return await _context.Rooms.FirstOrDefaultAsync(r => r.Id == id);
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
