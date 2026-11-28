import type { BookingChat } from '../types/bookingchat';
import { API_URL, getAuthHeaders, handleResponse, ensureOk } from '../shared/api';

export async function getBookingChats(bookingId: number): Promise<BookingChat[]> {
  const response = await fetch(`${API_URL}/api/bookingchatapi/booking/${bookingId}`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<BookingChat[]>(response);
}

export async function sendBookingChat(bookingId: number, message: string): Promise<BookingChat> {
  const response = await fetch(`${API_URL}/api/bookingchatapi/booking/${bookingId}`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify({ bookingId, message }),
  });
  return handleResponse<BookingChat>(response);
}

export async function deleteBookingChat(chatId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/bookingchatapi/${chatId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  await ensureOk(response);
}