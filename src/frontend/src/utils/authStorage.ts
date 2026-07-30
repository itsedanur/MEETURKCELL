const TOKEN_KEY = 'auth_token';

export const authStorage = {
  getToken: (): string | null => {
    return sessionStorage.getItem(TOKEN_KEY);
  },
  
  setToken: (token: string): void => {
    sessionStorage.setItem(TOKEN_KEY, token);
  },
  
  clearToken: (): void => {
    sessionStorage.removeItem(TOKEN_KEY);
  },
  
  isAuthenticated: (): boolean => {
    return !!sessionStorage.getItem(TOKEN_KEY);
  }
};
