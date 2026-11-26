import { createContext, useState, useContext, useEffect } from 'react';
import type { ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';
import type { User } from '../types/user';
import type { LoginDto } from '../types/auth';
import * as authService from './AuthService';

interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (credentials: LoginDto) => Promise<void>;
  logout: () => void;
  isLoading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

// Wraps the app and keeps the logged-in user and token for all components
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('token'));
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // On start (and when the token changes): read the user from the token, or clear it if expired
  useEffect(() => {
    if (!token) {
      setIsLoading(false);
      return;
    }
    try {
      const decodedUser: User = jwtDecode(token);
      if (decodedUser.exp * 1000 > Date.now()) {
        setUser(decodedUser);
      } else {
        localStorage.removeItem('token');
        setUser(null);
        setToken(null);
      }
    } catch {
      localStorage.removeItem('token');
      setUser(null);
      setToken(null);
    } finally {
      setIsLoading(false);
    }
  }, [token]);

  const login = async (credentials: LoginDto) => {
    const { token } = await authService.login(credentials);
    localStorage.setItem('token', token);
    setUser(jwtDecode<User>(token));
    setToken(token);
  };

  const logout = () => {
    localStorage.removeItem('token');
    setUser(null);
    setToken(null);
  };

  return (
    <AuthContext.Provider value={{ user, token, login, logout, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

// Hook used by components: const { user, login, logout } = useAuth();
export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
