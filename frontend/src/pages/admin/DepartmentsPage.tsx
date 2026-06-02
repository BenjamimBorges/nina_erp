import { useQuery } from "@tanstack/react-query";
import { Plus, Layers } from "lucide-react";
import { departmentsService } from "../../services/departments";
import { useModalStore } from "../../store/modal";
import CreateDepartmentModal from "../../components/modals/CreateDepartmentModal";

export default function DepartmentsPage() {
  const { activeModal, openModal, closeModal } = useModalStore();

  const { data, isLoading } = useQuery({
    queryKey: ["departments"],
    queryFn: departmentsService.getAll,
  });

  const departments = data?.data ?? [];

  return (
    <div className="p-6 space-y-6">
      <header className="flex flex-col gap-3 md:flex-row md:items-end md:justify-between">
        <div>
          <h2 className="text-2xl font-semibold text-slate-900">
            Departamentos
          </h2>
          <p className="text-sm text-slate-500">
            Gerencie os departamentos de produtos da sua empresa.
          </p>
        </div>
        <button
          onClick={() => openModal("department")}
          className="inline-flex items-center gap-2 bg-indigo-600 text-white px-4 py-2 rounded-2xl text-sm font-semibold hover:bg-indigo-700 transition-colors"
        >
          <Plus size={16} /> Novo departamento
        </button>
      </header>

      <div className="grid gap-4">
        <article className="rounded-3xl border border-slate-200 bg-white p-6 shadow-sm">
          <div className="flex items-center justify-between text-slate-500">
            <span>Total de departamentos</span>
            <Layers size={20} />
          </div>
          <div className="mt-5 text-3xl font-semibold text-slate-900">
            {departments.length}
          </div>
        </article>
      </div>

      <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-sm">
        <table className="min-w-full text-left text-sm">
          <thead className="bg-slate-50 text-slate-500">
            <tr>
              {["Departamento", "Descrição", "Produtos"].map((h) => (
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
                  colSpan={3}
                  className="px-5 py-8 text-center text-slate-400"
                >
                  Carregando...
                </td>
              </tr>
            ) : departments.length === 0 ? (
              <tr>
                <td
                  colSpan={3}
                  className="px-5 py-8 text-center text-slate-400"
                >
                  Nenhum departamento encontrado
                </td>
              </tr>
            ) : (
              departments.map((d: any) => (
                <tr key={d.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-5 py-4 font-medium text-slate-900">
                    {d.name}
                  </td>
                  <td className="px-5 py-4 text-slate-500">
                    {d.description || "—"}
                  </td>
                  <td className="px-5 py-4">
                    <span className="inline-flex rounded-full bg-indigo-50 px-3 py-1 text-xs font-semibold text-indigo-700">
                      {d.productCount}
                    </span>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <CreateDepartmentModal
        isOpen={activeModal === "department"}
        onClose={closeModal}
      />
    </div>
  );
}
