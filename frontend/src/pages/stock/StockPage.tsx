import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { stockService } from "../../services/stock";
import { productsService } from "../../services/products";
import { dashboardService } from "../../services/dashboard";
import {
  AlertTriangle,
  ChevronDown,
  ArrowUpRight,
  Database,
} from "lucide-react";

export default function StockPage() {
  const qc = useQueryClient();
  const [form, setForm] = useState({
    productId: "",
    type: "Entry",
    qty: 1,
    costUnit: 0,
    notes: "",
  });

  const { data: summaryData } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: dashboardService.getSummary,
    staleTime: 1000 * 60 * 2,
  });

  const { data: lowStockData, isLoading: lowStockLoading } = useQuery({
    queryKey: ["products", "low-stock"],
    queryFn: productsService.getLowStock,
  });

  const adjustMutation = useMutation({
    mutationFn: stockService.adjust,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["products"] });
      qc.invalidateQueries({ queryKey: ["products", "low-stock"] });
      qc.invalidateQueries({ queryKey: ["dashboard-summary"] });
      alert("Ajuste registrado!");
    },
  });

  const lowStock = lowStockData?.data ?? [];
  const summary = summaryData?.data;

  return (
    <div className="p-6 space-y-6">
      <header className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <h2 className="text-2xl font-semibold text-slate-900">Estoque</h2>
          <p className="text-sm text-slate-500">
            Acompanhe os níveis de estoque e registre movimentos rapidamente.
          </p>
        </div>
        <div className="rounded-3xl border border-slate-200 bg-white px-4 py-3 shadow-sm inline-flex items-center gap-2 text-sm text-slate-600">
          <Database size={18} /> Valor total em estoque:{" "}
          <strong className="text-slate-900">
            R$ {summary ? summary.totalInventoryValue.toFixed(2) : "—"}
          </strong>
        </div>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Produtos em estoque</span>
            <ArrowUpRight size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {summary ? summary.productsCount : "—"}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Estoque baixo</span>
            <AlertTriangle size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {summary ? summary.lowStockCount : "—"}
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="text-slate-500">Últimos ajustes</div>
          <div className="mt-5 text-sm text-slate-600">
            Registre entradas, saídas e perdas diretamente do formulário acima.
          </div>
        </article>

        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="text-slate-500">Atenção</div>
          <div className="mt-5 text-sm text-slate-600">
            Produtos com estoque abaixo do mínimo estão destacados abaixo.
          </div>
        </article>
      </div>

      <div className="grid gap-6 xl:grid-cols-[1.2fr_0.8fr]">
        <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-lg font-semibold text-slate-900">
                Movimento de estoque
              </h3>
              <p className="text-sm text-slate-500">
                Encontre o produto e registre a movimentação.
              </p>
            </div>
            <span className="rounded-2xl bg-indigo-100 px-3 py-1 text-xs font-semibold text-indigo-700">
              Manual
            </span>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                ID do produto
              </label>
              <input
                value={form.productId}
                onChange={(e) =>
                  setForm((f) => ({ ...f, productId: e.target.value }))
                }
                placeholder="UUID do produto"
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Tipo
              </label>
              <select
                value={form.type}
                onChange={(e) =>
                  setForm((f) => ({ ...f, type: e.target.value }))
                }
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              >
                <option value="Entry">Entrada</option>
                <option value="Exit">Saída</option>
                <option value="Adjustment">Ajuste</option>
                <option value="Loss">Perda</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Quantidade
              </label>
              <input
                type="number"
                min="0.001"
                step="0.001"
                value={form.qty}
                onChange={(e) =>
                  setForm((f) => ({ ...f, qty: Number(e.target.value) }))
                }
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Custo unitário
              </label>
              <input
                type="number"
                min="0"
                step="0.01"
                value={form.costUnit}
                onChange={(e) =>
                  setForm((f) => ({ ...f, costUnit: Number(e.target.value) }))
                }
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              />
            </div>

            <div className="md:col-span-2">
              <label className="block text-sm font-medium text-slate-700 mb-2">
                Observação
              </label>
              <input
                value={form.notes}
                onChange={(e) =>
                  setForm((f) => ({ ...f, notes: e.target.value }))
                }
                className="w-full rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm outline-none focus:border-indigo-500 focus:ring-2 focus:ring-indigo-100"
              />
            </div>
          </div>

          <button
            onClick={() => adjustMutation.mutate(form)}
            disabled={adjustMutation.isPending}
            className="mt-6 inline-flex items-center justify-center rounded-2xl bg-indigo-600 px-5 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-500/20 transition hover:bg-indigo-700 disabled:opacity-50"
          >
            {adjustMutation.isPending ? "Salvando..." : "Registrar movimento"}
          </button>
        </section>

        <section className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between mb-4">
            <div>
              <h3 className="text-lg font-semibold text-slate-900">
                Estoque baixo
              </h3>
              <p className="text-sm text-slate-500">
                Produtos que precisam de reposição.
              </p>
            </div>
            <ChevronDown size={20} className="text-slate-400" />
          </div>

          {lowStockLoading ? (
            <div className="py-12 text-center text-slate-400">
              Carregando...
            </div>
          ) : lowStock.length === 0 ? (
            <div className="py-12 text-center text-slate-400">
              Sem produtos com estoque baixo.
            </div>
          ) : (
            <div className="space-y-3">
              {lowStock.map((p) => (
                <div
                  key={p.id}
                  className="rounded-3xl border border-slate-100 bg-slate-50 p-4"
                >
                  <div className="flex items-center justify-between gap-4">
                    <div>
                      <p className="text-sm font-semibold text-slate-900">
                        {p.name}
                      </p>
                      <p className="text-xs text-slate-500">
                        {p.sku} • {p.department}
                      </p>
                    </div>
                    <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-semibold text-amber-700">
                      {p.stockQty}/{p.stockMin} {p.unit}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}
