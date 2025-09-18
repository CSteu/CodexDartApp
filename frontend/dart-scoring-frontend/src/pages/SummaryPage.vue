<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';
import { useMatchStore } from '@/stores/matchStore';
import { useToastStore } from '@/stores/toastStore';
import { formatThrowsFromResponses } from '@/utils/dartUtils';

const props = defineProps<{ matchId: number }>();

const matchStore = useMatchStore();
const toastStore = useToastStore();
const router = useRouter();

const { activeMatch, activeLeg, legSummary, matchPlayers, isLoading } = storeToRefs(matchStore);

const isSummaryLoading = ref(false);

const playersForView = computed(() => matchPlayers.value);
const playerNameMap = computed(() => {
  const map = new Map<number, string>();
  for (const player of playersForView.value) {
    map.set(player.id, player.displayName);
  }
  return map;
});

const mode = computed(() => activeMatch.value?.mode ?? null);
const legId = computed(() => activeLeg.value?.id ?? null);
const legDescriptor = computed(() => {
  if (!activeLeg.value) {
    return '';
  }
  const starterName = activeLeg.value.startingPlayerId
    ? playerNameMap.value.get(activeLeg.value.startingPlayerId)
    : null;
  return `Leg ${activeLeg.value.legNumber} • started by ${starterName ?? '—'}`;
});

const sortedTurns = computed(() => {
  if (!legSummary.value) {
    return [];
  }
  return [...legSummary.value.turns].sort(
    (a, b) => (a.turnNumber - b.turnNumber) || (a.id - b.id)
  );
});

const playerSummaries = computed(() => {
  if (!legSummary.value) {
    return [];
  }
  return legSummary.value.playerSummaries.map(summary => ({
    ...summary,
    name: playerNameMap.value.get(summary.playerId) ?? 'Unknown player'
  }));
});

const isReady = computed(() => !!activeMatch.value && !!legSummary.value);

function formatAverage(value: number) {
  return value.toFixed(2);
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
      toastStore.error('Unable to load the requested match.');
      router.push({ name: 'new-match' });
    }
  }
}

async function loadSummaryData() {
  if (!legId.value) {
    return;
  }

  isSummaryLoading.value = true;
  try {
    await matchStore.loadSummary(legId.value);
  } catch (error) {
    console.error(error);
    toastStore.error('Summary could not be loaded.');
  } finally {
    isSummaryLoading.value = false;
  }
}

onMounted(async () => {
  await matchStore.fetchPlayers();
  await ensureMatchLoaded(props.matchId);
  await loadSummaryData();
});

watch(
  () => props.matchId,
  async newId => {
    await ensureMatchLoaded(newId);
    await loadSummaryData();
  }
);

watch(legId, async newLegId => {
  if (newLegId) {
    await loadSummaryData();
  }
});
</script>

<template>
  <section class="container summary">
    <div class="page-header">
      <h1>Leg summary</h1>
      <p>Review every turn, average, and closing shot.</p>
    </div>

    <div v-if="isLoading || isSummaryLoading" class="card summary__empty">Loading summary…</div>

    <div v-else-if="!isReady" class="card summary__empty">
      <p>No summary is available yet. Head back to live scoring to start a leg.</p>
      <RouterLink class="btn btn--primary" :to="{ name: 'live-scoring', params: { id: props.matchId } }">
        Back to live scoring
      </RouterLink>
    </div>

    <div v-else class="summary__content">
      <header class="card summary__header">
        <div>
          <div class="summary__meta">
            <span class="badge">{{ mode }}</span>
            <span>{{ legDescriptor }}</span>
          </div>
          <h2>{{ playersForView.map(player => player.displayName).join(' vs ') }}</h2>
        </div>
        <RouterLink class="btn btn--secondary" :to="{ name: 'live-scoring', params: { id: props.matchId } }">
          Return to live scoring
        </RouterLink>
      </header>

      <section class="card summary__stats">
        <h3 class="section-title">Player stats</h3>
        <div class="stats-grid">
          <article v-for="summary in playerSummaries" :key="summary.playerId" class="stats-card">
            <h4>{{ summary.name }}</h4>
            <dl>
              <div>
                <dt>3-dart average</dt>
                <dd>{{ formatAverage(summary.threeDartAverage) }}</dd>
              </div>
              <div v-if="summary.remaining !== undefined && summary.remaining !== null">
                <dt>Remaining</dt>
                <dd>{{ summary.remaining }}</dd>
              </div>
              <div v-if="summary.cricketPoints !== undefined && summary.cricketPoints !== null">
                <dt>Cricket points</dt>
                <dd>{{ summary.cricketPoints }}</dd>
              </div>
            </dl>
          </article>
        </div>
      </section>

      <section class="card summary__turns">
        <header class="turns__header">
          <h3>Turn breakdown</h3>
          <span class="turns__count">{{ sortedTurns.length }} turns</span>
        </header>
        <table class="table">
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
              <td>{{ turn.throws.length ? formatThrowsFromResponses(turn.throws) : '—' }}</td>
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
.summary {
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

.summary__empty {
  padding: 2rem;
  text-align: center;
  color: rgba(226, 232, 240, 0.75);
  display: grid;
  gap: 1rem;
}

.summary__content {
  display: grid;
  gap: 1.5rem;
}

.summary__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 1.5rem;
}

.summary__meta {
  display: flex;
  gap: 0.75rem;
  font-size: 0.95rem;
  color: rgba(226, 232, 240, 0.7);
}

.summary__stats {
  display: grid;
  gap: 1rem;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.stats-card {
  border: 1px solid rgba(148, 163, 184, 0.16);
  border-radius: 1rem;
  padding: 1.25rem;
  background: rgba(15, 23, 42, 0.55);
  display: grid;
  gap: 0.75rem;
}

.stats-card h4 {
  margin: 0;
  font-size: 1.1rem;
}

dl {
  margin: 0;
  display: grid;
  gap: 0.5rem;
}

dl div {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 0.5rem;
}

dt {
  text-transform: uppercase;
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  color: rgba(148, 163, 184, 0.75);
}

dd {
  margin: 0;
  font-weight: 600;
}

.summary__turns {
  display: grid;
  gap: 1rem;
}

.turns__header {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
}

.turns__count {
  color: rgba(226, 232, 240, 0.6);
  font-size: 0.85rem;
}

.badge--bust {
  background: rgba(248, 113, 113, 0.18);
  color: #fecaca;
}

@media (max-width: 960px) {
  .summary__header {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
