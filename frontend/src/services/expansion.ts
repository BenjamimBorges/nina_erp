import api from './api'
import type { ApiResponse } from '../types'

export const approvalsService = {
  list: (status?: string) => api.get<ApiResponse<any[]>>('/approvals', { params: { status } }).then(r => r.data),
  request: (value: any) => api.post<ApiResponse<any>>('/approvals', value).then(r => r.data),
  decide: (id: string, approve: boolean, notes?: string) => api.post<ApiResponse<any>>(`/approvals/${id}/decision`, { approve, notes }).then(r => r.data),
}

export const omnichannelService = {
  catalog: () => api.get<ApiResponse<any[]>>('/omnichannel/catalog', { params: { limit: 1000 } }).then(r => r.data),
  orders: () => api.get<ApiResponse<any[]>>('/omnichannel/orders').then(r => r.data),
  receive: (value: any) => api.post<ApiResponse<any>>('/omnichannel/orders', value).then(r => r.data),
}

export const mobileService = {
  summary: () => api.get<ApiResponse<any>>('/mobile/summary').then(r => r.data),
  recentSales: () => api.get<ApiResponse<any[]>>('/mobile/sales/recent').then(r => r.data),
  lowStock: () => api.get<ApiResponse<any[]>>('/mobile/stock/low').then(r => r.data),
}

export const biService = {
  executive: (months = 12) => api.get<ApiResponse<any>>('/bi/executive', { params: { months } }).then(r => r.data),
  sellers: (months = 12) => api.get<ApiResponse<any[]>>('/bi/sellers', { params: { months } }).then(r => r.data),
  payments: (months = 12) => api.get<ApiResponse<any[]>>('/bi/payments', { params: { months } }).then(r => r.data),
  refresh: () => api.post<ApiResponse<any>>('/bi/refresh').then(r => r.data),
}

export const assistantService = {
  ask: (question: string) => api.post<ApiResponse<any>>('/assistant/ask', { question }).then(r => r.data),
}
