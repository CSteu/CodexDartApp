<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';
import ScoreboardX01 from '@/components/scoreboards/ScoreboardX01.vue';
import ScoreboardCricket from '@/components/scoreboards/ScoreboardCricket.vue';
import ThrowEntry from '@/components/ThrowEntry.vue';
import { useMatchStore } from '@/stores/matchStore';
import { useToastStore } from '@/stores/toastStore';
import type { MatchMode, SubmitThrow, ThrowResponse } from '@/types/api';

const props = defineProps<{ matchId: number }>();

const router = useRouter();
const matchStore = useMatchStore();
const toastStore = useToastStore();

const { activeMatch, activeLeg, currentPlayerId, matchPlayers, isLoading } = storeToRefs(matchStore);

const isSubmitting = ref(false);

const mode = computed<MatchMode | null>(() => activeMatch.value?.mode ?? null);
const playersForView = computed(() => matchPlayers.value);
const playerNameMap = computed(() => {
  const map = new Map<number, string>();
  for (const player of playersForView.value) {
    map.set(player.id, player.displayName);
  }
  return map;
});
const lastTurn = computed(() => {
  if (!activeLeg.value) {
    return null;
  }
  const turns = [...activeLeg.value.turns].sort(
    (a, b) => (a.turnNumber - b.turnNumber) || (a.id - b.id)
  );
  return turns.at(-1) ?? null;
});

const canUndoTurn = computed(() => !!lastTurn.value);

const isLegComplete = computed(() => !!activeLeg.value?.winnerPlayerId);
const winnerName = computed(() => {
  if (!isLegComplete.value || !activeLeg.value) {
    return null;
  }
  const name = playerNameMap.value.get(activeLeg.value.winnerPlayerId!);
  return name ?? null;
});

const currentPlayerName = computed(() => {
  if (!currentPlayerId.value) {
    return null;
  }
  return playerNameMap.value.get(currentPlayerId.value) ?? null;
});

const legDescriptor = computed(() => {
  if (!activeLeg.value) {
    return '';
  }
  const starterName = activeLeg.value.startingPlayerId
    ? playerNameMap.value.get(activeLeg.value.startingPlayerId)
    : null;
  return `Leg ${activeLeg.value.legNumber} • starts ${starterName ?? '—'}`;
});

const sortedTurns = computed(() => {
  if (!activeLeg.value) {
    return [] as typeof activeLeg.value.turns;
  }
  return [...activeLeg.value.turns].sort(
    (a, b) => (a.turnNumber - b.turnNumber) || (a.id - b.id)
  );
});

const isReady = computed(() => !!activeMatch.value && !!activeLeg.value);

function formatThrow(dart: ThrowResponse) {
  if (dart.segment === 50) {
    return 'Inner Bull';
  }
  if (dart.segment === 25) {
    return dart.multiplier === 2 ? 'Double Bull' : 'Outer Bull';
  }
  const prefix = dart.multiplier === 1 ? 'S' : dart.multiplier === 2 ? 'D' : 'T';
  return `${prefix}${dart.segment}`;
}

async function ensureMatchLoaded(matchId: number) {
  if (!matchId) {
    return;
  }

  if (!activeMatch.value || activeMatch.value.id !== matchId) {
    try {
      await matchStore.loadMatch(matchId);
    } catch (error) {
      console.error(error);
      toastStore.error('Could not load the requested match.');
      router.push({ name: 'new-match' });
    }
  }
}

onMounted(async () => {
  await matchStore.fetchPlayers();
  await ensureMatchLoaded(props.matchId);
});

watch(
  () => props.matchId,
  newId => {
    ensureMatchLoaded(newId);
  }
);

async function submitTurn(throwsPayload: SubmitThrow[]) {
  if (!activeLeg.value) {
    toastStore.error('No active leg to submit to.');
    return;
  }

  isSubmitting.value = true;
  try {
    await matchStore.submitTurn(activeLeg.value.id, { throws: throwsPayload });
    if (isLegComplete.value && activeMatch.value?.status === 'Completed') {
      toastStore.success('Match complete! View the summary to review every turn.');
    }
  } catch (error) {
    console.error(error);
    toastStore.error('Turn could not be recorded. Check your darts and try again.');
  } finally {
    isSubmitting.value = false;
  }
}

async function undoTurn() {
  if (!lastTurn.value) {
    return;
  }

  try {
    await matchStore.undoTurn(lastTurn.value.id);
  } catch (error) {
    console.error(error);
    toastStore.error('Undo failed.');
  }
}
</script>

<template>
  <section class="container live">
    <div class="page-header">
      <h1>Live scoring</h1>
      <p>Track every dart in real time. Dark board, bright shots.</p>
    </div>

    <div v-if="isLoading" class="card live__empty">
      Loading match…
    </div>

    <div v-else-if="!isReady" class="card live__empty">
      <p>No match loaded. Create a new match to begin scoring.</p>
      <RouterLink class="btn btn--primary" to="/">Go to setup</RouterLink>
    </div>

    <div v-else class="live__content">
      <header class="live__header card">
        <div class="live__info">
          <div class="live__mode">
            <span class="badge">{{ activeMatch?.mode }}</span>
            <span>{{ legDescriptor }}</span>
          </div>
          <div class="live__players">
            {{ playersForView.map(player => player.displayName).join(' vs ') }}
          </div>
        </div>
        <div class="live__status" aria-live="polite">
          <p v-if="winnerName" class="winner">{{ winnerName }} takes the leg!</p>
          <p v-else-if="currentPlayerName" class="current">{{ currentPlayerName }} to throw</p>
          <p v-else class="current">Waiting for throws…</p>
          <RouterLink class="btn btn--secondary" :to="{ name: 'summary', params: { id: activeMatch?.id } }">
            View summary
          </RouterLink>
        </div>
      </header>

      <div class="live__grid">
        <article class="card scoreboard-card">
          <ScoreboardCricket
            v-if="mode === 'Cricket'"
            :players="playersForView"
            :leg="activeLeg"
            :current-player-id="currentPlayerId"
          />
          <ScoreboardX01
            v-else
            :players="playersForView"
            :leg="activeLeg"
            :current-player-id="currentPlayerId"
            :target-score="activeMatch?.targetScore ?? 0"
            :double-out="activeMatch?.doubleOut ?? false"
          />
        </article>

        <article class="card entry-card">
          <ThrowEntry
            :key="activeLeg?.turns.length ?? 0"
            :mode="mode"
            :disabled="isLegComplete"
            :is-submitting="isSubmitting"
            :undo-disabled="!canUndoTurn"
            @submit="submitTurn"
            @undo="undoTurn"
          />
        </article>
      </div>

      <section class="card turns">
        <header class="turns__header">
          <h2>Turn history</h2>
          <span class="turns__count">{{ sortedTurns.length }} turns logged</span>
        </header>
        <div v-if="!sortedTurns.length" class="turns__empty">No darts have landed yet.</div>
        <table v-else class="table turns__table">
          <thead>
            <tr>
              <th scope="col">Turn</th>
              <th scope="col">Player</th>
              <th scope="col">Throws</th>
              <th scope="col">Total</th>
              <th scope="col">Result</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="turn in sortedTurns" :key="turn.id">
              <td>{{ turn.turnNumber }}</td>
              <td>{{ playerNameMap.get(turn.playerId) }}</td>
              <td>
                <span class="turns__throws">
                  {{ turn.throws.map(formatThrow).join(', ') || '—' }}
                </span>
              </td>
              <td>{{ turn.totalScored }}</td>
              <td>
                <span v-if="turn.wasBust" class="badge badge--bust">Bust</span>
                <span v-else>Scored</span>
              </td>
            </tr>
          </tbody>
        </table>
      </section>
    </div>
  </section>
</template>

<style scoped>
.live {
  display: grid;
  gap: 2rem;
}

.page-header h1 {
  margin: 0;
  font-size: clamp(2rem, 5vw, 2.75rem);
}

.page-header p {
  margin: 0.5rem 0 0;
  color: rgba(226, 232, 240, 0.7);
}

.live__empty {
  padding: 2rem;
  text-align: center;
  font-size: 1rem;
  color: rgba(226, 232, 240, 0.75);
  display: grid;
  gap: 1rem;
}

.live__content {
  display: grid;
  gap: 1.5rem;
}

.live__header {
  display: flex;
  justify-content: space-between;
  gap: 1.5rem;
  align-items: center;
}

.live__info {
  display: grid;
  gap: 0.5rem;
}

.live__mode {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-size: 0.95rem;
  color: rgba(226, 232, 240, 0.7);
}

.live__players {
  font-size: 1.1rem;
  font-weight: 600;
}

.live__status {
  display: grid;
  gap: 0.5rem;
  justify-items: end;
}

.live__status .current,
.live__status .winner {
  margin: 0;
  font-weight: 600;
}

.live__status .winner {
  color: var(--accent);
}

.live__grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 0.9fr);
  gap: 1.5rem;
}

.entry-card {
  align-self: start;
}

.turns {
  display: grid;
  gap: 1rem;
}

.turns__header {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
}

.turns__count {
  color: rgba(226, 232, 240, 0.6);
  font-size: 0.85rem;
}

.turns__empty {
  padding: 1rem 0;
  color: rgba(226, 232, 240, 0.65);
}

.turns__table {
  width: 100%;
}

.turns__throws {
  font-size: 0.95rem;
}

.badge--bust {
  background: rgba(248, 113, 113, 0.18);
  color: #fecaca;
}

@media (max-width: 1024px) {
  .live__header {
    flex-direction: column;
    align-items: flex-start;
  }

  .live__status {
    justify-items: flex-start;
  }

  .live__grid {
    grid-template-columns: 1fr;
  }
}
</style>
