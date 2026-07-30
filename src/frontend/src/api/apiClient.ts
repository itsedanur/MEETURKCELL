import axios from 'axios';
import { authStorage } from '../utils/authStorage';
import { parseApiError } from './apiError';

// Base API URL from environment variables, fallback to local backend port
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5283/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Attach JWT Token
apiClient.interceptors.request.use(
  (config) => {
    const token = authStorage.getToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response Interceptor: Global Error Handling & 401 Redirects
apiClient.interceptors.response.use(
  (response) => {
    // If backend returns a successful response wrapped in ApiResponse<T>, we can just return it
    // But axios wraps everything in its own response.data
    return response;
  },
  (error) => {
    const parsedError = parseApiError(error);

    // Handle 401 Unauthorized globally
    if (parsedError.statusCode === 401) {
      authStorage.clearToken();
      // Redirect to login if we're not already there
      if (typeof window !== 'undefined' && !window.location.pathname.includes('/login')) {
        window.location.href = '/login?expired=true';
      }
    }

    // Return the parsed ApiError so components can handle it cleanly
    return Promise.reject(parsedError);
  }
);

export default apiClient;
