<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';
import ScoreboardX01 from '@/components/scoreboards/ScoreboardX01.vue';
import ScoreboardCricket from '@/components/scoreboards/ScoreboardCricket.vue';
import ThrowEntry from '@/components/ThrowEntry.vue';
import { useMatchStore } from '@/stores/matchStore';
import { useToastStore } from '@/stores/toastStore';
import {
  formatCheckoutRoute,
  formatThrowsFromResponses
} from '@/utils/dartUtils';
import { getCheckoutSuggestions } from '@/utils/checkouts';
import type {
  CricketPlayerStateResponse,
  MatchMode,
  SubmitThrow,
  TurnResponse
} from '@/types/api';

const props = defineProps<{ matchId: number }>();

const cricketNumbers = [20, 19, 18, 17, 16, 15, 'Bull'] as const;
type CricketNumber = (typeof cricketNumbers)[number];
type TurnHighlight = 'max' | 'power' | 'ton' | 'bust' | null;

interface TurnBadge {
  label: string;
  class: string;
}

interface DecoratedTurn {
  turn: TurnResponse;
  badge: TurnBadge | null;
  highlight: TurnHighlight;
  isRecent: boolean;
}

interface InsightStat {
  label: string;
  value: string;
  description?: string;
}

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
  return turns.length > 0 ? turns[turns.length - 1] : null;
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

const sortedTurns = computed<TurnResponse[]>(() => {
  if (!activeLeg.value) {
    return [];
  }
  return [...activeLeg.value.turns].sort(
    (a, b) => (a.turnNumber - b.turnNumber) || (a.id - b.id)
  );
});

const isReady = computed(() => !!activeMatch.value && !!activeLeg.value);

const scoringTurns = computed(() => sortedTurns.value.filter(turn => !turn.wasBust));

const highTurn = computed(() => {
  const turns = scoringTurns.value;
  if (!turns.length) {
    return null;
  }
  const [first, ...rest] = turns as [TurnResponse, ...TurnResponse[]];
  let bestTurn: TurnResponse = first;
  for (const turn of rest) {
    if (turn.totalScored > bestTurn.totalScored) {
      bestTurn = turn;
    }
  }
  return bestTurn;
});

const tonCounts = computed(() => {
  const map = new Map<number, number>();
  for (const turn of scoringTurns.value) {
    if (turn.totalScored >= 100) {
      map.set(turn.playerId, (map.get(turn.playerId) ?? 0) + 1);
    }
  }
  return map;
});

const tonSummary = computed(() => {
  const entries = playersForView.value
    .map(player => {
      const count = tonCounts.value.get(player.id) ?? 0;
      return count > 0 ? `${player.displayName} ×${count}` : null;
    })
    .filter((entry): entry is string => entry !== null);

  if (!entries.length) {
    return null;
  }

  return entries.join(' • ');
});

const legMomentum = computed(() => {
  if (!activeLeg.value) {
    return null;
  }

  if (mode.value === 'X01') {
    const state = activeLeg.value.x01State;
    if (!state?.length) {
      return null;
    }

    const scoreboard = playersForView.value.map(player => ({
      player,
      remaining:
        state.find(entry => entry.playerId === player.id)?.remaining ?? activeMatch.value?.targetScore ?? 0
    }));

    if (scoreboard.length === 0) {
      return null;
    }

    scoreboard.sort((a, b) => a.remaining - b.remaining);
    const leader = scoreboard[0];
    const trailer = scoreboard[scoreboard.length - 1];
    if (!leader || !trailer) {
      return null;
    }

    if (leader.remaining === trailer.remaining) {
      return `Level at ${leader.remaining}`;
    }

    const diff = trailer.remaining - leader.remaining;
    return `${leader.player.displayName} ahead by ${diff}`;
  }

  if (mode.value === 'Cricket') {
    const states = activeLeg.value.cricketState;
    if (!states?.length) {
      return null;
    }

    const scoreboard = playersForView.value.map(player => ({
      player,
      points: states.find(entry => entry.playerId === player.id)?.points ?? 0
    }));

    if (scoreboard.length === 0) {
      return null;
    }

    scoreboard.sort((a, b) => b.points - a.points);
    const leader = scoreboard[0];
    const trailer = scoreboard[scoreboard.length - 1];

    if (!leader || !trailer) {
      return null;
    }

    if (leader.points === trailer.points) {
      return 'Level on points – close the remaining numbers.';
    }

    const diff = leader.points - trailer.points;
    if (diff >= 0) {
      return `${leader.player.displayName} leads by ${Math.abs(diff)} points`;
    }

    return `${trailer.player.displayName} leads by ${Math.abs(diff)} points`;
  }

  return null;
});

function classifyTurn(turn: TurnResponse): TurnHighlight {
  if (turn.wasBust) {
    return 'bust';
  }
  if (turn.totalScored === 180) {
    return 'max';
  }
  if (turn.totalScored >= 140) {
    return 'power';
  }
  if (turn.totalScored >= 100) {
    return 'ton';
  }
  return null;
}

function buildTurnBadge(turn: TurnResponse): TurnBadge | null {
  const highlight = classifyTurn(turn);
  switch (highlight) {
    case 'max':
      return { label: '180', class: 'badge--max' };
    case 'power':
      return { label: '140+', class: 'badge--power' };
    case 'ton':
      return { label: 'Ton+', class: 'badge--ton' };
    default:
      return null;
  }
}

const decoratedTurns = computed<DecoratedTurn[]>(() =>
  sortedTurns.value.map(turn => ({
    turn,
    badge: buildTurnBadge(turn),
    highlight: classifyTurn(turn),
    isRecent: lastTurn.value?.id === turn.id
  }))
);

function marksForNumber(state: CricketPlayerStateResponse | undefined, number: CricketNumber): number {
  if (!state) {
    return 0;
  }

  if (number === 'Bull') {
    return state.bullMarks;
  }

  const key = `n${number}` as keyof CricketPlayerStateResponse;
  const value = state[key];
  return typeof value === 'number' ? value : 0;
}

const cricketTargets = computed(() => {
  if (mode.value !== 'Cricket' || !activeLeg.value || !currentPlayerId.value) {
    return [] as {
      number: string;
      marksNeeded: number;
      opponentClosed: boolean;
    }[];
  }

  const states = activeLeg.value.cricketState ?? [];
  const playerState = states.find(entry => entry.playerId === currentPlayerId.value);
  if (!playerState) {
    return [];
  }

  const opponentState = states.find(entry => entry.playerId !== currentPlayerId.value);

  return cricketNumbers
    .map(number => {
      const marks = marksForNumber(playerState, number);
      const needed = Math.max(0, 3 - marks);
      const opponentMarks = marksForNumber(opponentState, number);

      return {
        number: number === 'Bull' ? 'Bull' : number.toString(),
        marksNeeded: needed,
        opponentClosed: opponentMarks >= 3
      };
    })
    .filter(entry => entry.marksNeeded > 0);
});

const checkoutDetails = computed(() => {
  if (mode.value !== 'X01' || !activeLeg.value || !activeMatch.value || !currentPlayerId.value) {
    return null;
  }

  const x01State = activeLeg.value.x01State;
  if (!x01State) {
    return null;
  }

  const state = x01State.find(entry => entry.playerId === currentPlayerId.value);
  if (!state) {
    return null;
  }

  const suggestion = getCheckoutSuggestions(state.remaining, activeMatch.value.doubleOut);
  return {
    remaining: state.remaining,
    note: suggestion.note,
    finishable: suggestion.finishable,
    routes: suggestion.routes.slice(0, 4).map(route => formatCheckoutRoute(route))
  };
});

const checkoutRoutes = computed(() => checkoutDetails.value?.routes ?? []);
const checkoutNote = computed(() => checkoutDetails.value?.note ?? '');

const insightStats = computed<InsightStat[]>(() => {
  const stats: InsightStat[] = [];

  if (highTurn.value) {
    const turn = highTurn.value;
    const name = playerNameMap.value.get(turn.playerId) ?? 'Unknown';
    stats.push({
      label: 'High turn',
      value: `${turn.totalScored}`,
      description: `Turn ${turn.turnNumber} by ${name}`
    });
  } else {
    stats.push({
      label: 'High turn',
      value: '—',
      description: 'Waiting for the first scoring visit.'
    });
  }

  if (tonSummary.value) {
    stats.push({ label: 'Ton+ visits', value: tonSummary.value });
  } else {
    stats.push({ label: 'Ton+ visits', value: 'None yet' });
  }

  if (legMomentum.value) {
    stats.push({
      label: mode.value === 'Cricket' ? 'Points race' : 'Leg momentum',
      value: legMomentum.value
    });
  }

  if (lastTurn.value) {
    const descriptor = lastTurn.value.wasBust ? 'Bust' : `${lastTurn.value.totalScored}`;
    const name = playerNameMap.value.get(lastTurn.value.playerId) ?? 'Unknown';
    stats.push({ label: 'Last turn', value: descriptor, description: name });
  }

  return stats;
});

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

      <section class="card live__insights">
        <div class="insights__primary">
          <template v-if="mode === 'X01'">
            <h2>Checkout guide</h2>
            <p class="insights__note">{{ checkoutNote }}</p>
            <ul v-if="checkoutRoutes.length" class="insights__list">
              <li v-for="route in checkoutRoutes" :key="route">{{ route }}</li>
            </ul>
            <p v-else class="insights__empty">No direct finish this visit.</p>
          </template>
          <template v-else>
            <h2>Targets to close</h2>
            <p class="insights__note">Keep the marks flowing to unlock points.</p>
            <ul v-if="cricketTargets.length" class="insights__list">
              <li v-for="target in cricketTargets" :key="target.number">
                <span class="insights__target">{{ target.number }}</span>
                <span class="insights__chip">
                  {{ target.marksNeeded }} mark{{ target.marksNeeded > 1 ? 's' : '' }} needed
                </span>
                <span
                  v-if="target.opponentClosed"
                  class="insights__chip insights__chip--muted"
                >
                  Opponent closed
                </span>
              </li>
            </ul>
            <p v-else class="insights__empty">Everything is closed – rack up the points.</p>
          </template>
        </div>

        <div class="insights__stats">
          <article v-for="stat in insightStats" :key="stat.label" class="insight">
            <span class="insight__label">{{ stat.label }}</span>
            <strong class="insight__value">{{ stat.value }}</strong>
            <span v-if="stat.description" class="insight__description">{{ stat.description }}</span>
          </article>
        </div>
      </section>

      <section class="card turns">
        <header class="turns__header">
          <h2>Turn history</h2>
          <span class="turns__count">{{ decoratedTurns.length }} turns logged</span>
        </header>
        <div v-if="!sortedTurns.length" class="turns__empty">No darts have landed yet.</div>
        <div v-else class="turns__table-wrapper">
          <table class="table turns__table">
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
              <tr
                v-for="{ turn, badge, highlight, isRecent } in decoratedTurns"
                :key="turn.id"
                :class="{
                  'turns__row--recent': isRecent,
                  'turns__row--power': highlight === 'power' || highlight === 'max',
                  'turns__row--ton': highlight === 'ton',
                  'turns__row--bust': highlight === 'bust'
                }"
              >
                <td>{{ turn.turnNumber }}</td>
                <td>{{ playerNameMap.get(turn.playerId) }}</td>
                <td>
                  <span class="turns__throws">
                    {{ turn.throws.length ? formatThrowsFromResponses(turn.throws) : '—' }}
                  </span>
                </td>
                <td>
                  <span class="turns__total">
                    {{ turn.totalScored }}
                    <span v-if="badge" class="badge" :class="badge.class">{{ badge.label }}</span>
                  </span>
                </td>
                <td>
                  <span v-if="turn.wasBust" class="badge badge--bust">Bust</span>
                  <span v-else>Scored</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
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

.live__insights {
  display: grid;
  gap: 1.5rem;
  grid-template-columns: minmax(0, 1.2fr) minmax(0, 1fr);
  align-items: start;
}

.insights__primary {
  display: grid;
  gap: 0.75rem;
}

.insights__note {
  margin: 0;
  color: rgba(226, 232, 240, 0.65);
  font-size: 0.9rem;
}

.insights__list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 0.5rem;
}

.insights__list li {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
}

.insights__target {
  font-weight: 600;
  font-size: 1rem;
}

.insights__chip {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.25rem 0.6rem;
  border-radius: 999px;
  font-size: 0.8rem;
  background: rgba(148, 163, 184, 0.12);
  border: 1px solid rgba(148, 163, 184, 0.25);
  color: rgba(226, 232, 240, 0.85);
}

.insights__chip--muted {
  background: rgba(148, 163, 184, 0.2);
  color: rgba(226, 232, 240, 0.7);
}

.insights__empty {
  margin: 0;
  color: rgba(226, 232, 240, 0.6);
}

.insights__stats {
  display: grid;
  gap: 0.75rem;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
}

.insight {
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 1rem;
  background: rgba(15, 23, 42, 0.55);
  padding: 1rem 1.1rem;
  display: grid;
  gap: 0.35rem;
}

.insight__label {
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: rgba(148, 163, 184, 0.75);
}

.insight__value {
  font-size: 1.35rem;
  font-weight: 600;
}

.insight__description {
  color: rgba(226, 232, 240, 0.7);
  font-size: 0.85rem;
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

.turns__table-wrapper {
  width: 100%;
  overflow-x: auto;
  padding-bottom: 0.5rem;
}

.turns__table {
  width: 100%;
  min-width: 520px;
}

.turns__table tbody tr {
  transition: background 0.2s ease;
}

.turns__throws {
  font-size: 0.95rem;
}

.turns__total {
  display: inline-flex;
  align-items: center;
  gap: 0.5rem;
}

.turns__row--recent {
  background: rgba(52, 211, 153, 0.06);
}

.turns__row--power {
  background: rgba(16, 185, 129, 0.08);
}

.turns__row--ton {
  background: rgba(56, 189, 248, 0.08);
}

.turns__row--bust {
  background: rgba(248, 113, 113, 0.08);
}

.turns__row--recent td:first-child {
  border-left: 3px solid var(--accent);
}

.badge--bust {
  background: rgba(248, 113, 113, 0.18);
  color: #fecaca;
}

.badge--power {
  background: rgba(16, 185, 129, 0.25);
  color: var(--accent);
}

.badge--ton {
  background: rgba(59, 130, 246, 0.25);
  color: #bfdbfe;
}

.badge--max {
  background: rgba(248, 113, 113, 0.25);
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

  .live__insights {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .live__status {
    justify-items: flex-start;
    width: 100%;
  }

  .turns__header {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
  }

  .turns__table {
    min-width: 480px;
  }
}
</style>
