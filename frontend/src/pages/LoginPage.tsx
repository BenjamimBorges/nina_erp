import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthStore } from '../store/auth'

export default function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const { login } = useAuthStore()
  const navigate = useNavigate()

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()

    setLoading(true)
    setError('')

    try {
      await login(username, password)
      navigate('/dashboard')
    } catch {
      setError('Usuário ou senha inválidos.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen bg-slate-950 flex items-center justify-center overflow-hidden">
      <div className="absolute inset-0 bg-gradient-to-br from-indigo-900 via-slate-950 to-cyan-950 opacity-95" />

      <div className="relative z-10 w-full max-w-6xl grid grid-cols-1 lg:grid-cols-2 rounded-3xl overflow-hidden shadow-2xl border border-white/10 backdrop-blur-xl">

        <div className="hidden lg:flex flex-col justify-between p-14 bg-gradient-to-br from-indigo-700 via-indigo-900 to-slate-950 text-white">
          <div>
            <div className="flex items-center gap-3 mb-10">
              <div className="w-12 h-12 rounded-2xl bg-white/10 flex items-center justify-center text-2xl font-bold">
                N
              </div>

              <div>
                <h1 className="text-3xl font-bold">Nina ERP</h1>
                <p className="text-indigo-200 text-sm">
                  Gestão empresarial inteligente
                </p>
              </div>
            </div>

            <h2 className="text-5xl font-bold leading-tight mb-6">
              Controle total da sua empresa em um só lugar.
            </h2>

            <p className="text-indigo-100 text-lg leading-relaxed">
              Estoque, vendas, financeiro, clientes e relatórios integrados
              em uma plataforma moderna e profissional.
            </p>
          </div>

          <div className="grid grid-cols-3 gap-4 mt-10">
            <div className="bg-white/10 rounded-2xl p-4">
              <p className="text-3xl font-bold">99%</p>
              <p className="text-sm text-indigo-100">Precisão</p>
            </div>

            <div className="bg-white/10 rounded-2xl p-4">
              <p className="text-3xl font-bold">24h</p>
              <p className="text-sm text-indigo-100">Monitoramento</p>
            </div>

            <div className="bg-white/10 rounded-2xl p-4">
              <p className="text-3xl font-bold">ERP</p>
              <p className="text-sm text-indigo-100">Enterprise</p>
            </div>
          </div>
        </div>

        <div className="bg-white p-10 lg:p-14 flex items-center">
          <div className="w-full">
            <div className="mb-10">
              <h2 className="text-4xl font-bold text-slate-900 mb-2">
                Bem-vindo
              </h2>

              <p className="text-slate-500">
                Faça login para acessar o painel administrativo.
              </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">
                  Usuário
                </label>

                <input
                  value={username}
                  onChange={(e) => setUsername(e.target.value)}
                  className="w-full rounded-2xl border border-slate-200 px-5 py-4 outline-none focus:ring-4 focus:ring-indigo-100 focus:border-indigo-500 transition-all"
                  placeholder="Digite seu usuário"
                  required
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">
                  Senha
                </label>

                <input
                  type="password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  className="w-full rounded-2xl border border-slate-200 px-5 py-4 outline-none focus:ring-4 focus:ring-indigo-100 focus:border-indigo-500 transition-all"
                  placeholder="Digite sua senha"
                  required
                />
              </div>

              {error && (
                <div className="rounded-2xl bg-red-50 border border-red-200 p-4 text-red-600 text-sm">
                  {error}
                </div>
              )}

              <button
                type="submit"
                disabled={loading}
                className="w-full rounded-2xl bg-indigo-600 hover:bg-indigo-700 text-white font-semibold py-4 transition-all shadow-lg shadow-indigo-500/30 disabled:opacity-60"
              >
                {loading ? 'Entrando...' : 'Acessar ERP'}
              </button>
            </form>

            <div className="mt-8 p-5 rounded-2xl bg-slate-100 border border-slate-200">

            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
