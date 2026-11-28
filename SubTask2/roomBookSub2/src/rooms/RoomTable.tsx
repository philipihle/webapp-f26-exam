import { Table, Button } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Room } from '../types/room';

interface RoomTableProps {
  rooms: Room[];
  onRoomDeleted?: (room: Room) => void; // only passed when the user is logged in
}

// Rooms shown as a table (alternative to RoomGrid)
export default function RoomTable({ rooms, onRoomDeleted }: RoomTableProps) {
  return (
    <div className="card border-0 shadow-sm rounded-4 overflow-hidden">
      <Table hover className="mb-0">
        <thead>
          <tr>
            <th>Name</th>
            <th>Code</th>
            <th>Building</th>
            <th>Floor</th>
            <th>Capacity</th>
            <th>Room type</th>
            {onRoomDeleted && <th>Actions</th>}
          </tr>
        </thead>
        <tbody>
          {rooms.map(room => (
            <tr key={room.id}>
              <td>{room.name}</td>
              <td>{room.roomCode}</td>
              <td>{room.building}</td>
              <td>{room.floor}</td>
              <td>{room.capacity}</td>
              <td>{room.roomType ?? '-'}</td>
              {onRoomDeleted && (
                <td className="text-center">
                  <Link to={`/roomupdate/${room.id}`}>Update</Link>
                  <Button
                    onClick={() => onRoomDeleted(room)}
                    variant="link"
                    className="text-danger"
                  >Delete</Button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  );
}
