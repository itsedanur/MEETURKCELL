import apiClient from '../../../api/apiClient';
import { ApiResponse } from '../../../api/apiResponse';
import { User } from '../../../contexts/AuthContext';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  token?: string;
  user: {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
  };
}

export const authApi = {
  login: async (data: LoginRequest) => {
    const response = await apiClient.post<ApiResponse<LoginResponse>>('/auth/login', data);
    return response.data;
  },
  
  me: async () => {
    const response = await apiClient.get<ApiResponse<User>>('/auth/me');
    return response.data;
  }
};
