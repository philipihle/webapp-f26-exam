import type { Room } from '../types/room';
import { API_URL, getAuthHeaders, handleResponse, ensureOk } from '../shared/api';

export async function fetchRooms(): Promise<Room[]> {
  const response = await fetch(`${API_URL}/api/roomapi/roomlist`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Room[]>(response);
}

export async function fetchRoomById(roomId: string): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/${roomId}`, {
    headers: getAuthHeaders(),
  });
  return handleResponse<Room>(response);
}

export async function createRoom(room: Room): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/create`, {
    method: 'POST',
    headers: getAuthHeaders(),
    body: JSON.stringify(room),
  });
  return handleResponse<Room>(response);
}

export async function updateRoom(room: Room): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/update/${room.id}`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(room),
  });
  return handleResponse<Room>(response);
}

export async function deleteRoom(roomId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/roomapi/delete/${roomId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(),
  });
  await ensureOk(response);
}
