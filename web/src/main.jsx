import React, { Component, StrictMode, createContext, useContext, useEffect, useMemo, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Link, Navigate, Route, Routes, useNavigate } from 'react-router-dom';
import './styles.css';

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '');
const SessionContext = createContext(null);

function ApiError(message, status, details) {
  const error = new Error(message);
  error.status = status;
  error.details = details;
  return error;
}

async function request(path, { csrfToken, method = 'GET', body } = {}) {
  const headers = { Accept: 'application/json' };
  if (csrfToken) headers['X-CSRF-TOKEN'] = csrfToken;
  if (body !== undefined) headers['Content-Type'] = 'application/json';

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
    credentials: 'include'
  });

  if (response.status === 204) return undefined;

  const contentType = response.headers.get('content-type') || '';
  const payload = contentType.includes('application/json') ? await response.json() : undefined;
  if (response.status === 401) throw ApiError('UNAUTHENTICATED', response.status, payload);
  if (response.status === 403) throw ApiError('FORBIDDEN', response.status, payload);
  if (!response.ok) throw ApiError('REQUEST_FAILED', response.status, payload);
  return payload;
}

async function getCsrfToken() {
  const payload = await request('/api/auth/csrf');
  const token = typeof payload === 'string' ? payload : payload?.token;
  if (!token) throw ApiError('CSRF_UNAVAILABLE');
  return token;
}

function SessionProvider({ children }) {
  const [status, setStatus] = useState('loading');
  const [user, setUser] = useState(null);
  const [notice, setNotice] = useState('');

  const bootstrap = async () => {
    setStatus('loading');
    try {
      const identity = await request('/api/auth/me');
      setUser(identity);
      setNotice('');
      setStatus('authenticated');
    } catch (error) {
      setUser(null);
      setStatus('anonymous');
      setNotice(error.status === 401 ? '' : 'Não foi possível verificar sua sessão. Tente novamente.');
    }
  };

  useEffect(() => { bootstrap(); }, []);

  const login = async ({ cnpj, username, password }) => {
    const csrfToken = await getCsrfToken();
    await request('/api/auth/login', {
      method: 'POST',
      csrfToken,
      body: { cnpj, username, password }
    });
    await bootstrap();
  };

  const logout = async () => {
    try {
      const csrfToken = await getCsrfToken();
      await request('/api/auth/logout', { method: 'POST', csrfToken });
      setNotice('Sessão encerrada.');
    } catch (error) {
      if (error.status === 401) setNotice('Sua sessão expirou. Entre novamente.');
      else throw error;
    } finally {
      setUser(null);
      setStatus('anonymous');
    }
  };

  const value = useMemo(() => ({ status, user, notice, login, logout }), [status, user, notice]);
  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

function useSession() {
  const session = useContext(SessionContext);
  if (!session) throw new Error('SessionProvider ausente.');
  return session;
}

function PageLoading() {
  return <main className="centered" aria-live="polite"><p>Verificando sessão…</p></main>;
}

function ProtectedRoute({ children }) {
  const { status } = useSession();
  if (status === 'loading') return <PageLoading />;
  return status === 'authenticated' ? children : <Navigate to="/cadastro" replace />;
}

function RegistrationPage() {
  const navigate = useNavigate();
  const [form, setForm] = useState({
    companyName: '', cnpj: '', adminFullName: '', adminUsername: '', password: '', confirmPassword: ''
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const update = (field) => (event) => setForm({ ...form, [field]: event.target.value });

  const submit = async (event) => {
    event.preventDefault();
    const cnpj = form.cnpj.replace(/[^0-9A-Za-z]/g, '').toUpperCase();
    const passwordIsValid = form.password.length >= 6;

    if (form.companyName.trim().length < 2 || form.adminFullName.trim().length < 2 || cnpj.length !== 14 ||
      !/^[A-Za-z0-9._-]{3,100}$/.test(form.adminUsername.trim()) || !passwordIsValid || form.password !== form.confirmPassword) {
      setError('Revise os dados: CNPJ válido, usuário e senha de ao menos 6 caracteres com confirmação são obrigatórios.');
      return;
    }

    setSubmitting(true);
    setError('');
    try {
      const csrfToken = await getCsrfToken();
      const result = await request('/api/auth/register', {
        method: 'POST',
        csrfToken,
        body: {
          companyName: form.companyName.trim(),
          cnpj,
          adminUsername: form.adminUsername.trim(),
          adminFullName: form.adminFullName.trim(),
          password: form.password,
          confirmPassword: form.confirmPassword
        }
      });
      setSuccess(result?.message || 'Empresa cadastrada com sucesso. Faça login para continuar.');
    } catch (requestError) {
      if (requestError.status === 409) setError('Não foi possível concluir o cadastro. Revise os dados informados.');
      else if (requestError.status === 429) setError('Muitas tentativas. Aguarde alguns minutos antes de tentar novamente.');
      else if (requestError.status === 400) setError('Revise os dados do cadastro e tente novamente.');
      else setError('Não foi possível cadastrar a empresa agora. Tente novamente.');
    } finally {
      setSubmitting(false);
    }
  };

  if (success) {
    return <main className="login-shell">
      <section className="card" aria-live="polite">
        <p className="eyebrow">Nina ERP</p>
        <h1>Cadastro concluído</h1>
        <p className="muted">{success}</p>
        <button type="button" onClick={() => navigate('/login')}>Ir para acesso</button>
      </section>
    </main>;
  }

  return <main className="login-shell">
    <form className="card registration-card" onSubmit={submit} noValidate>
      <p className="eyebrow">Nina ERP</p>
      <h1>Cadastre sua empresa</h1>
      <p className="muted">Crie a empresa e a primeira conta administradora.</p>
      {error && <p className="message" role="alert">{error}</p>}
      <label htmlFor="companyName">Nome da empresa</label>
      <input id="companyName" autoComplete="organization" value={form.companyName} onChange={update('companyName')} required />
      <label htmlFor="registrationCnpj">CNPJ</label>
      <input id="registrationCnpj" inputMode="text" autoComplete="organization" value={form.cnpj} onChange={update('cnpj')} required />
      <label htmlFor="adminFullName">Nome do administrador</label>
      <input id="adminFullName" autoComplete="name" value={form.adminFullName} onChange={update('adminFullName')} required />
      <label htmlFor="adminUsername">Usuário administrador</label>
      <input id="adminUsername" autoComplete="username" value={form.adminUsername} onChange={update('adminUsername')} required />
      <label htmlFor="registrationPassword">Senha</label>
      <input id="registrationPassword" type="password" autoComplete="new-password" value={form.password} onChange={update('password')} required />
      <p className="hint">Use ao menos 6 caracteres.</p>
      <label htmlFor="confirmPassword">Confirmar senha</label>
      <input id="confirmPassword" type="password" autoComplete="new-password" value={form.confirmPassword} onChange={update('confirmPassword')} required />
      <button type="submit" disabled={submitting}>{submitting ? 'Cadastrando…' : 'Cadastrar empresa'}</button>
      <p className="muted account-link">Já possui cadastro? <Link to="/login">Acessar empresa</Link></p>
    </form>
  </main>;
}

function LoginPage() {
  const navigate = useNavigate();
  const { login, status, notice } = useSession();
  const [form, setForm] = useState({ cnpj: '', username: '', password: '' });
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    if (status === 'authenticated') navigate('/');
  }, [status, navigate]);

  const submit = async (event) => {
    event.preventDefault();
    const cnpj = form.cnpj.replace(/\D/g, '');
    if (cnpj.length !== 14 || !form.username.trim() || !form.password) {
      setError('Informe CNPJ, usuário e senha.');
      return;
    }
    setSubmitting(true);
    setError('');
    try {
      await login({ cnpj, username: form.username.trim(), password: form.password });
      navigate('/');
    } catch (requestError) {
      setError(requestError.status === 401
        ? 'CNPJ, usuário ou senha inválidos.'
        : 'Não foi possível entrar agora. Tente novamente.');
    } finally {
      setSubmitting(false);
    }
  };

  return <main className="login-shell">
    <form className="card" onSubmit={submit} noValidate>
      <p className="eyebrow">Nina ERP</p>
      <h1>Acessar empresa</h1>
      <p className="muted">Use as credenciais fornecidas pela sua empresa.</p>
      {(error || notice) && <p className="message" role="alert">{error || notice}</p>}
      <label htmlFor="cnpj">CNPJ</label>
      <input id="cnpj" inputMode="numeric" autoComplete="organization" value={form.cnpj}
        onChange={(event) => setForm({ ...form, cnpj: event.target.value })} required />
      <label htmlFor="username">Usuário</label>
      <input id="username" autoComplete="username" value={form.username}
        onChange={(event) => setForm({ ...form, username: event.target.value })} required />
      <label htmlFor="password">Senha</label>
      <input id="password" type="password" autoComplete="current-password" value={form.password}
        onChange={(event) => setForm({ ...form, password: event.target.value })} required />
      <button type="submit" disabled={submitting}>{submitting ? 'Entrando…' : 'Entrar'}</button>
      <p className="muted account-link">Ainda não possui empresa? <Link to="/cadastro">Cadastre-se</Link></p>
    </form>
  </main>;
}

function DashboardPage() {
  const navigate = useNavigate();
  const { user, logout } = useSession();
  const [leaving, setLeaving] = useState(false);
  const [error, setError] = useState('');
  const leave = async () => {
    setLeaving(true);
    setError('');
    try {
      await logout();
      navigate('/login', { replace: true });
    } catch (requestError) {
      if (requestError.status === 401) navigate('/login', { replace: true });
      else setError('Não foi possível encerrar a sessão. Tente novamente.');
    } finally {
      setLeaving(false);
    }
  };
  return <main className="app-shell">
    <header className="topbar"><strong>Nina ERP</strong><button className="secondary" onClick={leave} disabled={leaving}>{leaving ? 'Saindo…' : 'Sair'}</button></header>
    <section className="content">
      <p className="eyebrow">Área protegida</p>
      <h1>Olá, {user?.fullName || user?.username}.</h1>
      <p className="muted">Empresa ativa: <strong>{user?.companyName || 'Não informada'}</strong></p>
      <p className="muted">Perfil: {user?.role || 'Não informado'}</p>
      {error && <p className="message" role="alert">{error}</p>}
    </section>
  </main>;
}

function App() {
  return <SessionProvider><BrowserRouter><Routes>
    <Route path="/cadastro" element={<RegistrationPage />} />
    <Route path="/login" element={<LoginPage />} />
    <Route path="/" element={<ProtectedRoute><DashboardPage /></ProtectedRoute>} />
    <Route path="*" element={<Navigate to="/" replace />} />
  </Routes></BrowserRouter></SessionProvider>;
}

class ApplicationErrorBoundary extends Component {
  constructor(props) {
    super(props);
    this.state = { failed: false };
  }

  static getDerivedStateFromError() {
    return { failed: true };
  }

  render() {
    if (this.state.failed) {
      return <main className="centered" role="alert">
        <section className="card">
          <p className="eyebrow">Nina ERP</p>
          <h1>Não foi possível abrir a aplicação</h1>
          <p className="muted">Atualize a página. Se o problema continuar, informe este erro ao suporte.</p>
          <button type="button" onClick={() => window.location.reload()}>Atualizar página</button>
        </section>
      </main>;
    }

    return this.props.children;
  }
}

const root = createRoot(document.getElementById('root'));
window.__ninaReactMounted?.();

root.render(
  <StrictMode><ApplicationErrorBoundary><App /></ApplicationErrorBoundary></StrictMode>
);

if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => {
    navigator.serviceWorker.register('/sw.js').catch(() => {
      // A aplicação continua online se o cache do shell não puder ser preparado.
    });
  });
}
