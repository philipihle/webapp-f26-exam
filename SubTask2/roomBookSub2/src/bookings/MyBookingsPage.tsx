import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Button, Alert } from 'react-bootstrap';
import type { Booking } from '../types/booking';
import * as BookingService from './BookingService';
import * as ParticipantService from './ParticipantService';

// Date helpers: "27.11.2026" and "10:00", same format as SubTask1
const formatDate = (iso: string) => new Date(iso).toLocaleDateString('nb-NO', { day: '2-digit', month: '2-digit', year: 'numeric' });
const formatTime = (iso: string) => new Date(iso).toLocaleTimeString('nb-NO', { hour: '2-digit', minute: '2-digit' });

// The logged-in user's bookings. Layout and classes follow Views/Booking/MyBookings.cshtml in SubTask1.
export default function MyBookingsPage() {
  const queryClient = useQueryClient();

  const { data: bookings = [], isLoading, error } = useQuery<Booking[], Error>({
    queryKey: ['mybookings'],
    queryFn: BookingService.fetchMyBookings,
  });

  // Bookings the user has joined as a participant
  const { data: joinedBookings = [] } = useQuery<Booking[], Error>({
    queryKey: ['joinedbookings'],
    queryFn: ParticipantService.fetchJoinedBookings,
  });

  const cancelMutation = useMutation({
    mutationFn: BookingService.deleteBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mybookings'] });
    },
  });

  // Switch JoinFlag on an own booking, then refetch so the button text changes
  const toggleMutation = useMutation({
    mutationFn: BookingService.toggleJoinFlag,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['mybookings'] });
    },
  });

  // Leave a joined booking, then refetch the joined list
  const leaveMutation = useMutation({
    mutationFn: ParticipantService.leaveBooking,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['joinedbookings'] });
    },
  });

  const handleCancel = (booking: Booking) => {
    if (window.confirm('Are you sure you want to cancel this booking?')) {
      cancelMutation.mutate(booking.id!);
    }
  };

  const handleLeave = (booking: Booking) => {
    if (window.confirm('Are you sure you want to leave this session?')) {
      leaveMutation.mutate(booking.id!);
    }
  };

  // Own and joined bookings in one list, like MyBookings() in SubTask1 (isOrganizer = own booking)
  const allBookings = [
    ...bookings.map(b => ({ ...b, isOrganizer: true })),
    ...joinedBookings.map(b => ({ ...b, isOrganizer: false })),
  ].sort((a, b) => new Date(a.start).getTime() - new Date(b.start).getTime()); // earliest first

  // Split into upcoming (not finished yet) and previous bookings
  const now = new Date();
  const upcomingBookings = allBookings.filter(b => new Date(b.end) > now);
  const pastBookings = allBookings.filter(b => new Date(b.end) <= now);

  return (
    <div className="py-4 text-start">
      <div className="d-flex flex-column flex-md-row justify-content-between align-items-md-center gap-3 mb-4">
        <div>
          <h1 className="h2 fw-bold text-dark mb-1">My Bookings</h1>
          <p className="text-secondary mb-0">View and manage your upcoming and previous reservations.</p>
        </div>
        <div>
          <Link to="/rooms" className="btn btn-primary">Book a Room</Link>
        </div>
      </div>

      {isLoading && <p className="text-muted">Loading...</p>}
      {error && <Alert variant="danger">Failed to fetch bookings.</Alert>}
      {cancelMutation.error && <Alert variant="danger">{cancelMutation.error.message}</Alert>}
      {leaveMutation.error && <Alert variant="danger">{leaveMutation.error.message}</Alert>}
      {toggleMutation.error && <Alert variant="danger">{toggleMutation.error.message}</Alert>}

      <div className="mb-5">
        <h4 className="fw-bold mb-3">Upcoming Sessions</h4>

        {!isLoading && upcomingBookings.length === 0 ? (
          <div className="card border-0 shadow-sm rounded-4 text-center p-5 bg-white">
            <h5 className="fw-bold mb-1">No upcoming bookings</h5>
            <p className="text-muted small mb-3">You do not have any active room reservations at the moment.</p>
            <div>
              <Link to="/rooms" className="btn btn-outline-primary btn-sm">Find a study room</Link>
            </div>
          </div>
        ) : (
          <div className="row g-3">
            {upcomingBookings.map(booking => (
              <div className="col-lg-6" key={booking.id}>
                <div className="card border-0 shadow-sm rounded-4 h-100">
                  <div className="card-body p-4 d-flex flex-column">
                    <div className="mb-3">
                      <h5 className="fw-bold text-dark mb-1">{booking.roomName}</h5>
                    </div>

                    <div className="bg-light rounded-3 p-3 mb-3">
                      <div className="text-dark fw-semibold mb-1">{formatDate(booking.start)}</div>
                      <div className="text-muted small">
                        <span>{formatTime(booking.start)} – {formatTime(booking.end)}</span>
                        {booking.purpose && <span> &bull; {booking.purpose}</span>}
                      </div>
                    </div>

                    <div className="mt-auto pt-2 d-flex justify-content-end gap-2 border-top">
                      {/* Owner can switch visibility, same button texts as BookingConfirmed.cshtml in SubTask1 */}
                      {booking.isOrganizer && (
                        <Button variant="outline-secondary" size="sm" onClick={() => toggleMutation.mutate(booking.id!)}>
                          {booking.joinFlag ? 'Change to Private' : 'Allow others to join'}
                        </Button>
                      )}
                      {/* Owner can cancel, participant can leave */}
                      {booking.isOrganizer
                        ? <Button variant="outline-danger" size="sm" onClick={() => handleCancel(booking)}>Cancel</Button>
                        : <Button variant="outline-danger" size="sm" onClick={() => handleLeave(booking)}>Leave</Button>}
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {pastBookings.length > 0 && (
        <div className="card border-0 shadow-sm rounded-4 overflow-hidden bg-white">
          <div className="card-header bg-white border-bottom py-3">
            <h5 className="fw-bold mb-0">Previous Bookings ({pastBookings.length})</h5>
          </div>
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead className="table-light">
                <tr>
                  <th>Room</th>
                  <th>Date</th>
                  <th>Time</th>
                  <th>Purpose</th>
                </tr>
              </thead>
              <tbody>
                {pastBookings.map(booking => (
                  <tr key={booking.id}>
                    <td><span className="fw-semibold">{booking.roomName}</span></td>
                    <td>{formatDate(booking.start)}</td>
                    <td>{formatTime(booking.start)} – {formatTime(booking.end)}</td>
                    <td>{booking.purpose}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
