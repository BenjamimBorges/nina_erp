import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Modal from "../shared/Modal";
import FormField from "../shared/FormField";
import { suppliersService } from "../../services/suppliers";

interface CreateSupplierModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function CreateSupplierModal({
  isOpen,
  onClose,
}: CreateSupplierModalProps) {
  const [formData, setFormData] = useState({
    name: "",
    document: "",
    fantasyName: "",
    email: "",
    phone: "",
    contactName: "",
    city: "",
    state: "",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const qc = useQueryClient();

  const createMutation = useMutation({
    mutationFn: (data: any) =>
      suppliersService.create({
        name: data.name,
        document: data.document,
        fantasyName: data.fantasyName,
        email: data.email,
        phone: data.phone,
        contactName: data.contactName,
        city: data.city,
        state: data.state,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["suppliers"] });
      setFormData({
        name: "",
        document: "",
        fantasyName: "",
        email: "",
        phone: "",
        contactName: "",
        city: "",
        state: "",
      });
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
      setErrors((e) => ({ ...e, document: "CNPJ é obrigatório" }));

    if (!formData.name || !formData.document) return;

    createMutation.mutate(formData);
  };

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Novo Fornecedor">
      <form onSubmit={handleSubmit} className="space-y-4 max-h-96 overflow-y-auto">
        <FormField
          label="Razão Social"
          placeholder="Razão social"
          value={formData.name}
          onChange={(e) =>
            setFormData({ ...formData, name: e.target.value })
          }
          error={errors.name}
        />
        <FormField
          label="CNPJ"
          placeholder="00.000.000/0000-00"
          value={formData.document}
          onChange={(e) =>
            setFormData({ ...formData, document: e.target.value })
          }
          error={errors.document}
        />
        <FormField
          label="Nome Fantasia"
          placeholder="Nome fantasia"
          value={formData.fantasyName}
          onChange={(e) =>
            setFormData({ ...formData, fantasyName: e.target.value })
          }
        />
        <FormField
          label="Contato"
          placeholder="Nome do contato"
          value={formData.contactName}
          onChange={(e) =>
            setFormData({ ...formData, contactName: e.target.value })
          }
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
        <FormField
          label="Cidade"
          placeholder="Cidade"
          value={formData.city}
          onChange={(e) =>
            setFormData({ ...formData, city: e.target.value })
          }
        />
        <FormField
          label="Estado (UF)"
          placeholder="SP"
          value={formData.state}
          onChange={(e) =>
            setFormData({ ...formData, state: e.target.value })
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

