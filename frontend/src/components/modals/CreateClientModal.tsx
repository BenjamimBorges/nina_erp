import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Modal from "../shared/Modal";
import FormField from "../shared/FormField";
import { clientsService } from "../../services/clients";

interface CreateClientModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function CreateClientModal({
  isOpen,
  onClose,
}: CreateClientModalProps) {
  const [formData, setFormData] = useState({
    name: "",
    document: "",
    email: "",
    phone: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const qc = useQueryClient();

  const createMutation = useMutation({
    mutationFn: clientsService.create,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["clients"] });
      setFormData({ name: "", document: "", email: "", phone: "" });
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
    if (!formData.document)
      setErrors((e) => ({ ...e, document: "CPF/CNPJ é obrigatório" }));

    if (!formData.name || !formData.document) return;

    createMutation.mutate(formData);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Novo Cliente">
      <form onSubmit={handleSubmit} className="space-y-4">
        <FormField
          label="Nome"
          placeholder="Nome do cliente"
          value={formData.name}
          onChange={(e) =>
            setFormData({ ...formData, name: e.target.value })
          }
          error={errors.name}
        />
        <FormField
          label="CPF/CNPJ"
          placeholder="000.000.000-00"
          value={formData.document}
          onChange={(e) =>
            setFormData({ ...formData, document: e.target.value })
          }
          error={errors.document}
        />
        <FormField
          label="Email"
          type="email"
          placeholder="email@example.com"
          value={formData.email}
          onChange={(e) =>
            setFormData({ ...formData, email: e.target.value })
          }
        />
        <FormField
          label="Telefone"
          placeholder="(11) 99999-9999"
          value={formData.phone}
          onChange={(e) =>
            setFormData({ ...formData, phone: e.target.value })
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
