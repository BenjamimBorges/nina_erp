import api from './api'

export interface Readiness {
  version: string
  environment: string
  database: { connected: boolean; pendingMigrations: string[] }
  commercialCore: { users: number; products: number; clients: number; openCashSessions: number; failedOutbox: number }
  fiscal: { configured: boolean; provider: string; activeTaxProfiles: number }
  payments: { configuredAdapters: string[] }
  security: {
    sessionCookieHttpOnly: boolean
    csrfHeaderGuard: boolean
    tlsActive: boolean
    secureCookie: boolean
    productionTransportReady: boolean
  }
  features: Record<string, boolean>
  safeguards: Record<string, boolean>
}

export const readinessService = {
  get: async () => (await api.get<{ data: Readiness }>('/readiness')).data.data,
}
