
import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthResponse } from '../types'
import api from '../services/api'

interface AuthState {
  user: AuthResponse | null
  token: string | null
  isAuthenticated: boolean
  login: (username: string, password: string) => Promise<void>
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: localStorage.getItem('token'),
      isAuthenticated: Boolean(localStorage.getItem('token')),

      login: async (username, password) => {
        const { data } = await api.post<{ success: boolean; data: AuthResponse }>(
          '/auth/login',
          { username, password }
        )

        localStorage.setItem('token', data.data.token)

        set({
          user: data.data,
          token: data.data.token,
          isAuthenticated: true
        })
      },

      logout: () => {
        localStorage.removeItem('token')

        set({
          user: null,
          token: null,
          isAuthenticated: false
        })
      }
    }),
    {
      name: 'nina-erp-auth'
    }
  )
)
