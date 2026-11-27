import { getAuthHeaders } from '../shared/api';
import type { LoginDto, RegisterDto } from '../types/auth';

const API_URL = import.meta.env.VITE_API_URL;

export interface AuthResponse {
  token: string;
}

export interface RegisterResponse {
  message?: string;
}

// POST /api/auth/login – returns a JWT if the credentials are correct
export const login = async (credentials: LoginDto): Promise<AuthResponse> => {
  const response = await fetch(`${API_URL}/api/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(credentials),
  });

  if (!response.ok) {
    throw new Error('Login failed. Please check your email and password.');
  }
  return response.json();
};

// POST /api/auth/register – the API returns a list of Identity errors if it fails
export const register = async (userData: RegisterDto): Promise<RegisterResponse> => {
  const response = await fetch(`${API_URL}/api/Auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(userData),
  });

  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    // ASP.NET Identity errors: [{ code: "...", description: "..." }]
    if (Array.isArray(errorData)) {
      const errorMessages = errorData.map((err) => err.description).join(', ');
      throw new Error(errorMessages || 'Registration failed');
    }
    throw new Error(errorData?.title || 'Registration failed');
  }
  return response.json();
};

export const updateAccount = async (userData: { email: string; password?: string; confirmPassword?: string }): Promise<RegisterResponse> => {
  const response = await fetch(`${API_URL}/api/Auth/update`, {
    method: 'PUT',
    headers: getAuthHeaders(),
    body: JSON.stringify(userData),
  });
  if (!response.ok) {
    const errorData = await response.json().catch(() => null);
    if (Array.isArray(errorData)) {
      const errorMessages = errorData.map((err) => err.description).join(', ');
      throw new Error(errorMessages || 'Failed to update account.');
    }
    throw new Error(errorData?.title || 'Failed to update account.');
  }
  return response.json();
};