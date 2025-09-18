<template>
  <div class="toast-container" role="status" aria-live="polite">
    <TransitionGroup name="toast" tag="div">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="toast"
        :class="`toast--${toast.type}`"
      >
        <span>{{ toast.message }}</span>
        <button type="button" class="toast__close" @click="dismiss(toast.id)">
          <span class="sr-only">Dismiss</span>
          ×
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup lang="ts">
import { storeToRefs } from 'pinia';
import { useToastStore } from '@/stores/toastStore';

const toastStore = useToastStore();
const { toasts } = storeToRefs(toastStore);
const { dismiss } = toastStore;
</script>

<style scoped>
.toast-container {
  position: fixed;
  bottom: 1.5rem;
  right: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  z-index: 60;
}

.toast {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  border-radius: 0.75rem;
  background: #1f1f1f;
  color: #f4f4f4;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.4);
  min-width: 220px;
}

.toast--success {
  border-left: 4px solid #4ade80;
}

.toast--error {
  border-left: 4px solid #f87171;
}

.toast--info {
  border-left: 4px solid #38bdf8;
}

.toast__close {
  border: none;
  background: transparent;
  color: inherit;
  font-size: 1.25rem;
  cursor: pointer;
  line-height: 1;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.25s ease;
}

.toast-enter-from {
  opacity: 0;
  transform: translateY(12px);
}

.toast-leave-to {
  opacity: 0;
  transform: translateY(12px);
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}
</style>
