// Matches BookingDto in the API
export interface Booking {
  id?: number;
  roomId: number;
  roomName?: string; // filled in by the API
  userName?: string; // filled in by the API
  start: string; // ISO date-time, e.g. "2026-11-27T10:00:00"
  end: string;
  purpose: string;
  joinFlag: boolean;
}
