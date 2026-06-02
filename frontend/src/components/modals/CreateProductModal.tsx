import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import Modal from "../shared/Modal";
import FormField from "../shared/FormField";
import FormSelect from "../shared/FormSelect";
import { productsService } from "../../services/products";
import { departmentsService } from "../../services/departments";

interface CreateProductModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export default function CreateProductModal({
  isOpen,
  onClose,
}: CreateProductModalProps) {
  const [formData, setFormData] = useState({
    name: "",
    sku: "",
    department: "",
    priceSale: "",
    priceMinimum: "",
    brand: "",
    ncm: "",
    barcode: "",
    unit: "UN",
    stockMin: "0",
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const qc = useQueryClient();

  const { data: departmentsData } = useQuery({
    queryKey: ["departments"],
    queryFn: departmentsService.getAll,
  });

  const createMutation = useMutation({
    mutationFn: (data: any) =>
      productsService.create({
        ...data,
        priceSale: parseFloat(data.priceSale),
        priceMinimum: parseFloat(data.priceMinimum),
        stockMin: parseInt(data.stockMin),
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["products"] });
      setFormData({
        name: "",
        sku: "",
        department: "",
        priceSale: "",
        priceMinimum: "",
        brand: "",
        ncm: "",
        barcode: "",
        unit: "UN",
        stockMin: "0",
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
    if (!formData.sku) setErrors((e) => ({ ...e, sku: "SKU é obrigatório" }));
    if (!formData.priceSale)
      setErrors((e) => ({ ...e, priceSale: "Preço é obrigatório" }));
    if (!formData.department)
      setErrors((e) => ({ ...e, department: "Departamento é obrigatório" }));

    if (!formData.name || !formData.sku || !formData.priceSale || !formData.department)
      return;

    createMutation.mutate(formData);
  };

  const departmentOptions =
    departmentsData?.data?.map((d: any) => ({
      value: d.name,
      label: d.name,
    })) ?? [];

  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Novo Produto">
      <form onSubmit={handleSubmit} className="space-y-4 max-h-96 overflow-y-auto">
        <FormField
          label="Nome"
          placeholder="Nome do produto"
          value={formData.name}
          onChange={(e) =>
            setFormData({ ...formData, name: e.target.value })
          }
          error={errors.name}
        />
        <FormField
          label="SKU"
          placeholder="Ex: 1, 2, 3..."
          value={formData.sku}
          onChange={(e) =>
            setFormData({ ...formData, sku: e.target.value })
          }
          error={errors.sku}
        />
        <FormSelect
          label="Departamento"
          options={departmentOptions}
          value={formData.department}
          onChange={(e) =>
            setFormData({ ...formData, department: e.target.value })
          }
          error={errors.department}
        />
        <FormField
          label="Marca"
          placeholder="Marca do produto"
          value={formData.brand}
          onChange={(e) =>
            setFormData({ ...formData, brand: e.target.value })
          }
        />
        <FormField
          label="Preço de Venda"
          type="number"
          step="0.01"
          placeholder="0.00"
          value={formData.priceSale}
          onChange={(e) =>
            setFormData({ ...formData, priceSale: e.target.value })
          }
          error={errors.priceSale}
        />
        <FormField
          label="Preço Mínimo"
          type="number"
          step="0.01"
          placeholder="0.00"
          value={formData.priceMinimum}
          onChange={(e) =>
            setFormData({ ...formData, priceMinimum: e.target.value })
          }
        />
        <FormField
          label="NCM"
          placeholder="00000000"
          value={formData.ncm}
          onChange={(e) =>
            setFormData({ ...formData, ncm: e.target.value })
          }
        />
        <FormField
          label="Código de Barras"
          placeholder="Código de barras"
          value={formData.barcode}
          onChange={(e) =>
            setFormData({ ...formData, barcode: e.target.value })
          }
        />
        <FormField
          label="Unidade"
          placeholder="UN"
          value={formData.unit}
          onChange={(e) =>
            setFormData({ ...formData, unit: e.target.value })
          }
        />
        <FormField
          label="Estoque Mínimo"
          type="number"
          placeholder="0"
          value={formData.stockMin}
          onChange={(e) =>
            setFormData({ ...formData, stockMin: e.target.value })
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
