
import { Routes, Route, Navigate } from 'react-router-dom'
import { useAuthStore } from './store/auth'

import LoginPage from './pages/LoginPage'
import Layout from './components/shared/Layout'

import DashboardPage from './pages/dashboard/DashboardPage'
import CadastroProdutoPage from './pages/admin/CadastroProdutoPage'
import ClientsPage from './pages/admin/ClientsPage'
import SuppliersPage from './pages/admin/SuppliersPage'
import StockPage from './pages/stock/StockPage'
import PosPage from './pages/sales/PosPage'
import CashPage from './pages/operations/CashPage'
import PurchasesPage from './pages/operations/PurchasesPage'
import FinancePage from './pages/operations/FinancePage'
import FiscalPage from './pages/operations/FiscalPage'
import ReportsPage from './pages/operations/ReportsPage'
import SettingsPage from './pages/operations/SettingsPage'


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
        <Route path="cadastro-produto" element={<CadastroProdutoPage />} />
        <Route path="clients" element={<ClientsPage />} />
        <Route path="suppliers" element={<SuppliersPage />} />
        <Route path="stock" element={<StockPage />} />
        <Route path="pos" element={<PosPage />} />
        <Route path="cash" element={<CashPage />} />
        <Route path="purchases" element={<PurchasesPage />} />
        <Route path="finance" element={<FinancePage />} />
        <Route path="fiscal" element={<FiscalPage />} />
        <Route path="reports" element={<ReportsPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
    </Routes>
  )
}
