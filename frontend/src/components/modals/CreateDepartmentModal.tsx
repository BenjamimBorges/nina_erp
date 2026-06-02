import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Modal from "../shared/Modal";
import FormField from "../shared/FormField";
import { departmentsService } from "../../services/departments";

interface CreateDepartmentModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function CreateDepartmentModal({
  isOpen,
  onClose,
}: CreateDepartmentModalProps) {
  const [formData, setFormData] = useState({
    name: "",
    description: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const qc = useQueryClient();

  const createMutation = useMutation({
    mutationFn: departmentsService.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["departments"] });
      setFormData({ name: "", description: "" });
      onClose();
    },
    onError: (error: any) => {
      setErrors({ submit: error.message });
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setErrors({});

    if (!formData.name) setErrors((e) => ({ ...e, name: "Nome é obrigatório" }));

    if (!formData.name) return;

    createMutation.mutate(formData);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Novo Departamento">
      <form onSubmit={handleSubmit} className="space-y-4">
        <FormField
          label="Nome"
          placeholder="Nome do departamento"
          value={formData.name}
          onChange={(e) =>
            setFormData({ ...formData, name: e.target.value })
          }
          error={errors.name}
        />
        <FormField
          label="Descrição (opcional)"
          placeholder="Descrição"
          value={formData.description}
          onChange={(e) =>
            setFormData({ ...formData, description: e.target.value })
          }
        />
        {errors.submit && (
          <p className="text-xs text-rose-600">{errors.submit}</p>
        )}
        <div className="flex gap-2 pt-4">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-lg border border-slate-200 py-2 text-sm font-medium text-slate-700 hover:bg-slate-50"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={createMutation.isPending}
            className="flex-1 rounded-lg bg-indigo-600 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:opacity-50"
          >
            {createMutation.isPending ? "Criando..." : "Criar"}
          </button>
        </div>
      </form>
    </Modal>
  );
}
