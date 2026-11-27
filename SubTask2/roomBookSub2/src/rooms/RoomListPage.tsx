import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button, Form } from 'react-bootstrap';
import { Link } from 'react-router-dom';
import RoomGrid from './RoomGrid';
import RoomTable from './RoomTable';
import type { Room } from '../types/room';
import * as RoomService from './RoomService';
import { useAuth } from '../auth/AuthContext';

export default function RoomListPage() {
  const [showGrid, setShowGrid] = useState<boolean>(true); // cards by default, as in SubTask1
  const [searchQuery, setSearchQuery] = useState<string>(''); // search text, as in the lecturer's ItemListPage
  const [building, setBuilding] = useState<string>(''); // '' = all buildings

  // Fetch rooms through the service layer; React Query tracks loading and error
  const { data: rooms = [], isLoading, error } = useQuery<Room[], Error>({
    queryKey: ['rooms'],
    queryFn: RoomService.fetchRooms,
  });

  const queryClient = useQueryClient();
  const { user } = useAuth(); // logged-in user, or null
  const isAdmin = user?.role === 'Admin'; // only admins can change rooms, as in SubTask1

  // Delete a room, then refetch the list so the page updates
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

  const toggleGridOrTable = () => setShowGrid(prev => !prev);

  // Building names for the dropdown, without duplicates
  const buildings = [...new Set(rooms.map(room => room.building))];

  // Filter on the client like the lecturer: search text (name or description) AND building
  const filteredRooms = rooms.filter(room =>
    (room.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      (room.description ?? '').toLowerCase().includes(searchQuery.toLowerCase())) &&
    (building === '' || room.building === building)
  );

  return (
    <div className="py-4 text-start">
      <div className="row align-items-center justify-content-between mb-4">
        <div className="col-md-8">
          <h1 className="h2 fw-bold text-dark mb-1">Booking Portal</h1>
          <p className="text-secondary mb-0">Browse and book study rooms, auditoriums and meeting rooms</p>
        </div>
        <div className="col-md-4 d-flex justify-content-md-end gap-2 mt-3 mt-md-0">
          <Button variant="outline-primary" size="sm" onClick={toggleGridOrTable}>
            {showGrid ? 'Display Table' : 'Display Cards'}
          </Button>
          {isAdmin && <Link to="/roomcreate" className="btn btn-primary btn-sm">Add New Room</Link>}
        </div>
      </div>

      {/* Search and filter */}
      <div className="row g-2 mb-4">
        <Form.Group className="col-md-8">
          <Form.Control
            type="text"
            className="bg-light border-0"
            placeholder="Search by name or description"
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
          />
        </Form.Group>
        <Form.Group className="col-md-4">
          <Form.Select className="bg-light border-0" value={building} onChange={e => setBuilding(e.target.value)}>
            <option value="">All buildings</option>
            {buildings.map(b => <option key={b} value={b}>{b}</option>)}
          </Form.Select>
        </Form.Group>
      </div>

      {isLoading && <p className="text-muted">Loading...</p>}
      {error && <p className="text-danger">Failed to fetch rooms.</p>}
      {deleteMutation.isError && <p className="text-danger">Failed to delete room.</p>}

      {!isLoading && !error && rooms.length === 0 ? (
        <div className="card border-0 shadow-sm rounded-4 p-5 text-center bg-white my-4">
          <h4 className="fw-bold mb-1">No rooms yet</h4>
          <p className="text-muted small mb-0">Add a room to get started.</p>
        </div>
      ) : showGrid
        ? <RoomGrid rooms={filteredRooms} onRoomDeleted={isAdmin ? handleRoomDeleted : undefined} canBook={!!user} />
        : <RoomTable rooms={filteredRooms} onRoomDeleted={isAdmin ? handleRoomDeleted : undefined} />}
    </div>
  );
}
