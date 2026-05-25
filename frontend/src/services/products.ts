import api from './api'
import type { ApiResponse, Product } from '../types'

export const productsService = {
  getAll: (search?: string) =>
    api.get<ApiResponse<Product[]>>('/products', { params: { search } }).then(r => r.data),
  getLowStock: () =>
    api.get<ApiResponse<Product[]>>('/products/low-stock').then(r => r.data),
  create: (data: Omit<Product, 'id' | 'costAverage' | 'stockQty' | 'isActive'>) =>
    api.post<ApiResponse<string>>('/products', data).then(r => r.data),
  update: (id: string, data: Partial<Product>) =>
    api.put<void>(`/products/${id}`, data),
  delete: (id: string) =>
    api.delete<void>(`/products/${id}`)
}
