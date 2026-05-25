import { Link } from "react-router-dom";
import { useQuery } from "@tanstack/react-query";
import { dashboardService } from "../../services/dashboard";

export default function DashboardPage() {
  const { data, isLoading } = useQuery({
    queryKey: ["dashboard-summary"],
    queryFn: dashboardService.getSummary,
    staleTime: 1000 * 60 * 2,
  });

  const summary = data?.data;

  return (
    <div className="space-y-8 p-6 lg:p-10">
      <div>
        <h1 className="text-4xl font-bold text-slate-900">Dashboard</h1>
        <p className="text-slate-500 mt-2">
          Visão geral operacional do Nina ERP.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
        {isLoading ? (
          <div className="col-span-full rounded-3xl bg-white p-6 shadow-sm border border-slate-100 text-center text-slate-500">
            Carregando os dados do painel...
          </div>
        ) : summary ? (
          [
            {
              title: "Produtos",
              value: `${summary.productsCount}`,
              to: "/products",
            },
            {
              title: "Clientes",
              value: `${summary.clientsCount}`,
              to: "/clients",
            },
            {
              title: "Fornecedores",
              value: `${summary.suppliersCount}`,
              to: "/suppliers",
            },
            {
              title: "Estoque baixo",
              value: `${summary.lowStockCount}`,
              to: "/stock",
            },
          ].map((card) => (
            <Link
              key={card.title}
              to={card.to}
              className="group block rounded-3xl border border-slate-200 bg-white p-6 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md"
            >
              <p className="text-slate-500 text-sm">{card.title}</p>
              <h2 className="text-3xl font-bold text-slate-900 mt-3">
                {card.value}
              </h2>
              <p className="mt-5 text-sm text-indigo-600 group-hover:text-indigo-700">
                Acessar {card.title.toLowerCase()}
              </p>
            </Link>
          ))
        ) : (
          <div className="col-span-full rounded-3xl bg-white p-6 shadow-sm border border-slate-100 text-center text-red-600">
            Não foi possível carregar os dados do dashboard.
          </div>
        )}
      </div>

      {summary && (
        <div className="grid gap-6 lg:grid-cols-2">
          <div className="bg-white rounded-3xl p-8 border border-slate-100 shadow-sm">
            <h2 className="text-2xl font-bold text-slate-900 mb-4">
              Resumo de inventário
            </h2>
            <p className="text-slate-600 leading-relaxed">
              O valor total de estoque registrado é de{" "}
              <strong>R$ {summary.totalInventoryValue.toFixed(2)}</strong>, com
              todos os dados sendo carregados diretamente do banco de dados.
            </p>
          </div>

          <div className="bg-slate-900 rounded-3xl p-8 text-white">
            <h2 className="text-2xl font-bold mb-4">Dados reais do banco</h2>
            <p className="text-slate-300 leading-relaxed">
              Os números exibidos aqui representam dados reais persistidos no
              banco. Produtos, clientes, fornecedores e estoque já são
              consultados pela API.
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
