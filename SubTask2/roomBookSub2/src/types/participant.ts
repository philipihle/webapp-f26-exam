// Matches BookingParticipantDto in the API
export interface Participant {
  id: number;
  bookingId: number;
  userName?: string;
  joinedAtDateTime: string; // ISO date-time, e.g. "2026-11-27T10:00:00"
}
