import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import apiClient from '../api/apiClient';
import { authStorage } from '../utils/authStorage';
import { ApiResponse } from '../api/apiResponse';

export interface User {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: string;
}

interface AuthContextType {
  isAuthenticated: boolean;
  user: User | null;
  isLoading: boolean;
  login: (token: string, user: User) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const initializeAuth = async () => {
      const token = authStorage.getToken();
      if (token) {
        try {
          // Verify token by calling /api/auth/me
          const response = await apiClient.get<ApiResponse<User>>('/auth/me');
          if (response.data.success && response.data.data) {
            setUser(response.data.data);
            setIsAuthenticated(true);
          } else {
            // Invalid response
            authStorage.clearToken();
          }
        } catch (error) {
          // Token is likely invalid or expired
          authStorage.clearToken();
        }
      }
      setIsLoading(false);
    };

    initializeAuth();
  }, []);

  const login = (token: string, userData: User) => {
    authStorage.setToken(token);
    setUser(userData);
    setIsAuthenticated(true);
  };

  const logout = () => {
    authStorage.clearToken();
    setUser(null);
    setIsAuthenticated(false);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, user, isLoading, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
