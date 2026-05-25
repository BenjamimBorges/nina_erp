import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { productsService } from "../../services/products";
import { dashboardService } from "../../services/dashboard";
import {
  Search,
  Plus,
  AlertTriangle,
  DollarSign,
  Box,
  Layers,
} from "lucide-react";

export default function ProductsPage() {
  const [search, setSearch] = useState("");
  const qc = useQueryClient();

  const { data: summaryData } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: dashboardService.getSummary,
    staleTime: 1000 * 60 * 2,
  });

  const { data, isLoading } = useQuery({
    queryKey: ["products", search],
    queryFn: () => productsService.getAll(search || undefined),
  });

  const deleteMutation = useMutation({
    mutationFn: productsService.delete,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["products"] }),
  });

  const products = data?.data ?? [];
  const summary = summaryData?.data;

  return (
    <div className="p-6 space-y-6">
      <header className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <h2 className="text-2xl font-semibold text-slate-900">Produtos</h2>
          <p className="text-sm text-slate-500">
            Gerencie o catálogo de produtos da sua empresa.
          </p>
        </div>
        <button className="inline-flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-2xl text-sm font-semibold hover:bg-indigo-700 transition-colors">
          <Plus size={16} /> Novo produto
        </button>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Total de produtos</span>
            <Box size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {summary ? summary.productsCount : "—"}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Estoque abaixo do mínimo</span>
            <AlertTriangle size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {summary ? summary.lowStockCount : "—"}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Valor de estoque</span>
            <DollarSign size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            R$ {summary ? summary.totalInventoryValue.toFixed(2) : "—"}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Visão geral</span>
            <Layers size={20} />
          </div>
          <div className="mt-5 text-sm text-slate-600">
            Busque por nome, SKU ou barra para localizar produtos rapidamente.
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
          placeholder="Buscar por nome, SKU, departamento..."
          className="w-full rounded-3xl border border-slate-200 bg-white py-3 pl-11 pr-4 text-sm shadow-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
        />
      </div>

      <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-50 text-slate-500">
            <tr>
              {[
                "SKU",
                "Produto",
                "Departamento",
                "Preço",
                "Estoque",
                "Mín.",
                "Ações",
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
                  colSpan={7}
                  className="px-5 py-8 text-center text-slate-400"
                >
                  Carregando...
                </td>
              </tr>
            ) : products.length === 0 ? (
              <tr>
                <td
                  colSpan={7}
                  className="px-5 py-8 text-center text-slate-400"
                >
                  Nenhum produto encontrado
                </td>
              </tr>
            ) : (
              products.map((p) => (
                <tr key={p.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-5 py-4 font-mono text-slate-500">
                    {p.sku}
                  </td>
                  <td className="px-5 py-4 font-medium text-slate-900">
                    {p.name}
                  </td>
                  <td className="px-5 py-4 text-slate-500">{p.department}</td>
                  <td className="px-5 py-4 text-slate-900">
                    R$ {p.priceSale.toFixed(2)}
                  </td>
                  <td className="px-5 py-4">
                    <span
                      className={`inline-flex rounded-full px-3 py-1 text-xs font-semibold ${p.stockQty <= p.stockMin ? "bg-red-50 text-red-700" : "bg-emerald-50 text-emerald-700"}`}
                    >
                      {p.stockQty} {p.unit}
                    </span>
                  </td>
                  <td className="px-5 py-4 text-slate-500">{p.stockMin}</td>
                  <td className="px-5 py-4">
                    <button
                      onClick={() => deleteMutation.mutate(p.id)}
                      className="text-xs font-semibold uppercase tracking-wide text-rose-600 hover:text-rose-800"
                    >
                      Excluir
                    </button>
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
