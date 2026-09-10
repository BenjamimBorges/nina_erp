import api from './api'
import type { ApiResponse, Sale } from '../types'
export type PaymentMethod='Cash'|'CreditCard'|'DebitCard'|'Pix'|'BankSlip'|'Check'|'Other'
export interface CreateSaleItemInput{productId:string;qty:number;discount:number}
export interface PaymentInput{method:PaymentMethod;amount:number;idempotencyKey:string;provider?:string;dueDate?:string}
export const salesService={
 getRecent:()=>api.get<ApiResponse<Sale[]>>('/sales').then(r=>r.data),
 create:(input:{cashSessionId:string;warehouseId?:string;idempotencyKey:string;clientId?:string;items:CreateSaleItemInput[]})=>api.post<ApiResponse<string>>('/sales',input).then(r=>r.data),
 finalize:(saleId:string,cashSessionId:string,payments:PaymentInput[])=>api.post<ApiResponse<{transactions:any[];status:string}>>(`/sales/${saleId}/finalize`,{cashSessionId,payments}).then(r=>r.data),
 suspend:(saleId:string)=>api.post(`/sales/${saleId}/suspend`), resume:(saleId:string)=>api.post(`/sales/${saleId}/resume`),
 cancel:(saleId:string,reason:string)=>api.post(`/sales/${saleId}/cancel`,{reason,idempotencyKey:crypto.randomUUID()}),
}
