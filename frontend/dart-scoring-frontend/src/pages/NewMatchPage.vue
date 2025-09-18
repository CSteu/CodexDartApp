<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import { storeToRefs } from 'pinia';
import { useMatchStore } from '@/stores/matchStore';
import { useToastStore } from '@/stores/toastStore';
import type { MatchMode } from '@/types/api';

const router = useRouter();
const matchStore = useMatchStore();
const toastStore = useToastStore();

const { players, activeMatch, isLoading } = storeToRefs(matchStore);

const mode = ref<MatchMode>('X01');
const targetScore = ref(501);
const doubleOut = ref(true);
const selectedPlayers = ref<number[]>([]);
const startingPlayerId = ref<number | null>(null);

const demoMatches = [
  { id: 1, label: 'Match A – X01 Demo' },
  { id: 2, label: 'Match B – Cricket Demo' }
];

const isBusy = computed(() => isLoading.value);
const isX01 = computed(() => mode.value === 'X01');

const canCreate = computed(
  () =>
    selectedPlayers.value.length === 2 &&
    startingPlayerId.value !== null &&
    selectedPlayers.value.includes(startingPlayerId.value)
);

const availablePlayers = computed(() => players.value);
const resumeMatchDescriptor = computed(() => {
  if (!activeMatch.value) {
    return '';
  }

  const matchPlayers = Array.isArray(activeMatch.value.players) ? activeMatch.value.players : [];
  if (matchPlayers.length === 0) {
    return `${activeMatch.value.mode}`;
  }

  const playerNames = matchPlayers.map(player => player.displayName).join(' vs ');
  return `${activeMatch.value.mode} • ${playerNames}`;
});

onMounted(() => {
  matchStore.fetchPlayers();
});

watch(mode, value => {
  if (value === 'Cricket') {
    targetScore.value = 0;
    doubleOut.value = false;
  } else {
    if (targetScore.value === 0) {
      targetScore.value = 501;
    }
    doubleOut.value = true;
  }
});

watch(selectedPlayers, value => {
  if (!value.includes(startingPlayerId.value ?? -1)) {
    startingPlayerId.value = value[0] ?? null;
  }
});

function togglePlayer(playerId: number) {
  const exists = selectedPlayers.value.includes(playerId);
  if (exists) {
    selectedPlayers.value = selectedPlayers.value.filter(id => id !== playerId);
    return;
  }

  if (selectedPlayers.value.length >= 2) {
    selectedPlayers.value = [...selectedPlayers.value.slice(1), playerId];
  } else {
    selectedPlayers.value = [...selectedPlayers.value, playerId];
  }
}

async function createMatch() {
  if (!canCreate.value) {
    toastStore.error('Select two players and a starting thrower first.');
    return;
  }

  try {
    await matchStore.createMatch({
      mode: mode.value,
      targetScore: targetScore.value,
      doubleOut: doubleOut.value,
      playerIds: selectedPlayers.value,
      startingPlayerId: startingPlayerId.value ?? undefined
    });

    const matchId = matchStore.activeMatch?.id;
    if (matchId) {
      router.push({ name: 'live-scoring', params: { id: matchId } });
    }
  } catch (error) {
    console.error(error);
    toastStore.error('Unable to create match. Please try again.');
  }
}

async function loadDemoMatch(matchId: number) {
  try {
    await matchStore.loadMatch(matchId);
    router.push({ name: 'live-scoring', params: { id: matchId } });
  } catch (error) {
    console.error(error);
    toastStore.error('Demo match could not be loaded.');
  }
}
</script>

<template>
  <section class="container new-match">
    <div class="page-header">
      <h1>Create a Match</h1>
      <p>Pick your game mode, choose two throwers, and start keeping score in seconds.</p>
    </div>

    <div class="grid new-match__layout">
      <article class="card">
        <h2 class="section-title">Match setup</h2>
        <form class="form" @submit.prevent="createMatch">
          <fieldset class="form__group">
            <legend>Game mode</legend>
            <label class="choice">
              <input v-model="mode" type="radio" name="mode" value="X01" />
              <span>
                <strong>X01</strong>
                <small>Double-out, configurable target</small>
              </span>
            </label>
            <label class="choice">
              <input v-model="mode" type="radio" name="mode" value="Cricket" />
              <span>
                <strong>Cricket</strong>
                <small>Marks 15-20 &amp; bull</small>
              </span>
            </label>
          </fieldset>

          <div class="form__row" :class="{ 'form__row--disabled': !isX01 }">
            <label for="target">Target score</label>
            <input
              id="target"
              v-model.number="targetScore"
              type="number"
              min="101"
              step="1"
              :disabled="!isX01"
            />
          </div>

          <label class="toggle">
            <input v-model="doubleOut" type="checkbox" :disabled="!isX01" />
            <span>Double-out required</span>
          </label>

          <fieldset class="form__group">
            <legend>Players</legend>
            <p class="form__hint">Select exactly two players. Click again to deselect.</p>
            <div class="player-grid">
              <button
                v-for="player in availablePlayers"
                :key="player.id"
                type="button"
                class="player-chip"
                :class="{ 'player-chip--selected': selectedPlayers.includes(player.id) }"
                :aria-pressed="selectedPlayers.includes(player.id)"
                @click="togglePlayer(player.id)"
              >
                {{ player.displayName }}
              </button>
            </div>
          </fieldset>

          <div class="form__row">
            <label for="starting">Starting player</label>
            <select id="starting" v-model.number="startingPlayerId">
              <option :value="null" disabled>Select starting player</option>
              <option
                v-for="playerId in selectedPlayers"
                :key="playerId"
                :value="playerId"
              >
                {{ availablePlayers.find(player => player.id === playerId)?.displayName }}
              </option>
            </select>
          </div>

          <div class="form__actions">
            <button type="submit" class="btn btn--primary" :disabled="!canCreate || isBusy">
              {{ isBusy ? 'Creating…' : 'Start Match' }}
            </button>
          </div>
        </form>
      </article>

      <aside class="card new-match__sidebar">
        <h2 class="section-title">Demo shortcuts</h2>
        <p class="sidebar__hint">Jump into the seeded matches or resume where you left off.</p>

        <div class="sidebar__section" role="list">
          <button
            v-for="demo in demoMatches"
            :key="demo.id"
            type="button"
            class="btn btn--secondary sidebar__action"
            :disabled="isBusy"
            @click="loadDemoMatch(demo.id)"
          >
            {{ demo.label }}
          </button>
        </div>

        <div v-if="activeMatch" class="resume">
          <h3>Resume current match</h3>
          <p v-if="resumeMatchDescriptor">
            {{ resumeMatchDescriptor }}
          </p>
          <div class="resume__actions">
            <button
              type="button"
              class="btn btn--primary"
              :disabled="isBusy"
              @click="router.push({ name: 'live-scoring', params: { id: activeMatch.id } })"
            >
              Go to live scoring
            </button>
            <button
              type="button"
              class="btn btn--secondary"
              :disabled="isBusy"
              @click="router.push({ name: 'summary', params: { id: activeMatch.id } })"
            >
              View summary
            </button>
          </div>
        </div>
      </aside>
    </div>
  </section>
</template>

<style scoped>
.new-match {
  display: grid;
  gap: 2rem;
}

.page-header h1 {
  font-size: clamp(2rem, 5vw, 2.75rem);
  margin: 0;
}

.page-header p {
  margin: 0.5rem 0 0;
  color: rgba(226, 232, 240, 0.7);
}

.new-match__layout {
  grid-template-columns: minmax(0, 1fr) minmax(260px, 0.8fr);
  align-items: start;
}

.form {
  display: grid;
  gap: 1.25rem;
}

.form__group {
  display: grid;
  gap: 0.75rem;
  border: none;
  padding: 0;
  margin: 0;
}

.form__group legend {
  font-weight: 600;
  text-transform: uppercase;
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  color: rgba(148, 163, 184, 0.85);
}

.choice {
  display: flex;
  gap: 0.75rem;
  align-items: flex-start;
  padding: 0.75rem 0.9rem;
  border-radius: 0.85rem;
  border: 1px solid rgba(148, 163, 184, 0.16);
  background: rgba(15, 23, 42, 0.55);
}

.choice input {
  margin-top: 0.35rem;
}

.choice strong {
  display: block;
}

.choice small {
  color: rgba(226, 232, 240, 0.6);
}

.form__row {
  display: grid;
  gap: 0.35rem;
}

.form__row label {
  font-weight: 600;
}

.form__row--disabled {
  opacity: 0.5;
}

.toggle {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.form__hint {
  margin: 0;
  color: rgba(226, 232, 240, 0.6);
  font-size: 0.85rem;
}

.player-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.player-chip {
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 999px;
  background: rgba(15, 23, 42, 0.65);
  padding: 0.45rem 1rem;
  color: rgba(226, 232, 240, 0.85);
  transition: border-color 0.2s ease, background 0.2s ease;
}

.player-chip--selected {
  border-color: var(--accent);
  background: var(--accent-muted);
  color: var(--accent);
}

.form__actions {
  display: flex;
  justify-content: flex-end;
}

.new-match__sidebar {
  display: grid;
  gap: 1.25rem;
  align-content: start;
}

.sidebar__hint {
  margin: 0;
  color: rgba(226, 232, 240, 0.6);
}

.sidebar__section {
  display: grid;
  gap: 0.5rem;
}

.sidebar__action {
  justify-content: flex-start;
}

.resume {
  border: 1px solid rgba(148, 163, 184, 0.12);
  border-radius: 1rem;
  padding: 1rem 1.25rem;
  background: rgba(15, 23, 42, 0.55);
  display: grid;
  gap: 0.5rem;
}

.resume h3 {
  margin: 0;
  font-size: 1.1rem;
}

.resume p {
  margin: 0;
  color: rgba(226, 232, 240, 0.75);
}

.resume__actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

@media (max-width: 960px) {
  .new-match__layout {
    grid-template-columns: 1fr;
  }
}
</style>
