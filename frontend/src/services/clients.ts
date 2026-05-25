import api from './api'
import type { ApiResponse, Client } from '../types'

export const clientsService = {
  getAll: (search?: string) =>
    api.get<ApiResponse<Client[]>>('/clients', { params: { search } }).then(r => r.data),
  create: (data: Omit<Client, 'id' | 'isActive'>) =>
    api.post<ApiResponse<string>>('/clients', data).then(r => r.data),
  update: (id: string, data: Partial<Client>) =>
    api.put<void>(`/clients/${id}`, data)
}
