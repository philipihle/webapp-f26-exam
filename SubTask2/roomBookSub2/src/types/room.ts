export interface Room {
  id?: number;
  name: string;
  roomCode: string;
  building: string;
  floor: number;
  capacity: number;
  roomType?: string;
  description: string;
  isActive: boolean;
  amenities: string[];
}
