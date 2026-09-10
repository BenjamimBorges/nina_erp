import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ShieldCheck, UserPlus, Users } from 'lucide-react'
import { usersService, type UserRole } from '../../services/users'

const roles: { value: UserRole; label: string; description: string }[] = [
  { value: 'Admin', label: 'Administrador', description: 'Acesso completo e administração de usuários.' },
  { value: 'Manager', label: 'Gerente', description: 'Gestão comercial, caixa, cancelamentos e relatórios.' },
  { value: 'Stock', label: 'Estoque', description: 'Produtos, inventário, movimentações e transferências.' },
  { value: 'Cashier', label: 'Vendedor / Caixa', description: 'PDV, clientes, vendas e recebimentos permitidos.' },
]

export default function UsersPage() {
  const qc = useQueryClient()
  const users = useQuery({ queryKey: ['managed-users'], queryFn: usersService.list })
  const [form, setForm] = useState({ username: '', fullName: '', email: '', password: '', role: 'Cashier' as UserRole })
  const [message, setMessage] = useState('')

  const create = useMutation({
    mutationFn: () => usersService.create(form),
    onSuccess: async () => {
      setForm({ username: '', fullName: '', email: '', password: '', role: 'Cashier' })
      setMessage('Usuário criado com sucesso.')
      await qc.invalidateQueries({ queryKey: ['managed-users'] })
    },
    onError: () => setMessage('Não foi possível criar o usuário. Confira senha, usuário e permissões.'),
  })

  return <div className="p-6 max-w-7xl mx-auto space-y-6">
    <div>
      <h2 className="text-3xl font-semibold flex items-center gap-3"><Users size={28}/> Usuários e permissões</h2>
      <p className="mt-2 text-sm text-slate-500">Administração multiempresa protegida. Somente administradores podem alterar contas e perfis.</p>
    </div>

    <div className="grid xl:grid-cols-[1fr_420px] gap-6">
      <section className="rounded-2xl border bg-white overflow-hidden">
        <div className="px-5 py-4 border-b flex items-center justify-between"><h3 className="font-semibold">Usuários da empresa</h3><span className="text-xs text-slate-500">{users.data?.length ?? 0} cadastrados</span></div>
        <div className="divide-y">
          {users.data?.map(user => <div key={user.id} className="p-5 grid md:grid-cols-[1fr_180px_130px] gap-4 items-center">
            <div><p className="font-medium">{user.fullName}</p><p className="text-sm text-slate-500">{user.username} · {user.email || 'sem e-mail'}</p></div>
            <select value={user.role} onChange={async e => { await usersService.changeRole(user.id, e.target.value as UserRole); await qc.invalidateQueries({ queryKey: ['managed-users'] }) }} className="rounded-xl border px-3 py-2 text-sm">
              {roles.map(role => <option key={role.value} value={role.value}>{role.label}</option>)}
            </select>
            <button onClick={async () => { await usersService.setActive(user.id, !user.isActive); await qc.invalidateQueries({ queryKey: ['managed-users'] }) }} className={`rounded-xl px-3 py-2 text-sm ${user.isActive ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}>{user.isActive ? 'Ativo' : 'Inativo'}</button>
          </div>)}
          {!users.isLoading && !users.data?.length && <div className="p-8 text-sm text-slate-500">Nenhum usuário encontrado.</div>}
        </div>
      </section>

      <section className="rounded-2xl border bg-white p-5 h-fit">
        <h3 className="font-semibold flex items-center gap-2"><UserPlus size={18}/> Novo usuário</h3>
        <div className="mt-4 space-y-3">
          <input className="w-full rounded-xl border px-3 py-2.5" placeholder="Nome completo" value={form.fullName} onChange={e=>setForm({...form,fullName:e.target.value})}/>
          <input className="w-full rounded-xl border px-3 py-2.5" placeholder="Usuário" value={form.username} onChange={e=>setForm({...form,username:e.target.value})}/>
          <input className="w-full rounded-xl border px-3 py-2.5" placeholder="E-mail" type="email" value={form.email} onChange={e=>setForm({...form,email:e.target.value})}/>
          <input className="w-full rounded-xl border px-3 py-2.5" placeholder="Senha temporária forte" type="password" value={form.password} onChange={e=>setForm({...form,password:e.target.value})}/>
          <select className="w-full rounded-xl border px-3 py-2.5" value={form.role} onChange={e=>setForm({...form,role:e.target.value as UserRole})}>{roles.map(role=><option key={role.value} value={role.value}>{role.label}</option>)}</select>
          <button disabled={create.isPending} onClick={()=>create.mutate()} className="w-full rounded-xl bg-indigo-600 text-white px-4 py-2.5 disabled:opacity-50">{create.isPending ? 'Criando...' : 'Criar usuário'}</button>
          {message && <p className="text-sm text-slate-600">{message}</p>}
        </div>
        <div className="mt-5 rounded-xl bg-indigo-50 p-4 text-xs text-indigo-900"><ShieldCheck size={16} className="mb-2"/>A senha exige 12+ caracteres, maiúscula, minúscula, número e símbolo. A empresa nunca pode ficar sem um administrador ativo.</div>
      </section>
    </div>
  </div>
}
