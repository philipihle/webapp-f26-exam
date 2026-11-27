import type { Booking } from '../types/booking';
import type { Participant } from '../types/participant';
import { API_URL, getAuthHeaders, handleResponse, ensureOk } from '../shared/api';

export async function fetchParticipants(bookingId: number): Promise<Participant[]> {
  const response = await fetch(`${API_URL}/api/bookingparticipantapi/booking/${bookingId}`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Participant[]>(response);
}

export async function fetchJoinedBookings(): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingparticipantapi/joinedbookings`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking[]>(response);
}

// Join and leave return no body, so only the status is checked
export async function joinBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingparticipantapi/join/${bookingId}`, {
    method: 'POST',
    headers: getAuthHeaders(),
  });
  await ensureOk(response);
}

export async function leaveBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingparticipantapi/leave/${bookingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  await ensureOk(response);
}
