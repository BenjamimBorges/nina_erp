import { useQuery } from '@tanstack/react-query'
import { reportsService } from '../../services/operations'

const money=new Intl.NumberFormat('pt-BR',{style:'currency',currency:'BRL'})
export default function ReportsPage(){
  const to=new Date().toISOString(); const from=new Date(Date.now()-30*86400000).toISOString()
  const daily=useQuery({queryKey:['reports','daily',from,to],queryFn:()=>reportsService.daily(from,to)})
  const financial=useQuery({queryKey:['reports','financial'],queryFn:reportsService.financial})
  const sellers=useQuery({queryKey:['reports','sellers',from,to],queryFn:()=>reportsService.sellers(from,to)})
  const payments=useQuery({queryKey:['reports','payment-mix',from,to],queryFn:()=>reportsService.paymentMix(from,to)})
  const fin=financial.data?.data
  return <div className="p-6 max-w-7xl mx-auto space-y-6">
    <div><h2 className="text-3xl font-semibold">Relatórios gerenciais</h2><p className="text-slate-500">Métricas oficiais por views PostgreSQL, sempre filtradas por empresa. BI avançado usa um read model separado.</p></div>
    <div className="grid md:grid-cols-4 gap-3">{[['Pagar aberto',fin?.payablesOpen],['Receber aberto',fin?.receivablesOpen],['Pago',fin?.payablesPaid],['Recebido',fin?.receivablesReceived]].map(([t,v])=><div key={String(t)} className="rounded-2xl border bg-white p-4"><p className="text-xs text-slate-500">{t}</p><p className="mt-2 text-xl font-semibold">{money.format(Number(v??0))}</p></div>)}</div>
    <div className="grid xl:grid-cols-2 gap-5"><Table title="Vendedores · 30 dias" heads={['Vendedor','Vendas','Líquido','TKM']} rows={(sellers.data?.data??[]).map((x:any)=>[x.sellerName,x.salesCount,money.format(x.netSales),money.format(x.averageTicket)])}/><Table title="Meios de pagamento · 30 dias" heads={['Meio','Transações','Capturado','Estornado','Líquido']} rows={(payments.data?.data??[]).map((x:any)=>[x.method,x.transactions,money.format(x.grossCaptured),money.format(x.refunded),money.format(x.netReceived)])}/></div>
    <section className="rounded-2xl border bg-white overflow-hidden"><h3 className="p-4 border-b font-semibold">Vendas diárias · 30 dias</h3><table className="w-full text-sm"><thead className="bg-slate-50"><tr><th className="p-3 text-left">Data</th><th className="p-3 text-right">Bruta</th><th className="p-3 text-right">Descontos</th><th className="p-3 text-right">Líquida</th><th className="p-3 text-right">Recebido</th></tr></thead><tbody>{(daily.data?.data??[]).map((x:any)=><tr key={x.date} className="border-t"><td className="p-3">{new Date(x.date).toLocaleDateString('pt-BR')}</td><td className="p-3 text-right">{money.format(x.grossSales)}</td><td className="p-3 text-right">{money.format(x.discounts)}</td><td className="p-3 text-right">{money.format(x.netSales)}</td><td className="p-3 text-right">{money.format(x.received)}</td></tr>)}</tbody></table></section>
  </div>
}
function Table({title,heads,rows}:{title:string;heads:string[];rows:any[][]}){return <section className="rounded-2xl border bg-white overflow-hidden"><h3 className="p-4 border-b font-semibold">{title}</h3><div className="overflow-auto"><table className="w-full text-sm"><thead className="bg-slate-50"><tr>{heads.map(h=><th className="p-3 text-left whitespace-nowrap" key={h}>{h}</th>)}</tr></thead><tbody>{rows.map((r,i)=><tr className="border-t" key={i}>{r.map((v,j)=><td className="p-3 whitespace-nowrap" key={j}>{v}</td>)}</tr>)}</tbody></table></div>{!rows.length&&<p className="p-6 text-sm text-slate-400">Sem dados no período.</p>}</section>}
