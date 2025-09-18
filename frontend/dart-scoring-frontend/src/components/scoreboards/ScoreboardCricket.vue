<script setup lang="ts">
import { computed } from 'vue';
import type {
  CricketPlayerStateResponse,
  LegStateResponse,
  PlayerResponse
} from '@/types/api';

const props = defineProps<{
  players: PlayerResponse[];
  leg: LegStateResponse | null;
  currentPlayerId: number | null;
}>();

const orderedNumbers = [20, 19, 18, 17, 16, 15, 'Bull'] as const;

type CricketNumber = (typeof orderedNumbers)[number];

const stateByPlayer = computed(() => {
  const map = new Map<number, CricketPlayerStateResponse>();
  if (props.leg?.cricketState) {
    for (const state of props.leg.cricketState) {
      map.set(state.playerId, state);
    }
  }
  return map;
});

const playerStates = computed(() =>
  props.players.map(player => ({
    player,
    state: stateByPlayer.value.get(player.id)
  }))
);

function marksForNumber(state: CricketPlayerStateResponse | undefined, number: CricketNumber) {
  if (!state) {
    return 0;
  }

  if (number === 'Bull') {
    return state.bullMarks;
  }

  const key = `n${number}` as keyof CricketPlayerStateResponse;
  const marks = state[key];
  return typeof marks === 'number' ? marks : 0;
}

function isClosed(state: CricketPlayerStateResponse | undefined, number: CricketNumber) {
  return marksForNumber(state, number) >= 3;
}
</script>

<template>
  <section class="scoreboard">
    <header class="scoreboard__header">
      <span class="badge">Cricket</span>
      <p class="scoreboard__meta">Close 15-20 and the bull, then score on open numbers.</p>
    </header>

    <div class="cricket-table" role="table" aria-label="Cricket marks">
      <div class="cricket-row cricket-row--header" role="row">
        <div class="cricket-cell cricket-cell--number" role="columnheader">Number</div>
        <div
          v-for="entry in playerStates"
          :key="entry.player.id"
          class="cricket-cell cricket-cell--player"
          role="columnheader"
        >
          {{ entry.player.displayName }}
        </div>
      </div>

      <div
        v-for="number in orderedNumbers"
        :key="number"
        class="cricket-row"
        role="row"
      >
        <div class="cricket-cell cricket-cell--number" role="cell">
          {{ number }}
        </div>
        <div
          v-for="entry in playerStates"
          :key="entry.player.id"
          class="cricket-cell cricket-cell--marks"
          role="cell"
          :aria-label="`${entry.player.displayName} has ${marksForNumber(entry.state, number)} marks on ${number}`"
        >
          <div class="marks" :class="{ 'marks--closed': isClosed(entry.state, number) }">
            <span v-for="index in 3" :key="index" class="mark" :class="{ 'mark--filled': marksForNumber(entry.state, number) >= index }" />
          </div>
        </div>
      </div>
    </div>

    <div class="points">
      <div
        v-for="entry in playerStates"
        :key="entry.player.id"
        class="points__card"
        :class="{ 'points__card--active': currentPlayerId === entry.player.id }"
      >
        <div class="points__header">
          <h3>{{ entry.player.displayName }}</h3>
          <span v-if="currentPlayerId === entry.player.id" class="badge badge--glow">Throwing</span>
        </div>
        <p class="points__value">
          {{ entry.state?.points ?? 0 }}
        </p>
        <p class="points__label">Points</p>
      </div>
    </div>
  </section>
</template>

<style scoped>
.scoreboard {
  display: grid;
  gap: 1.5rem;
}

.scoreboard__header {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.scoreboard__meta {
  margin: 0;
  color: rgba(226, 232, 240, 0.7);
}

.cricket-table {
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 1rem;
  overflow: hidden;
  background: rgba(15, 23, 42, 0.6);
}

.cricket-row {
  display: grid;
  grid-template-columns: 120px repeat(auto-fit, minmax(140px, 1fr));
  border-bottom: 1px solid rgba(148, 163, 184, 0.12);
}

.cricket-row:last-child {
  border-bottom: none;
}

.cricket-row--header {
  background: rgba(15, 23, 42, 0.85);
  text-transform: uppercase;
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  color: rgba(148, 163, 184, 0.9);
}

.cricket-cell {
  padding: 0.75rem 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cricket-cell--number {
  justify-content: flex-start;
  font-weight: 600;
  font-size: 1rem;
}

.cricket-cell--player {
  justify-content: center;
  font-weight: 600;
}

.marks {
  display: inline-flex;
  gap: 0.4rem;
  align-items: center;
}

.marks--closed {
  color: var(--accent);
}

.mark {
  width: 0.75rem;
  height: 0.75rem;
  border-radius: 999px;
  border: 1px solid rgba(148, 163, 184, 0.5);
  display: inline-flex;
}

.mark--filled {
  background: var(--accent);
  border-color: rgba(52, 211, 153, 0.6);
  box-shadow: 0 0 0 2px rgba(52, 211, 153, 0.15);
}

.points {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1rem;
}

.points__card {
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 1rem;
  padding: 1.25rem;
  background: linear-gradient(160deg, rgba(31, 41, 55, 0.88), rgba(15, 23, 42, 0.92));
  transition: border-color 0.2s ease, box-shadow 0.2s ease, transform 0.2s ease;
}

.points__card--active {
  border-color: var(--accent);
  box-shadow: 0 18px 40px rgba(16, 185, 129, 0.25);
  transform: translateY(-2px);
}

.points__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.5rem;
}

.points__value {
  margin: 0;
  font-size: clamp(2rem, 4vw, 3rem);
  font-weight: 700;
  color: #f8fafc;
}

.points__label {
  margin-top: 0.25rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  font-size: 0.75rem;
  color: rgba(226, 232, 240, 0.65);
}

.badge--glow {
  background: rgba(52, 211, 153, 0.16);
  color: var(--accent);
  border: 1px solid rgba(52, 211, 153, 0.45);
}

@media (max-width: 640px) {
  .cricket-row {
    grid-template-columns: 90px repeat(auto-fit, minmax(120px, 1fr));
  }
}
</style>
