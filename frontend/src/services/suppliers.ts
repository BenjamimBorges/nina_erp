import api from './api'
import type { ApiResponse, Supplier } from '../types'

export const suppliersService = {
  getAll: (search?: string) =>
    api.get<ApiResponse<Supplier[]>>('/suppliers', { params: { search } }).then(r => r.data)
}
