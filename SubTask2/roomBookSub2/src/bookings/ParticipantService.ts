import type { Booking } from '../types/booking';
import type { Participant } from '../types/participant';

const API_URL = import.meta.env.VITE_API_URL;

// All participant endpoints require a logged-in user, so the JWT is always sent
const getAuthHeaders = () => {
  const token = localStorage.getItem('token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
};

// Reads the error message from the API, e.g. "This booking is not joinable."
async function getErrorMessage(response: Response): Promise<string> {
  const text = await response.text();
  return text || `Request failed (status ${response.status})`;
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
  return response.json();
}

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
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
}

export async function leaveBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingparticipantapi/leave/${bookingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
}
