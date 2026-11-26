import type { Booking } from '../types/booking';

const API_URL = import.meta.env.VITE_API_URL;

// All booking endpoints require a logged-in user, so the JWT is always sent
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

// Reads the error message from the API: plain text from the domain rule
// (e.g. "This room is already booked...") or JSON from the validation attributes
async function getErrorMessage(response: Response): Promise<string> {
  const text = await response.text();
  try {
    const json = JSON.parse(text);
    return json.title ?? text;
  } catch {
    return text || `Request failed (status ${response.status})`;
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
  return response.json();
}

export async function fetchMyBookings(): Promise<Booking[]> {
  const response = await fetch(`${API_URL}/api/bookingapi/mybookings`, {
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

export async function deleteBooking(bookingId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingapi/delete/${bookingId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  if (!response.ok) {
    throw new Error(await getErrorMessage(response));
  }
}
