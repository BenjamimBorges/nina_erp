import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { clientsService } from "../../services/clients";
import { dashboardService } from "../../services/dashboard";
import { Search, Plus, Users, ShieldCheck } from "lucide-react";

export default function ClientsPage() {
  const [search, setSearch] = useState("");

  const { data: summaryData } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: dashboardService.getSummary,
    staleTime: 1000 * 60 * 2,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["clients", search],
    queryFn: () => clientsService.getAll(search || undefined),
  });

  const clients = data?.data ?? [];
  const summary = summaryData?.data;

  return (
    <div className="p-6 space-y-6">
      <header className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <h2 className="text-2xl font-semibold text-slate-900">Clientes</h2>
          <p className="text-sm text-slate-500">
            Controle os clientes ativos e seus limites de crédito.
          </p>
        </div>
        <button className="inline-flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-2xl text-sm font-semibold hover:bg-indigo-700 transition-colors">
          <Plus size={16} /> Novo cliente
        </button>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Total de clientes</span>
            <Users size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {summary ? summary.clientsCount : "—"}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Clientes com crédito</span>
            <ShieldCheck size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {clients.filter((c) => c.creditLimit > 0).length}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="text-slate-500">Busca rápida</div>
          <div className="mt-5 text-sm text-slate-600">
            Use o campo abaixo para filtrar clientes por CPF/CNPJ, nome ou
            telefone.
          </div>
        </article>
      </div>

      <div className="relative">
        <Search
          size={18}
          className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"
        />
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Buscar por nome, CPF/CNPJ, telefone..."
          className="w-full rounded-3xl border border-slate-200 bg-white py-3 pl-11 pr-4 text-sm shadow-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        />
      </div>

      <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-50 text-slate-500">
            <tr>
              {[
                "Documento",
                "Nome",
                "Cidade / UF",
                "Telefone",
                "Limite crédito",
              ].map((h) => (
                <th
                  key={h}
                  className="px-5 py-4 font-semibold uppercase tracking-wider"
                >
                  {h}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td
                  colSpan={5}
                  className="px-5 py-8 text-center text-slate-400"
                >
                  Carregando...
                </td>
              </tr>
            ) : clients.length === 0 ? (
              <tr>
                <td
                  colSpan={5}
                  className="px-5 py-8 text-center text-slate-400"
                >
                  Nenhum cliente encontrado
                </td>
              </tr>
            ) : (
              clients.map((c) => (
                <tr key={c.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-5 py-4 font-mono text-slate-500">
                    {c.document}
                  </td>
                  <td className="px-5 py-4 font-medium text-slate-900">
                    {c.name}
                  </td>
                  <td className="px-5 py-4 text-slate-500">
                    {c.city} / {c.state}
                  </td>
                  <td className="px-5 py-4 text-slate-500">{c.phone}</td>
                  <td className="px-5 py-4 text-slate-900">
                    R$ {c.creditLimit.toFixed(2)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
