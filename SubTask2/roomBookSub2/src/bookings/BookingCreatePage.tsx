import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Form, Button, Alert } from 'react-bootstrap';
import * as RoomService from '../rooms/RoomService';
import * as BookingService from './BookingService';

// Booking page for one room. Layout and classes follow Views/Booking/Create.cshtml in SubTask1:
// room details on the left, booking form on the right.
export default function BookingCreatePage() {
  const { roomId } = useParams<{ roomId: string }>(); // id from the URL /book/:roomId
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [date, setDate] = useState<string>('');
  const [startTime, setStartTime] = useState<string>('');
  const [endTime, setEndTime] = useState<string>('');
  const [purpose, setPurpose] = useState<string>('Study');
  const [joinFlag, setJoinFlag] = useState<boolean>(false);

  const today = new Date().toISOString().split('T')[0]; // "yyyy-mm-dd", used as min date

  // Load the room so its details can be shown
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

  const handleSubmit = (event: React.SyntheticEvent<HTMLFormElement>) => {
    event.preventDefault();
    // Combine date and times into the format the API expects, e.g. "2026-11-27T10:00:00"
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

      <div className="card border-0 shadow-sm rounded-4 bg-white overflow-hidden mb-4">
        <div className="row g-0">
          {/* Room details */}
          <div className="col-lg-6 p-4 p-md-5 d-flex flex-column justify-content-between border-bottom border-lg-bottom-0 border-lg-end">
            <div>
              <div className="d-flex justify-content-between align-items-start mb-3">
                <div>
                  <h2 className="h3 fw-bold mb-1">{room.name}</h2>
                  {room.roomType && <div className="fw-semibold text-dark small mb-1">{room.roomType}</div>}
                  <p className="text-muted mb-0">{room.building} &bull; Floor {room.floor} &bull; {room.roomCode}</p>
                </div>
                <span className="text-muted small flex-shrink-0 ms-2">{room.capacity} people max</span>
              </div>

              <p className="text-secondary my-4">{room.description}</p>

              <h5 className="fw-bold mb-3">Facilities &amp; Equipment</h5>
              <div className="d-flex flex-wrap gap-2">
                {room.amenities.length > 0
                  ? room.amenities.map(amenity => <span className="amenity-chip" key={amenity}>{amenity}</span>)
                  : <span className="text-muted small">Standard room equipment</span>}
              </div>
            </div>

            <div className="pt-4 mt-4 border-top text-muted small">
              Reservation confirmation is sent immediately upon booking.
            </div>
          </div>

          {/* Booking form */}
          <div className="col-lg-6 p-4 p-md-5 d-flex flex-column justify-content-between">
            <div>
              <h4 className="fw-bold mb-1">Reserve this Room</h4>
              <p className="text-muted small mb-4">Set your start and end time. Choose an available slot for your session.</p>

              <Form onSubmit={handleSubmit}>
                {mutation.error && (
                  <Alert variant="danger" className="py-2 px-3 small border-0 rounded-3 mb-3">
                    {mutation.error.message}
                  </Alert>
                )}

                <Form.Group className="mb-3" controlId="formBookingDate">
                  <Form.Label className="small fw-semibold text-muted">Date</Form.Label>
                  <Form.Control
                    type="date"
                    className="bg-light border-0 py-2"
                    value={date}
                    min={today}
                    onChange={(e) => setDate(e.target.value)}
                    required
                  />
                </Form.Group>

                <div className="row g-2 mb-3">
                  <Form.Group className="col-6" controlId="formBookingStart">
                    <Form.Label className="small fw-semibold text-muted">From (Start time)</Form.Label>
                    <Form.Control
                      type="time"
                      className="bg-light border-0 py-2"
                      value={startTime}
                      step={900}
                      onChange={(e) => setStartTime(e.target.value)}
                      required
                    />
                  </Form.Group>
                  <Form.Group className="col-6" controlId="formBookingEnd">
                    <Form.Label className="small fw-semibold text-muted">To (End time)</Form.Label>
                    <Form.Control
                      type="time"
                      className="bg-light border-0 py-2"
                      value={endTime}
                      step={900}
                      onChange={(e) => setEndTime(e.target.value)}
                      required
                    />
                  </Form.Group>
                </div>

                <Form.Group className="mb-4" controlId="formBookingPurpose">
                  <Form.Label className="small fw-semibold text-muted">Purpose</Form.Label>
                  <Form.Select
                    className="bg-light border-0 py-2"
                    value={purpose}
                    onChange={(e) => setPurpose(e.target.value)}
                  >
                    <option value="Study">Individual Study</option>
                    <option value="Group Project">Group Project</option>
                    <option value="Meeting">Meeting</option>
                    <option value="Presentation">Presentation</option>
                    <option value="Other">Other</option>
                  </Form.Select>
                </Form.Group>

                <Form.Check
                  className="mb-4 small text-dark"
                  type="checkbox"
                  id="joinFlag"
                  label="Allow other students to join this session"
                  checked={joinFlag}
                  onChange={(e) => setJoinFlag(e.target.checked)}
                />

                <Button type="submit" variant="primary" className="w-100 py-2 fw-semibold shadow-sm" disabled={mutation.isPending}>
                  {mutation.isPending ? 'Saving...' : 'Confirm Reservation'}
                </Button>
              </Form>
            </div>

            <div className="text-center mt-3 small text-muted">
              You can view and manage your reservation under My Bookings.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
