export interface Supplier {
  id: string;
  name: string;
  document: string;
  email: string;
  phone: string;
  address: string;
  contactPerson: string;
  isActive: boolean;
  companyId: string;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateSupplierRequest {
  name: string;
  document: string;
  email: string;
  phone: string;
  address: string;
  contactPerson: string;
  companyId: string;
}

export interface UpdateSupplierRequest extends CreateSupplierRequest {
  id: string;
  isActive: boolean;
}
