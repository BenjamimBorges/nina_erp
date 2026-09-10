import { useQuery } from '@tanstack/react-query'
import { Activity, CheckCircle2, Database, ShieldCheck, TriangleAlert } from 'lucide-react'
import { readinessService } from '../../services/readiness'

export default function QualityPage(){
 const q=useQuery({queryKey:['readiness'],queryFn:readinessService.get,refetchInterval:30000})
 const r=q.data
 return <div className="p-6 max-w-7xl mx-auto space-y-6">
  <div><h2 className="text-3xl font-semibold flex items-center gap-3"><Activity size={28}/> Qualidade e prontidão</h2><p className="mt-2 text-sm text-slate-500">Painel operacional do candidato comercial. Não substitui o resultado do CI, homologação fiscal ou teste Windows.</p></div>
  {q.isLoading && <div className="rounded-2xl border bg-white p-6">Carregando diagnóstico...</div>}
  {r && <>
   <div className="grid md:grid-cols-4 gap-4">
    <Card title="Versão" value={r.version} ok />
    <Card title="Banco" value={r.database.connected?'Conectado':'Indisponível'} ok={r.database.connected}/>
    <Card title="Migrations" value={r.database.pendingMigrations.length?`${r.database.pendingMigrations.length} pendente(s)`:'Em dia'} ok={!r.database.pendingMigrations.length}/>
    <Card title="Outbox falha" value={String(r.commercialCore.failedOutbox)} ok={r.commercialCore.failedOutbox===0}/>
   </div>
   <div className="grid lg:grid-cols-2 gap-6">
    <section className="rounded-2xl border bg-white p-5"><h3 className="font-semibold flex items-center gap-2"><Database size={18}/> Núcleo comercial</h3><div className="mt-4 grid grid-cols-2 gap-3 text-sm"><Metric label="Usuários" value={r.commercialCore.users}/><Metric label="Produtos" value={r.commercialCore.products}/><Metric label="Clientes" value={r.commercialCore.clients}/><Metric label="Caixas abertos" value={r.commercialCore.openCashSessions}/></div></section>
    <section className="rounded-2xl border bg-white p-5"><h3 className="font-semibold flex items-center gap-2"><ShieldCheck size={18}/> Integrações críticas</h3><div className="mt-4 space-y-3 text-sm"><Status label="Fiscal" ok={r.fiscal.configured} text={r.fiscal.configured?r.fiscal.provider:'não homologado/configurado'}/><Status label="Pagamentos externos" ok={r.payments.configuredAdapters.length>0} text={r.payments.configuredAdapters.length?r.payments.configuredAdapters.join(', '):'nenhum adapter registrado'}/><Status label="Perfis tributários ativos" ok={r.fiscal.activeTaxProfiles>0} text={String(r.fiscal.activeTaxProfiles)}/></div></section>
   </div>
   <section className="rounded-2xl border bg-white p-5"><h3 className="font-semibold flex items-center gap-2"><ShieldCheck size={18}/> Segurança da sessão</h3><div className="mt-4 grid md:grid-cols-4 gap-3"><Status label="JWT no navegador" ok={r.security.sessionCookieHttpOnly} text={r.security.sessionCookieHttpOnly?'HttpOnly':'exposto'}/><Status label="CSRF" ok={r.security.csrfHeaderGuard} text={r.security.csrfHeaderGuard?'protegido':'pendente'}/><Status label="TLS" ok={r.security.tlsActive} text={r.security.tlsActive?'ativo':'HTTP/HML'}/><Status label="Cookie Secure" ok={r.security.secureCookie} text={r.security.secureCookie?'ativo':'pendente para produção'}/></div>{!r.security.productionTransportReady&&<p className="mt-4 rounded-xl bg-amber-50 p-3 text-sm text-amber-800">Transporte ainda não atende ao gate de produção. Configure HTTPS/TLS e cookie Secure antes do go-live.</p>}</section>
   <section className="rounded-2xl border bg-white p-5"><h3 className="font-semibold">Etapas 20–30 · telemetria</h3><div className="mt-4 grid md:grid-cols-5 gap-3"><Metric label="Aprovações pendentes" value={r.expansion.pendingApprovals}/><Metric label="Pedidos omnichannel" value={r.expansion.channelOrders}/><Metric label="Pedidos rejeitados" value={r.expansion.rejectedChannelOrders}/><Metric label="IA bloqueada · 24h" value={r.expansion.blockedAssistantInteractions24h}/><Metric label="Nós saudáveis" value={r.expansion.healthyClusterNodes}/></div><p className="mt-4 text-xs text-slate-400">Runtime: {r.runtime.framework} · {r.runtime.processArchitecture}</p></section>
   <section className="rounded-2xl border bg-white p-5"><h3 className="font-semibold">Expansão controlada</h3><p className="mt-1 text-sm text-slate-500">Recursos de expansão permanecem desligados até validação específica.</p><div className="mt-4 grid md:grid-cols-3 gap-3">{Object.entries(r.features).map(([k,v])=><div key={k} className="rounded-xl border p-3 flex items-center justify-between text-sm"><span>{k}</span><span className={v?'text-emerald-700':'text-slate-500'}>{v?'Ativo':'Bloqueado'}</span></div>)}</div></section>
  </>}
 </div>
}
function Card({title,value,ok}:{title:string,value:string,ok:boolean}){return <div className="rounded-2xl border bg-white p-5"><div className="flex items-center justify-between">{ok?<CheckCircle2 className="text-emerald-600" size={20}/>:<TriangleAlert className="text-amber-600" size={20}/>}<span className="text-xs text-slate-400">{title}</span></div><p className="mt-3 text-xl font-semibold">{value}</p></div>}
function Metric({label,value}:{label:string,value:number}){return <div className="rounded-xl bg-slate-50 p-3"><p className="text-slate-500">{label}</p><p className="text-xl font-semibold mt-1">{value}</p></div>}
function Status({label,ok,text}:{label:string,ok:boolean,text:string}){return <div className="flex items-center justify-between rounded-xl bg-slate-50 p-3"><span>{label}</span><span className={ok?'text-emerald-700':'text-amber-700'}>{text}</span></div>}
