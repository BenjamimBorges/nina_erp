import api from './api'
import type { ApiResponse } from '../types'

export interface Department {
  id: string;
  name: string;
  description?: string;
  productCount: number;
}

export const departmentsService = {
  getAll: () =>
    api.get<ApiResponse<Department[]>>('/departments').then(r => r.data),
  create: (data: Omit<Department, 'id' | 'productCount'>) =>
    api.post<ApiResponse<string>>('/departments', data).then(r => r.data),
}
