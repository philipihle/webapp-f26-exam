import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Form, Button, Alert } from 'react-bootstrap';
import * as RoomService from '../rooms/RoomService';
import * as BookingService from './BookingService';
import * as ParticipantService from './ParticipantService';
import { useAuth } from '../auth/AuthContext';
import type { Booking } from '../types/booking';

// Booking page for one room: room details on the left, booking form on the right.
export default function BookingCreatePage() {
  const { roomId } = useParams<{ roomId: string }>(); // id from the URL /book/:roomId
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const defaultStart = new Date();
  defaultStart.setMinutes(Math.ceil(defaultStart.getMinutes() / 15) * 15, 0, 0);
  const today = new Date(defaultStart.getTime() - defaultStart.getTimezoneOffset() * 60000).toISOString().slice(0, 10);

  const [date, setDate] = useState(today);
  const [startTime, setStartTime] = useState(defaultStart.toTimeString().slice(0, 5));
  const [endTime, setEndTime] = useState('');
  const [purpose, setPurpose] = useState('Study');
  const [joinFlag, setJoinFlag] = useState(false);

  const roomQuery = useQuery({
    queryKey: ['rooms', roomId],
    queryFn: () => RoomService.fetchRoomById(roomId!),
    enabled: !!roomId,
  });

  const mutation = useMutation({
    mutationFn: BookingService.createBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mybookings'] });
      navigate('/mybookings');
    },
  });

  const { user } = useAuth(); // user.sub is the username, used to find "Host"

  const { data: roomBookings = [] } = useQuery<Booking[], Error>({
    queryKey: ['roombookings', roomId],
    queryFn: () => BookingService.fetchRoomBookings(roomId!),
    enabled: !!roomId,
  });

  const { data: joinedBookings = [] } = useQuery<Booking[], Error>({
    queryKey: ['joinedbookings'],
    queryFn: ParticipantService.fetchJoinedBookings,
  });

  const joinMutation = useMutation({
    mutationFn: ParticipantService.joinBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['joinedbookings'] });
    },
  });

  const handleSubmit = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    mutation.mutate({
      roomId: Number(roomId),
      start: `${date}T${startTime}:00`,
      end: `${date}T${endTime}:00`,
      purpose,
      joinFlag,
    });
  };

  if (roomQuery.isLoading) return <p className="text-muted py-4">Loading...</p>;
  if (roomQuery.isError || !roomQuery.data) return <p className="text-danger py-4">Failed to load room</p>;

  const room = roomQuery.data;

  return (
    <div className="py-4 text-start">
      <div className="mb-4">
        <Link to="/rooms" className="btn btn-outline-secondary btn-sm">Back to all rooms</Link>
      </div>

      <div className="card border-0 shadow-sm rounded-4 mb-4">
        <div className="row g-0">
          {/* Room details */}
          <div className="col-lg-6 p-4">
            <div className="d-flex justify-content-between align-items-start mb-3">
              <div>
                <h2 className="h3 fw-bold mb-1">{room.name}</h2>
                {room.roomType && <div className="fw-semibold small mb-1">{room.roomType}</div>}
                <p className="text-muted mb-0">{room.building} &bull; Floor {room.floor} &bull; {room.roomCode}</p>
              </div>
              <span className="text-muted small text-nowrap ms-2">{room.capacity} people max</span>
            </div>

            <p className="text-secondary my-4">{room.description}</p>

            <h5 className="fw-bold mb-3">Facilities &amp; Equipment</h5>
            <div className="d-flex flex-wrap gap-2">
              {room.amenities.length > 0
                ? room.amenities.map(amenity => <span className="amenity-chip" key={amenity}>{amenity}</span>)
                : <span className="text-muted small">Standard room equipment</span>}
            </div>

            <div className="pt-3 mt-4 border-top text-muted small">
              Reservation confirmation is sent immediately upon booking.
            </div>
          </div>

          {/* Booking form */}
          <div className="col-lg-6 p-4">
            <h4 className="fw-bold mb-1">Reserve this Room</h4>
            <p className="text-muted small mb-4">Set your start and end time. Choose an available slot for your session.</p>

            <Form onSubmit={handleSubmit}>
              {mutation.error && <Alert variant="danger" className="py-2 small mb-3">{mutation.error.message}</Alert>}

              <Form.Group className="mb-3" controlId="formBookingDate">
                <Form.Label className="small fw-semibold text-muted">Date</Form.Label>
                <Form.Control type="date" className="bg-light border-0" value={date} min={today}
                  onChange={(e) => setDate(e.target.value)} required />
              </Form.Group>

              <div className="row g-2 mb-3">
                <Form.Group className="col-6" controlId="formBookingStart">
                  <Form.Label className="small fw-semibold text-muted">From (Start time)</Form.Label>
                  <Form.Control type="time" className="bg-light border-0" value={startTime} step={900}
                    onChange={(e) => setStartTime(e.target.value)} required />
                </Form.Group>
                <Form.Group className="col-6" controlId="formBookingEnd">
                  <Form.Label className="small fw-semibold text-muted">To (End time)</Form.Label>
                  <Form.Control type="time" className="bg-light border-0" value={endTime} step={900}
                    onChange={(e) => setEndTime(e.target.value)} required />
                </Form.Group>
              </div>

              <Form.Group className="mb-4" controlId="formBookingPurpose">
                <Form.Label className="small fw-semibold text-muted">Purpose</Form.Label>
                <Form.Select className="bg-light border-0" value={purpose} onChange={(e) => setPurpose(e.target.value)}>
                  <option value="Study">Individual Study</option>
                  <option value="Group Project">Group Project</option>
                  <option value="Meeting">Meeting</option>
                  <option value="Presentation">Presentation</option>
                  <option value="Other">Other</option>
                </Form.Select>
              </Form.Group>

              <Form.Check className="mb-4 small" type="checkbox" id="joinFlag"
                label="Allow other students to join this session" checked={joinFlag}
                onChange={(e) => setJoinFlag(e.target.checked)} />

              <Button type="submit" variant="primary" className="w-100 py-2 fw-semibold" disabled={mutation.isPending}>
                {mutation.isPending ? 'Saving...' : 'Confirm Reservation'}
              </Button>
            </Form>

            <div className="text-center mt-3 small text-muted">
              You can view and manage your reservation under My Bookings.
            </div>
          </div>
        </div>
      </div>

      {/* Upcoming bookings in this room */}
      <div className="card border-0 shadow-sm rounded-4">
        <div className="card-body p-4">
          <h5 className="fw-bold mb-3">Upcoming Sessions in this Room</h5>
          {joinMutation.error && <Alert variant="danger" className="py-2 small mb-3">{joinMutation.error.message}</Alert>}

          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th>Name</th>
                  <th>Date</th>
                  <th>From</th>
                  <th>To</th>
                  <th className="text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                {roomBookings.map(booking => (
                  <tr key={booking.id}>
                    <td className="fw-semibold">{booking.userName ?? 'Student'}</td>
                    <td>{booking.start.split('T')[0]}</td>
                    <td>{booking.start.substring(11, 16)}</td>
                    <td>{booking.end.substring(11, 16)}</td>
                    <td className="text-end">
                      {!booking.joinFlag ? (
                        <span className="text-muted small">Private</span>
                      ) : booking.userName === user?.sub ? (
                        <span className="badge bg-secondary">Host</span>
                      ) : joinedBookings.some(b => b.id === booking.id) ? (
                        <span className="badge bg-success">Joined</span>
                      ) : (
                        <Button variant="outline-primary" size="sm" onClick={() => joinMutation.mutate(booking.id!)}>Join</Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
