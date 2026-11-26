using api.Models;

namespace api.DAL;

public interface IRoomRepository
{
    // Read methods return null if the database call fails
    Task<IEnumerable<Room>?> GetAllRoomsAsync();
    Task<IEnumerable<Room>?> GetActiveRoomsAsync();
    Task<Room?> GetRoomByIdAsync(int id);
    Task<bool> CreateRoomAsync(Room room);
    Task<bool> UpdateRoomAsync(Room room);
    Task<bool> DeleteRoomAsync(int id);
}
