<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import type { MatchMode, SubmitThrow } from '@/types/api';
import { dartScore, formatSubmitThrow } from '@/utils/dartUtils';

const props = defineProps<{
  mode: MatchMode | null;
  disabled?: boolean;
  isSubmitting?: boolean;
  undoDisabled?: boolean;
}>();

const emit = defineEmits<{
  (e: 'submit', payload: SubmitThrow[]): void;
  (e: 'undo'): void;
}>();

const throws = ref<SubmitThrow[]>([]);
const selectedMultiplier = ref<number>(1);

const multipliers = reactive([
  { value: 1, label: 'Single', short: 'S' },
  { value: 2, label: 'Double', short: 'D' },
  { value: 3, label: 'Triple', short: 'T' }
]);

const quickTotalsOrder = [26, 45, 60, 85, 100, 140, 180];
const quickTotalsMap: Record<number, SubmitThrow[]> = {
  26: [
    { multiplier: 1, segment: 20 },
    { multiplier: 1, segment: 5 },
    { multiplier: 1, segment: 1 }
  ],
  45: [{ multiplier: 3, segment: 15 }],
  60: [{ multiplier: 3, segment: 20 }],
  85: [
    { multiplier: 3, segment: 17 },
    { multiplier: 2, segment: 17 }
  ],
  100: [
    { multiplier: 3, segment: 20 },
    { multiplier: 2, segment: 20 }
  ],
  140: [
    { multiplier: 3, segment: 20 },
    { multiplier: 3, segment: 20 },
    { multiplier: 2, segment: 10 }
  ],
  180: [
    { multiplier: 3, segment: 20 },
    { multiplier: 3, segment: 20 },
    { multiplier: 3, segment: 20 }
  ]
};

const maxThrows = 3;

const isX01 = computed(() => props.mode === 'X01');
const isCricket = computed(() => props.mode === 'Cricket');

const x01Segments = Array.from({ length: 20 }, (_, index) => 20 - index).concat([25, 50]);
const cricketSegments = [20, 19, 18, 17, 16, 15, 25, 50];

const segments = computed(() => (isCricket.value ? cricketSegments : x01Segments));

watch(
  () => props.mode,
  () => {
    clearThrows();
    selectedMultiplier.value = 1;
  }
);

const totalScore = computed(() => throws.value.reduce((acc, dart) => acc + dartScore(dart), 0));
const hasMaxThrows = computed(() => throws.value.length >= maxThrows);

function setMultiplier(value: number) {
  selectedMultiplier.value = value;
}

function recordSegment(segment: number) {
  if (hasMaxThrows.value || props.disabled || props.isSubmitting) {
    return;
  }

  let multiplier = selectedMultiplier.value;

  if (segment === 50) {
    multiplier = 1;
    if (selectedMultiplier.value !== 1) {
      selectedMultiplier.value = 1;
    }
  } else if (segment === 25 && multiplier > 2) {
    multiplier = 2;
    selectedMultiplier.value = 2;
  }

  throws.value = [
    ...throws.value,
    {
      multiplier,
      segment
    }
  ];
}

function removeThrow(index: number) {
  if (props.disabled) {
    return;
  }
  throws.value = throws.value.filter((_, idx) => idx !== index);
}

function clearThrows() {
  throws.value = [];
}

function applyQuickTotal(total: number) {
  if (!isX01.value || props.disabled || props.isSubmitting) {
    return;
  }
  const preset = quickTotalsMap[total];
  if (!preset) {
    return;
  }
  throws.value = preset.slice(0, maxThrows).map(dart => ({ ...dart }));
}

function submit() {
  if (!throws.value.length || props.disabled || props.isSubmitting) {
    return;
  }
  emit('submit', throws.value.map(dart => ({ ...dart })));
  clearThrows();
}

const canSubmit = computed(() => !props.disabled && !props.isSubmitting && throws.value.length > 0);
const canUndo = computed(() => !props.disabled && !props.isSubmitting && !props.undoDisabled);
const canClear = computed(() => !props.disabled && throws.value.length > 0);
</script>

<template>
  <section class="throw-entry">
    <header class="throw-entry__header">
      <h2>Throw Entry</h2>
      <p class="throw-entry__subtitle">
        {{
          isX01
            ? 'Use quick totals or select each dart to record the turn.'
            : 'Select your marks for the current turn.'
        }}
      </p>
    </header>

    <div v-if="isX01" class="quick-entries" aria-label="Quick score buttons">
      <button
        v-for="total in quickTotalsOrder"
        :key="total"
        type="button"
        class="btn btn--secondary"
        :disabled="props.disabled || props.isSubmitting"
        @click="applyQuickTotal(total)"
      >
        {{ total }}
      </button>
    </div>

    <div class="picker">
      <div class="picker__multipliers" role="group" aria-label="Multiplier selection">
        <button
          v-for="option in multipliers"
          :key="option.value"
          type="button"
          class="picker__btn"
          :class="{ 'picker__btn--active': selectedMultiplier === option.value }"
          :disabled="props.disabled || props.isSubmitting"
          @click="setMultiplier(option.value)"
        >
          {{ option.label }}
        </button>
      </div>

      <div class="picker__segments" role="grid" aria-label="Segment selection">
        <button
          v-for="segment in segments"
          :key="segment"
          type="button"
          role="gridcell"
          class="picker__segment"
          :disabled="props.disabled || props.isSubmitting"
          @click="recordSegment(segment)"
        >
          <span v-if="segment === 25">Bull</span>
          <span v-else-if="segment === 50">Inner Bull</span>
          <span v-else>{{ segment }}</span>
        </button>
      </div>
    </div>

    <div class="throws" aria-live="polite">
      <div v-if="throws.length === 0" class="throws__empty">
        No darts selected yet.
      </div>
      <div v-else class="throws__list">
        <div v-for="(dart, index) in throws" :key="index" class="throws__item">
          <span class="throws__label">{{ formatSubmitThrow(dart) }}</span>
          <span class="throws__score">{{ dartScore(dart) }}</span>
          <button type="button" class="throws__remove" :disabled="props.disabled" @click="removeThrow(index)">
            <span class="sr-only">Remove dart</span>
            ×
          </button>
        </div>
      </div>
      <footer class="throws__footer" v-if="throws.length">
        <span>Total</span>
        <strong>{{ totalScore }}</strong>
        <button type="button" class="btn btn--secondary btn--sm" :disabled="!canClear" @click="clearThrows">
          Clear
        </button>
      </footer>
    </div>

    <div class="throw-entry__actions">
      <button type="button" class="btn btn--primary" :disabled="!canSubmit" @click="submit">
        {{ props.isSubmitting ? 'Submitting…' : 'Submit Turn' }}
      </button>
      <button type="button" class="btn btn--secondary" :disabled="!canUndo" @click="emit('undo')">
        Undo Last Turn
      </button>
    </div>
  </section>
</template>

<style scoped>
.throw-entry {
  display: grid;
  gap: 1.5rem;
}

.throw-entry__header h2 {
  margin: 0;
  font-size: 1.4rem;
}

.throw-entry__subtitle {
  margin: 0;
  color: rgba(226, 232, 240, 0.7);
}

.quick-entries {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.picker {
  display: grid;
  gap: 1rem;
  background: rgba(15, 23, 42, 0.55);
  border-radius: 1.25rem;
  padding: 1.25rem;
  border: 1px solid rgba(148, 163, 184, 0.16);
}

.picker__multipliers {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.picker__btn {
  background: rgba(15, 23, 42, 0.65);
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 0.75rem;
  padding: 0.5rem 0.85rem;
  color: rgba(226, 232, 240, 0.85);
  transition: border-color 0.2s ease, background 0.2s ease;
}

.picker__btn--active {
  border-color: var(--accent);
  background: var(--accent-muted);
  color: var(--accent);
}

.picker__segments {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(64px, 1fr));
  gap: 0.5rem;
}

.picker__segment {
  background: rgba(31, 41, 55, 0.75);
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 0.85rem;
  padding: 0.75rem 0.5rem;
  color: #e2e8f0;
  text-transform: uppercase;
  font-weight: 600;
  transition: border-color 0.2s ease, transform 0.2s ease;
}

.throws {
  background: rgba(15, 23, 42, 0.55);
  border-radius: 1.25rem;
  border: 1px solid rgba(148, 163, 184, 0.16);
  padding: 1.25rem;
  display: grid;
  gap: 0.75rem;
}

.throws__empty {
  color: rgba(226, 232, 240, 0.6);
}

.throws__list {
  display: grid;
  gap: 0.75rem;
}

.throws__item {
  display: grid;
  grid-template-columns: minmax(0, 1fr) 80px auto;
  align-items: center;
  gap: 0.75rem;
  background: rgba(31, 41, 55, 0.85);
  border-radius: 0.85rem;
  padding: 0.6rem 0.75rem;
}

.throws__label {
  font-weight: 600;
}

.throws__score {
  font-variant-numeric: tabular-nums;
  text-align: right;
}

.throws__remove {
  border: none;
  background: transparent;
  color: rgba(226, 232, 240, 0.7);
  font-size: 1.25rem;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.throws__remove:hover,
.throws__remove:focus-visible {
  color: var(--accent);
}

.throws__footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px solid rgba(148, 163, 184, 0.16);
  padding-top: 0.75rem;
}

.btn--sm {
  padding: 0.45rem 0.75rem;
  font-size: 0.85rem;
}

.throw-entry__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
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

@media (max-width: 640px) {
  .picker__segments {
    grid-template-columns: repeat(auto-fit, minmax(54px, 1fr));
  }

  .throws__item {
    grid-template-columns: minmax(0, 1fr) 60px auto;
  }
}
</style>
