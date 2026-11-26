import { useQuery } from '@tanstack/react-query';
import { Table } from 'react-bootstrap';
import type { Room } from '../types/room';
import * as RoomService from './RoomService';

export default function RoomListPage() {
  // Fetch rooms through the service layer; React Query tracks loading and error
  const { data: rooms = [], isLoading, error } = useQuery<Room[], Error>({
    queryKey: ['rooms'],
    queryFn: RoomService.fetchRooms,
  });

  return (
    <div className="text-start">
      <h1>Rooms</h1>
      {isLoading && <p>Loading...</p>}
      {error && <p className="text-danger">Failed to fetch rooms.</p>}
      <Table striped bordered hover>
        <thead>
          <tr>
            <th>Name</th>
            <th>Code</th>
            <th>Building</th>
            <th>Floor</th>
            <th>Capacity</th>
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
            </tr>
          ))}
        </tbody>
      </Table>
    </div>
  );
}
