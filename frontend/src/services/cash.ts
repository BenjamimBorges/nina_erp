import api from './api';
import type { ApiResponse } from '../types';
export interface CashRegister { id:string; code:string; name:string; enabled:boolean }
export interface CashSession { id:string; cashRegisterId:string; registerName:string; status:string; openingAmount:number; expectedAmount:number; openedAt:string; countedAmount?:number; difference?:number }
const key=()=>crypto.randomUUID();
export const cashService={
  registers:()=>api.get<ApiResponse<CashRegister[]>>('/cash/registers').then(r=>r.data),
  openSession:()=>api.get<ApiResponse<CashSession|null>>('/cash/session/open').then(r=>r.data),
  open:(cashRegisterId:string,openingAmount:number,idempotencyKey=key())=>api.post<ApiResponse<CashSession>>('/cash/session/open',{cashRegisterId,openingAmount,idempotencyKey}).then(r=>r.data),
  close:(id:string,countedAmount:number,notes?:string,idempotencyKey=key())=>api.post<ApiResponse<CashSession>>(`/cash/session/${id}/close`,{countedAmount,notes,idempotencyKey}).then(r=>r.data),
  movement:(id:string,type:string,amount:number,description:string,idempotencyKey=key())=>api.post(`/cash/session/${id}/movement`,{type,amount,description,idempotencyKey})
};
