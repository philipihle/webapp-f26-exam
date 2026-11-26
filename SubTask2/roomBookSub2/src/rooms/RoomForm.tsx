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

// Shared form for creating and updating a room.
// The validation rules match the attributes on RoomDto in the API.
export default function RoomForm({
  onRoomChanged,
  roomId,
  initialValues,
  error = null,
  submitting = false,
}: RoomFormProps) {
  const [name, setName] = useState<string>(initialValues?.name ?? '');
  const [roomCode, setRoomCode] = useState<string>(initialValues?.roomCode ?? '');
  const [building, setBuilding] = useState<string>(initialValues?.building ?? '');
  const [floor, setFloor] = useState<number>(initialValues?.floor ?? 0);
  const [capacity, setCapacity] = useState<number>(initialValues?.capacity ?? 1);
  const [roomType, setRoomType] = useState<string>(initialValues?.roomType ?? '');
  const [description, setDescription] = useState<string>(initialValues?.description ?? '');
  const [isActive, setIsActive] = useState<boolean>(initialValues?.isActive ?? true);
  // Amenities are edited as one comma-separated text, e.g. "Whiteboard, Projector"
  const [amenities, setAmenities] = useState<string>((initialValues?.amenities ?? []).join(', '));
  const navigate = useNavigate();

  const onCancel = () => {
    navigate(-1); // go back one step in the history
  };

  const handleSubmit = async (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    const room: Room = {
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
    };
    await onRoomChanged(room);
  };

  return (
    <Form onSubmit={handleSubmit}>
      <Form.Group controlId="formRoomName" className="mb-3">
        <Form.Label>Name</Form.Label>
        <Form.Control
          type="text"
          placeholder="Enter room name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          minLength={2}
          maxLength={50}
        />
      </Form.Group>

      <Form.Group controlId="formRoomCode" className="mb-3">
        <Form.Label>Room code</Form.Label>
        <Form.Control
          type="text"
          placeholder="E.g. P46-201"
          value={roomCode}
          onChange={(e) => setRoomCode(e.target.value)}
          required
          maxLength={20}
        />
      </Form.Group>

      <Form.Group controlId="formRoomBuilding" className="mb-3">
        <Form.Label>Building</Form.Label>
        <Form.Control
          type="text"
          placeholder="E.g. P46"
          value={building}
          onChange={(e) => setBuilding(e.target.value)}
          required
          maxLength={50}
        />
      </Form.Group>

      <Form.Group controlId="formRoomFloor" className="mb-3">
        <Form.Label>Floor</Form.Label>
        <Form.Control
          type="number"
          value={floor}
          onChange={(e) => setFloor(Number(e.target.value))}
          required
          min={0}
          max={20}
        />
      </Form.Group>

      <Form.Group controlId="formRoomCapacity" className="mb-3">
        <Form.Label>Capacity</Form.Label>
        <Form.Control
          type="number"
          value={capacity}
          onChange={(e) => setCapacity(Number(e.target.value))}
          required
          min={1}
          max={50}
        />
      </Form.Group>

      <Form.Group controlId="formRoomType" className="mb-3">
        <Form.Label>Room type</Form.Label>
        <Form.Control
          type="text"
          placeholder="E.g. Group room (optional)"
          value={roomType}
          onChange={(e) => setRoomType(e.target.value)}
          maxLength={30}
        />
      </Form.Group>

      <Form.Group controlId="formRoomDescription" className="mb-3">
        <Form.Label>Description</Form.Label>
        <Form.Control
          as="textarea"
          rows={3}
          placeholder="Enter room description"
          value={description}
          onChange={(e) => setDescription(e.target.value)}
          maxLength={500}
        />
      </Form.Group>

      <Form.Group controlId="formRoomAmenities" className="mb-3">
        <Form.Label>Amenities</Form.Label>
        <Form.Control
          type="text"
          placeholder="Comma separated, e.g. Whiteboard, Projector"
          value={amenities}
          onChange={(e) => setAmenities(e.target.value)}
        />
      </Form.Group>

      <Form.Group controlId="formRoomIsActive" className="mb-3">
        <Form.Check
          type="checkbox"
          label="Active (can be booked)"
          checked={isActive}
          onChange={(e) => setIsActive(e.target.checked)}
        />
      </Form.Group>

      {error && <Alert variant="danger">{error}</Alert>}

      <Button variant="primary" type="submit" disabled={submitting}>
        {submitting ? 'Saving...' : roomId ? 'Update Room' : 'Create Room'}
      </Button>
      <Button variant="secondary" onClick={onCancel} className="ms-2" disabled={submitting}>
        Cancel
      </Button>
    </Form>
  );
}
