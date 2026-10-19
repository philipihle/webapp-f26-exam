using webapp_f26_exam.Models;

namespace webapp_f26_exam.DAL;

public interface IRoomRepository
{
    Task<IEnumerable<Room>> GetAllRoomsAsync();
    Task<IEnumerable<Room>> GetActiveRoomsAsync();
    Task<Room?> GetRoomByIdAsync(int id);
    Task<bool> CreateRoomAsync(Room room);
    Task<bool> UpdateRoomAsync(Room room);
    Task<bool> DeleteRoomAsync(int id);
}
