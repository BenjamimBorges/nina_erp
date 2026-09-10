import { useQuery } from '@tanstack/react-query'
import { fiscalService } from '../../services/operations'

export default function FiscalPage(){
  const readiness=useQuery({queryKey:['fiscal','readiness'],queryFn:fiscalService.readiness})
  const dashboard=useQuery({queryKey:['fiscal','dashboard'],queryFn:fiscalService.dashboard})
  const docs=useQuery({queryKey:['fiscal','docs'],queryFn:fiscalService.documents})
  const guides=useQuery({queryKey:['fiscal','guides'],queryFn:fiscalService.guides})
  const x=dashboard.data?.data; const r=readiness.data?.data
  return <div className="p-6 max-w-7xl mx-auto space-y-6">
    <div><h2 className="text-3xl font-semibold">Central Fiscal</h2><p className="text-slate-500">NF-e/NFC-e, eventos, rejeições, contingência e guias. O painel diferencia configuração de homologação real.</p></div>
    <section className={`rounded-2xl border p-5 ${r?.canAttemptEmission?'bg-emerald-50 border-emerald-200':'bg-amber-50 border-amber-200'}`}><div className="flex flex-wrap items-start justify-between gap-4"><div><p className="text-xs uppercase tracking-wider text-slate-500">Readiness fiscal</p><h3 className="mt-1 text-lg font-semibold">{r?.canAttemptEmission?'Pré-requisitos mínimos presentes':'Emissão bloqueada por pré-requisitos'}</h3><p className="text-sm text-slate-600 mt-1">Provedor: {r?.provider??'Não configurado'} · ambiente: {r?.environment??'-'} · perfis tributários: {r?.activeTaxProfiles??0}</p></div><span className="rounded-full bg-white/70 px-3 py-1.5 text-xs font-medium">Não equivale a homologação SEFAZ</span></div>{(r?.blockingReasons??[]).length>0&&<ul className="mt-4 list-disc pl-5 text-sm text-amber-900 space-y-1">{r.blockingReasons.map((reason:string)=><li key={reason}>{reason}</li>)}</ul>}</section>
    <div className="grid md:grid-cols-5 gap-3">{[['Autorizados',x?.authorized],['Pendentes',x?.pending],['Rejeitados',x?.rejected],['Cancelados',x?.cancelled],['Contingência',x?.contingency]].map(([a,b])=><div key={String(a)} className="rounded-2xl border bg-white p-4"><p className="text-xs text-slate-500">{a}</p><p className="text-2xl font-semibold mt-2">{b??0}</p></div>)}</div>
    <Section title="Documentos fiscais" rows={docs.data?.data??[]}/><Section title="Guias e obrigações" rows={guides.data?.data??[]}/>
  </div>
}
function Section({title,rows}:{title:string;rows:any[]}){return <section className="rounded-2xl border bg-white p-5"><h3 className="font-semibold">{title}</h3><div className="mt-3 space-y-2">{rows.slice(0,20).map((x,i)=><div key={x.id??i} className="rounded-xl bg-slate-50 p-3 text-sm flex justify-between gap-3"><span>{x.taxType??`${x.model} · série ${x.series} · nº ${x.number}`}</span><span className="font-medium">{x.status}</span></div>)}{!rows.length&&<p className="text-sm text-slate-400 py-6">Nenhum registro.</p>}</div></section>}
