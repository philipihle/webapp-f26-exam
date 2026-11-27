import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Form, Button, Alert } from 'react-bootstrap';
import type { Room } from '../types/room';

interface RoomFormProps {
  onRoomChanged: (room: Room) => Promise<unknown> | void;
  roomId?: number;
  initialValues?: Partial<Room>;
  error?: string | null;
  submitting?: boolean;
}

// Shared form for creating and updating a room. Validation matches RoomDto in the API.
export default function RoomForm({ onRoomChanged, roomId, initialValues, error = null, submitting = false }: RoomFormProps) {
  const [name, setName] = useState(initialValues?.name ?? '');
  const [roomCode, setRoomCode] = useState(initialValues?.roomCode ?? '');
  const [building, setBuilding] = useState(initialValues?.building ?? '');
  const [floor, setFloor] = useState(initialValues?.floor ?? 0);
  const [capacity, setCapacity] = useState(initialValues?.capacity ?? 1);
  const [roomType, setRoomType] = useState(initialValues?.roomType ?? '');
  const [description, setDescription] = useState(initialValues?.description ?? '');
  const [isActive, setIsActive] = useState(initialValues?.isActive ?? true);
  // Amenities are edited as one comma-separated text, e.g. "Whiteboard, Projector"
  const [amenities, setAmenities] = useState((initialValues?.amenities ?? []).join(', '));
  const navigate = useNavigate();

  const handleSubmit = async (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    await onRoomChanged({
      id: roomId,
      name,
      roomCode,
      building,
      floor,
      capacity,
      roomType: roomType || undefined,
      description,
      isActive,
      amenities: amenities.split(',').map(a => a.trim()).filter(a => a !== ''),
    });
  };

  return (
    <Form onSubmit={handleSubmit}>
      <Form.Group controlId="formRoomName" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Name</Form.Label>
        <Form.Control className="bg-light border-0" type="text" placeholder="Enter room name"
          value={name} onChange={(e) => setName(e.target.value)} required minLength={2} maxLength={50} />
      </Form.Group>

      <Form.Group controlId="formRoomCode" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Room code</Form.Label>
        <Form.Control className="bg-light border-0" type="text" placeholder="E.g. P46-201"
          value={roomCode} onChange={(e) => setRoomCode(e.target.value)} required maxLength={20} />
      </Form.Group>

      <Form.Group controlId="formRoomBuilding" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Building</Form.Label>
        <Form.Control className="bg-light border-0" type="text" placeholder="E.g. P46"
          value={building} onChange={(e) => setBuilding(e.target.value)} required maxLength={50} />
      </Form.Group>

      <Form.Group controlId="formRoomFloor" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Floor</Form.Label>
        <Form.Control className="bg-light border-0" type="number"
          value={floor} onChange={(e) => setFloor(Number(e.target.value))} required min={0} max={20} />
      </Form.Group>

      <Form.Group controlId="formRoomCapacity" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Capacity</Form.Label>
        <Form.Control className="bg-light border-0" type="number"
          value={capacity} onChange={(e) => setCapacity(Number(e.target.value))} required min={1} max={50} />
      </Form.Group>

      <Form.Group controlId="formRoomType" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Room type</Form.Label>
        <Form.Control className="bg-light border-0" type="text" placeholder="E.g. Group room (optional)"
          value={roomType} onChange={(e) => setRoomType(e.target.value)} maxLength={30} />
      </Form.Group>

      <Form.Group controlId="formRoomDescription" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Description</Form.Label>
        <Form.Control className="bg-light border-0" as="textarea" rows={3} placeholder="Enter room description"
          value={description} onChange={(e) => setDescription(e.target.value)} maxLength={500} />
      </Form.Group>

      <Form.Group controlId="formRoomAmenities" className="mb-3">
        <Form.Label className="small fw-semibold text-muted">Amenities</Form.Label>
        <Form.Control className="bg-light border-0" type="text" placeholder="Comma separated, e.g. Whiteboard, Projector"
          value={amenities} onChange={(e) => setAmenities(e.target.value)} />
      </Form.Group>

      <Form.Group controlId="formRoomIsActive" className="mb-3">
        <Form.Check type="checkbox" label="Active (can be booked)"
          checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
      </Form.Group>

      {error && <Alert variant="danger" className="py-2 small">{error}</Alert>}

      <Button variant="primary" type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : roomId ? 'Update Room' : 'Create Room'}
      </Button>
      <Button variant="secondary" onClick={() => navigate(-1)} className="ms-2" disabled={submitting}>
        Cancel
      </Button>
    </Form>
  );
}
