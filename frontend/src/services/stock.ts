import api from './api'
import type { ApiResponse } from '../types'

export const stockService = {
  adjust: (data: { productId: string; type: string; qty: number; costUnit: number; notes?: string }) =>
    api.post<ApiResponse<string>>('/stock/adjust', data).then(r => r.data)
}
