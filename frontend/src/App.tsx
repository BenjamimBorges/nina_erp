
import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/auth'

import LoginPage from './pages/LoginPage'
import Layout from './components/shared/Layout'

import DashboardPage from './pages/dashboard/DashboardPage'
import ProductsPage from './pages/admin/ProductsPage'
import ClientsPage from './pages/admin/ClientsPage'
import SuppliersPage from './pages/admin/SuppliersPage'
import StockPage from './pages/stock/StockPage'

const PrivateRoute = ({ children }: { children: React.ReactNode }) => {
  const { isAuthenticated } = useAuthStore()

  return isAuthenticated ? (
    <>{children}</>
  ) : (
    <Navigate to="/login" replace />
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route
        path="/"
        element={
          <PrivateRoute>
            <Layout />
          </PrivateRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" />} />

        <Route path="dashboard" element={<DashboardPage />} />
        <Route path="products" element={<ProductsPage />} />
        <Route path="clients" element={<ClientsPage />} />
        <Route path="suppliers" element={<SuppliersPage />} />
        <Route path="stock" element={<StockPage />} />
      </Route>
    </Routes>
  )
}
