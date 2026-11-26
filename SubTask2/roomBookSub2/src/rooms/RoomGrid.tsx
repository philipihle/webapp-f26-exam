import { Button } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Room } from '../types/room';

interface RoomGridProps {
  rooms: Room[];
  onRoomDeleted?: (room: Room) => void; // only passed when the user is logged in
}

// Room cards, same markup and classes as Views/Rooms/Index.cshtml in SubTask1
export default function RoomGrid({ rooms, onRoomDeleted }: RoomGridProps) {
  return (
    <div className="row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4">
      {rooms.map(room => (
        <div className="col" key={room.id}>
          <div className="card h-100 border-0 shadow-sm rounded-4 overflow-hidden d-flex flex-column justify-content-between bg-white">
            <div>
              <div className="p-4 pb-2">
                <div className="d-flex justify-content-between align-items-center mb-3">
                  {room.isActive
                    ? <span className="badge bg-success">Active</span>
                    : <span className="badge bg-secondary">Inactive</span>}
                  <span className="text-muted small">{room.capacity} people</span>
                </div>

                <div className="mb-2">
                  <h5 className="fw-bold mb-0">{room.name}</h5>
                  {room.roomType && (
                    <div className="fw-semibold text-dark small">{room.roomType}</div>
                  )}
                  <small className="text-muted">{room.building} &bull; Floor {room.floor} - {room.roomCode}</small>
                </div>

                <p className="text-muted small mb-3">{room.description}</p>

                <div className="d-flex flex-wrap gap-1 mb-2">
                  {room.amenities.map(amenity => (
                    <span className="amenity-chip" key={amenity}>{amenity}</span>
                  ))}
                </div>
              </div>
            </div>

            {onRoomDeleted && (
              <div className="p-4 pt-0 d-flex gap-2">
                <Link to={`/roomupdate/${room.id}`} className="btn btn-outline-primary btn-sm w-100">Update</Link>
                <Button variant="outline-danger" size="sm" className="w-100" onClick={() => onRoomDeleted(room)}>
                  Delete
                </Button>
              </div>
            )}
          </div>
        </div>
      ))}
    </div>
  );
}
