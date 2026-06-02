import { create } from "zustand";

export type ModalType = "client" | "product" | "supplier" | "department";

interface ModalState {
  activeModal: ModalType | null;
  openModal: (type: ModalType) => void;
  closeModal: () => void;
}

export const useModalStore = create<ModalState>((set) => ({
  activeModal: null,
  openModal: (type) => set({ activeModal: type }),
  closeModal: () => set({ activeModal: null }),
}));
