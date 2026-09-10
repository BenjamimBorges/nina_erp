import { create } from 'zustand'
import type { AuthResponse } from '../types'
import api from '../services/api'

interface AuthState {
  user: AuthResponse | null
  isAuthenticated: boolean
  initialized: boolean
  login: (companyCnpj: string, username: string, password: string) => Promise<void>
  logout: () => Promise<void>
  bootstrap: () => Promise<void>
}

export const useAuthStore = create<AuthState>()((set) => ({
  user: null,
  isAuthenticated: false,
  initialized: false,

  login: async (companyCnpj, username, password) => {
    const { data } = await api.post<{ success: boolean; data: AuthResponse }>(
      '/auth/login',
      { companyCnpj, username, password }
    )
    set({ user: data.data, isAuthenticated: true, initialized: true })
  },

  logout: async () => {
    try {
      await api.post('/auth/logout')
    } finally {
      set({ user: null, isAuthenticated: false, initialized: true })
    }
  },

  bootstrap: async () => {
    try {
      const { data } = await api.get<{ success: boolean; data: AuthResponse }>('/auth/me')
      set({ user: data.data, isAuthenticated: true, initialized: true })
    } catch {
      set({ user: null, isAuthenticated: false, initialized: true })
    }
  }
}))
