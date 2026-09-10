import api from './api'

export type UserRole = 'Admin' | 'Manager' | 'Cashier' | 'Stock'

export interface ManagedUser {
  id: string
  username: string
  fullName: string
  email: string
  role: UserRole
  isActive: boolean
  createdAt: string
  updatedAt: string
}

export interface CreateManagedUserInput {
  username: string
  fullName: string
  email: string
  password: string
  role: UserRole
}

export const usersService = {
  list: async () => (await api.get<{ data: ManagedUser[] }>('/users')).data.data,
  create: async (input: CreateManagedUserInput) => (await api.post<{ data: string }>('/users', input)).data.data,
  changeRole: async (id: string, role: UserRole) => api.put(`/users/${id}/role`, { role }),
  setActive: async (id: string, isActive: boolean) => api.put(`/users/${id}/active`, { isActive }),
  resetPassword: async (id: string, newPassword: string) => api.put(`/users/${id}/password`, { newPassword }),
}
