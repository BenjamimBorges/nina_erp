export interface Product {
  id: string; sku: string; name: string; description: string;
  ncm: string; unit: string; priceSale: number; priceMinimum: number;
  costAverage: number; stockQty: number; stockMin: number;
  brand: string; department: string; barcode: string; isActive: boolean;
}

export interface Client {
  id: string; document: string; isLegalEntity: boolean; name: string;
  fantasyName: string; email: string; phone: string; address: string;
  city: string; state: string; zipCode: string; creditLimit: number; isActive: boolean;
}

export interface Supplier {
  id: string; document: string; isLegalEntity: boolean; name: string;
  fantasyName: string; stateRegistration: string; email: string;
  phone: string; city: string; state: string; contactName: string; isActive: boolean;
}

export interface StockMovement {
  id: string; productId: string; productName: string;
  type: string; qty: number; costUnit: number; notes?: string; movedAt: string;
}

export interface DashboardSummary {
  productsCount: number;
  clientsCount: number;
  suppliersCount: number;
  lowStockCount: number;
  totalInventoryValue: number;
}

export interface ApiResponse<T> {
  success: boolean; message: string; data?: T; errors?: string[];
}

export interface AuthResponse {
  token: string; username: string; fullName: string; role: string; companyId: string;
}
