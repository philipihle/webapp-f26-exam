// Data sent to /api/auth/login and /api/auth/register (matches LoginDto and RegisterDto in the API)
export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  username: string;
  email: string;
  password: string;
}
