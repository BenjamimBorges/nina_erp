import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: { 'X-Requested-With': 'XMLHttpRequest' }
})

api.interceptors.response.use(
  response => response,
  error => {
    const requestUrl = String(error.config?.url ?? '')
    const isSessionProbe = requestUrl.includes('/auth/me') || requestUrl.includes('/auth/login')
    if (error.response?.status === 401 && !isSessionProbe && window.location.pathname !== '/login') {
      window.location.assign('/login')
    }
    return Promise.reject(error)
  }
)

export default api
