import { defineStore } from 'pinia';
import { ref } from 'vue';

type ToastType = 'success' | 'error' | 'info';

interface ToastMessage {
  id: number;
  type: ToastType;
  message: string;
}

let counter = 0;
const DEFAULT_DURATION = 3500;

export const useToastStore = defineStore('toasts', () => {
  const toasts = ref<ToastMessage[]>([]);

  function push(type: ToastType, message: string) {
    const id = ++counter;
    toasts.value.push({ id, type, message });
    window.setTimeout(() => dismiss(id), DEFAULT_DURATION);
  }

  function success(message: string) {
    push('success', message);
  }

  function error(message: string) {
    push('error', message);
  }

  function info(message: string) {
    push('info', message);
  }

  function dismiss(id: number) {
    toasts.value = toasts.value.filter(toast => toast.id !== id);
  }

  return {
    toasts,
    success,
    error,
    info,
    dismiss
  };
});
