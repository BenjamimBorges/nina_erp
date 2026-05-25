import api from './api'
import type { ApiResponse, DashboardSummary } from '../types'

export const dashboardService = {
  getSummary: () => api.get<ApiResponse<DashboardSummary>>('/dashboard').then(r => r.data)
}
