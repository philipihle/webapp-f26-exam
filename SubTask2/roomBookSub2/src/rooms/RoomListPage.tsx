import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Table, Button } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import type { Room } from '../types/room';
import * as RoomService from './RoomService';

export default function RoomListPage() {
  // Fetch rooms through the service layer; React Query tracks loading and error
  const { data: rooms = [], isLoading, error } = useQuery<Room[], Error>({
    queryKey: ['rooms'],
    queryFn: RoomService.fetchRooms,
  });

  const queryClient = useQueryClient();

  // Delete a room, then refetch the list so the table updates
  const deleteMutation = useMutation({
    mutationFn: RoomService.deleteRoom,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rooms'] });
    },
  });

  const handleRoomDeleted = (room: Room) => {
    const confirmDelete = window.confirm(`Are you sure you want to delete the room ${room.name}?`);
    if (confirmDelete) {
      deleteMutation.mutate(room.id!);
    }
  };

  return (
    <div className="text-start">
      <h1>Rooms</h1>
      {isLoading && <p>Loading...</p>}
      {error && <p className="text-danger">Failed to fetch rooms.</p>}
      {deleteMutation.isError && <p className="text-danger">Failed to delete room.</p>}
      <Table striped bordered hover>
        <thead>
          <tr>
            <th>Name</th>
            <th>Code</th>
            <th>Building</th>
            <th>Floor</th>
            <th>Capacity</th>
            <th>Actions</th>
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
              <td className="text-center">
                <Link to={`/roomupdate/${room.id}`}>Update</Link>
                <Button
                  onClick={() => handleRoomDeleted(room)}
                  variant="link"
                  className="text-danger"
                >Delete</Button>
              </td>
            </tr>
          ))}
        </tbody>
      </Table>
      <Link to="/roomcreate" className="btn btn-secondary mt-3">Add New Room</Link>
    </div>
  );
}
