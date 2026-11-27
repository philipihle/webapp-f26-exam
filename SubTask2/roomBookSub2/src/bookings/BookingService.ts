import type { Booking } from '../types/booking';
import { API_URL, getAuthHeaders, handleResponse, ensureOk } from '../shared/api';

export async function fetchMyBookings(): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingapi/mybookings`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking[]>(response);
}

// Upcoming bookings in one room, shown in the table on the booking page
export async function fetchRoomBookings(roomId: string): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingapi/room/${roomId}`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking[]>(response);
}

export async function createBooking(booking: Booking): Promise<{ id: number }> {
  const response = await fetch(`${API_URL}/api/bookingapi/create`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify(booking),
  });
  return handleResponse<{ id: number }>(response);
}

// Switches the booking between open for others and private (only the owner)
export async function toggleJoinFlag(bookingId: number): Promise<Booking> {
  const response = await fetch(`${API_URL}/api/bookingapi/togglejoinflag/${bookingId}`, {
    method: 'PUT',
    headers: getAuthHeaders(),
  });
  return handleResponse<Booking>(response);
}

export async function deleteBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingapi/delete/${bookingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  await ensureOk(response);
}
