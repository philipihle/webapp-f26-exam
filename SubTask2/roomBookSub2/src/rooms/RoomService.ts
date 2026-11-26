import type { Room } from '../types/room';

const API_URL = import.meta.env.VITE_API_URL;

const headers = {
  'Content-Type': 'application/json',
};

// Throws an error if the API did not answer with 2xx, otherwise returns the JSON body
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`Request failed (status ${response.status})`);
  }
  return response.json();
}

export async function fetchRooms(): Promise<Room[]> {
  const response = await fetch(`${API_URL}/api/roomapi/roomlist`);
  return handleResponse<Room[]>(response);
}

export async function fetchRoomById(roomId: string): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/${roomId}`);
  return handleResponse<Room>(response);
}

export async function createRoom(room: Room): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/create`, {
    method: 'POST',
    headers,
    body: JSON.stringify(room),
  });
  return handleResponse<Room>(response);
}

export async function updateRoom(room: Room): Promise<Room> {
  const response = await fetch(`${API_URL}/api/roomapi/update/${room.id}`, {
    method: 'PUT',
    headers,
    body: JSON.stringify(room),
  });
  return handleResponse<Room>(response);
}

export async function deleteRoom(roomId: number): Promise<void> {
  const response = await fetch(`${API_URL}/api/roomapi/delete/${roomId}`, {
    method: 'DELETE',
  });
  if (!response.ok) {
    throw new Error(`Failed to delete room (status ${response.status})`);
  }
}
