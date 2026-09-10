import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { approvalsService } from '../../services/expansion'
import { useAuthStore } from '../../store/auth'

export default function ApprovalsPage() {
  const qc = useQueryClient(); const user = useAuthStore(s => s.user)
  const query = useQuery({ queryKey: ['approvals'], queryFn: () => approvalsService.list() })
  const [reason, setReason] = useState('Solicitação operacional para análise do responsável.')
  const [resourceId, setResourceId] = useState('')
  const create = useMutation({ mutationFn: () => approvalsService.request({ type: 'CashMovement', resourceType: 'CashSession', resourceId, requiredRole: 'Manager', reason, payloadJson: null, idempotencyKey: crypto.randomUUID(), expiresAt: new Date(Date.now()+3600000).toISOString() }), onSuccess: () => qc.invalidateQueries({queryKey:['approvals']}) })
  const decide = useMutation({ mutationFn: ({id,approve}:{id:string;approve:boolean}) => approvalsService.decide(id, approve), onSuccess: () => qc.invalidateQueries({queryKey:['approvals']}) })
  const rows = query.data?.data ?? []
  const pending = useMemo(()=>rows.filter((x:any)=>x.status==='Pending'),[rows])
  return <div className="p-6 max-w-7xl mx-auto space-y-6"><div><h2 className="text-3xl font-semibold">Aprovações</h2><p className="text-slate-500">Fluxo de quatro-olhos para exceções operacionais. O solicitante não aprova a própria solicitação.</p></div>
    <div className="grid lg:grid-cols-3 gap-4"><section className="rounded-2xl border bg-white p-5 lg:col-span-1"><h3 className="font-semibold">Solicitar aprovação</h3><p className="text-xs text-slate-500 mt-1">Exemplo: sangria/ajuste de uma sessão de caixa.</p><input className="mt-4 w-full rounded-xl border p-3" placeholder="ID da sessão/recurso" value={resourceId} onChange={e=>setResourceId(e.target.value)}/><textarea className="mt-3 w-full rounded-xl border p-3 min-h-24" value={reason} onChange={e=>setReason(e.target.value)}/><button className="mt-3 rounded-xl bg-indigo-600 text-white px-4 py-2.5 disabled:opacity-40" disabled={!resourceId || create.isPending} onClick={()=>create.mutate()}>Enviar para gerente</button></section>
    <section className="rounded-2xl border bg-white p-5 lg:col-span-2"><div className="flex justify-between"><h3 className="font-semibold">Pendentes</h3><span className="text-sm text-slate-500">{pending.length}</span></div><div className="mt-3 space-y-2">{pending.map((x:any)=><div className="rounded-xl border p-4" key={x.id}><div className="flex flex-wrap justify-between gap-2"><div><p className="font-medium">{x.type} · {x.resourceType}</p><p className="text-sm text-slate-500">{x.reason}</p><p className="text-xs text-slate-400 mt-1">Exige {x.requiredRole}</p></div>{(user?.role==='Admin'||user?.role==='Manager')&&<div className="flex gap-2"><button className="rounded-lg bg-emerald-600 text-white px-3 py-2 text-sm" onClick={()=>decide.mutate({id:x.id,approve:true})}>Aprovar</button><button className="rounded-lg bg-rose-50 text-rose-700 px-3 py-2 text-sm" onClick={()=>decide.mutate({id:x.id,approve:false})}>Rejeitar</button></div>}</div></div>)}{!pending.length&&<p className="text-sm text-slate-400 py-8">Nenhuma aprovação pendente.</p>}</div></section></div></div>
}
