import { create } from 'zustand'

import { persist } from 'zustand/middleware'

export interface UserData {
  id: string;
  email: string;
  fullName?: string;
  employeeId?: string;
  role: string;
  permissions: string[];
}

interface AuthState {
  accessToken: string | null;
  refreshToken: string | null;
  user: UserData | null;
  setAuth: (accessToken: string, refreshToken: string, user: UserData) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      accessToken: null,
      refreshToken: null,
      user: null,
      setAuth: (accessToken, refreshToken, user) => set({ accessToken, refreshToken, user }),
      clearAuth: () => set({ accessToken: null, refreshToken: null, user: null }),
    }),
    {
      name: 'auth-storage'
    }
  )
);
