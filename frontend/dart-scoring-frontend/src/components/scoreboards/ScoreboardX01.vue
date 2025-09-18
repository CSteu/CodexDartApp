<script setup lang="ts">
import { computed } from 'vue';
import { formatThrowsFromResponses } from '@/utils/dartUtils';
import type {
  LegStateResponse,
  PlayerResponse,
  TurnResponse,
  X01PlayerStateResponse
} from '@/types/api';

const props = defineProps<{
  players: PlayerResponse[];
  leg: LegStateResponse | null;
  currentPlayerId: number | null;
  targetScore: number;
  doubleOut: boolean;
}>();

const x01StateByPlayer = computed(() => {
  const map = new Map<number, X01PlayerStateResponse>();
  if (props.leg?.x01State) {
    for (const entry of props.leg.x01State) {
      map.set(entry.playerId, entry);
    }
  }
  return map;
});

const turnMap = computed(() => {
  const grouped = new Map<number, TurnResponse[]>();
  if (!props.leg) {
    return grouped;
  }

  for (const turn of props.leg.turns) {
    const list = grouped.get(turn.playerId) ?? [];
    list.push(turn);
    grouped.set(turn.playerId, list);
  }

  for (const list of grouped.values()) {
    list.sort((a, b) => (a.turnNumber - b.turnNumber) || (a.id - b.id));
  }
  return grouped;
});

const playerCards = computed(() =>
  props.players.map(player => {
    const state = x01StateByPlayer.value.get(player.id);
    const playerTurns = turnMap.value.get(player.id) ?? [];
    const dartsThrown = playerTurns.reduce((acc, turn) => acc + turn.throws.length, 0);
    const lastTurn = playerTurns.length ? playerTurns[playerTurns.length - 1] : null;
    const lastVisit = lastTurn
      ? {
          wasBust: lastTurn.wasBust,
          scoreLabel: lastTurn.wasBust ? 'Bust' : `${lastTurn.totalScored}`,
          throws: lastTurn.throws.length ? formatThrowsFromResponses(lastTurn.throws) : null
        }
      : null;

    if (state) {
      const scored = props.targetScore > 0 ? Math.max(props.targetScore - state.remaining, 0) : 0;
      const progress = props.targetScore > 0 ? Math.round((Math.min(scored, props.targetScore) / props.targetScore) * 100) : 0;
      return {
        player,
        remaining: state.remaining,
        average: state.threeDartAverage,
        progress,
        visits: playerTurns.length,
        dartsThrown,
        lastVisit
      };
    }

    const totalScore = playerTurns
      .filter(turn => !turn.wasBust)
      .reduce((acc, turn) => acc + turn.totalScored, 0);
    const remaining = Math.max(props.targetScore - totalScore, 0);
    const average = dartsThrown > 0 ? (totalScore / dartsThrown) * 3 : 0;
    const progress = props.targetScore > 0 ? Math.round((Math.min(totalScore, props.targetScore) / props.targetScore) * 100) : 0;

    return {
      player,
      remaining,
      average,
      progress,
      visits: playerTurns.length,
      dartsThrown,
      lastVisit
    };
  })
);

function formatAverage(value: number) {
  return value.toFixed(2);
}
</script>

<template>
  <section class="scoreboard">
    <header class="scoreboard__header">
      <span class="badge">X01</span>
      <div class="scoreboard__meta">
        <span>Target {{ targetScore }}</span>
        <span aria-label="Double out requirement">{{ doubleOut ? 'Double out required' : 'Straight out' }}</span>
      </div>
    </header>

    <div class="scoreboard__grid">
      <article
        v-for="card in playerCards"
        :key="card.player.id"
        class="score-card"
        :class="{ 'score-card--active': currentPlayerId === card.player.id }"
      >
        <header class="score-card__header">
          <h3>{{ card.player.displayName }}</h3>
          <span v-if="currentPlayerId === card.player.id" class="badge badge--glow">Throwing</span>
        </header>
        <p class="score-card__remaining" aria-label="Remaining score">
          {{ card.remaining }}
        </p>
        <div class="score-card__progress" role="group" aria-label="Progress toward finishing">
          <div class="score-card__progress-track" aria-hidden="true">
            <div class="score-card__progress-fill" :style="{ width: `${card.progress}%` }"></div>
          </div>
          <span class="score-card__progress-label">{{ card.progress }}% to finish</span>
        </div>
        <div class="score-card__metrics">
          <p class="score-card__metric">
            <span class="label">3-dart avg</span>
            <span class="score-card__metric-value">{{ formatAverage(card.average) }}</span>
          </p>
          <p class="score-card__metric">
            <span class="label">Visits</span>
            <span class="score-card__metric-value">
              {{ card.visits }}
              <span v-if="card.dartsThrown" class="score-card__muted">({{ card.dartsThrown }} darts)</span>
            </span>
          </p>
        </div>
        <p class="score-card__last">
          <span class="label">Last visit</span>
          <span v-if="card.lastVisit" :class="{ 'score-card__last--bust': card.lastVisit.wasBust }">
            {{ card.lastVisit.scoreLabel }}
            <span v-if="card.lastVisit.throws" class="score-card__last-throws">({{ card.lastVisit.throws }})</span>
          </span>
          <span v-else>—</span>
        </p>
      </article>
    </div>
  </section>
</template>

<style scoped>
.scoreboard {
  display: grid;
  gap: 1rem;
}

.scoreboard__header {
  display: flex;
  align-items: center;
  gap: 1rem;
  justify-content: space-between;
}

.scoreboard__meta {
  display: flex;
  gap: 1rem;
  font-size: 0.95rem;
  color: rgba(226, 232, 240, 0.7);
}

.scoreboard__grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 1rem;
}

.score-card {
  border: 1px solid rgba(148, 163, 184, 0.18);
  border-radius: 1rem;
  padding: 1.25rem;
  background: linear-gradient(160deg, rgba(31, 41, 55, 0.88), rgba(15, 23, 42, 0.92));
  position: relative;
  transition: transform 0.2s ease, border-color 0.2s ease, box-shadow 0.2s ease;
}

.score-card--active {
  border-color: var(--accent);
  box-shadow: 0 18px 40px rgba(16, 185, 129, 0.25);
  transform: translateY(-2px);
}

.score-card__header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 0.75rem;
}

.score-card__remaining {
  font-size: clamp(2.5rem, 5vw, 3.5rem);
  font-weight: 700;
  margin: 0;
  color: #f8fafc;
}

.label {
  font-size: 0.75rem;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.score-card__progress {
  display: grid;
  gap: 0.5rem;
  margin-top: 0.75rem;
}

.score-card__progress-track {
  width: 100%;
  height: 0.5rem;
  background: rgba(148, 163, 184, 0.2);
  border-radius: 999px;
  overflow: hidden;
}

.score-card__progress-fill {
  height: 100%;
  background: linear-gradient(90deg, var(--accent), rgba(16, 185, 129, 0.75));
}

.score-card__progress-label {
  font-size: 0.85rem;
  color: rgba(226, 232, 240, 0.75);
}

.score-card__metrics {
  display: grid;
  gap: 0.75rem;
  margin-top: 1rem;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
}

.score-card__metric {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.75rem;
  color: rgba(226, 232, 240, 0.75);
  margin: 0;
}

.score-card__metric-value {
  font-size: 1rem;
  font-weight: 600;
  color: #f8fafc;
}

.score-card__muted {
  font-size: 0.85rem;
  color: rgba(226, 232, 240, 0.6);
  margin-left: 0.35rem;
}

.score-card__last {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-top: 1.25rem;
  color: rgba(226, 232, 240, 0.75);
  gap: 0.75rem;
}

.score-card__last span:last-child {
  font-size: 1rem;
  font-weight: 600;
  color: #f8fafc;
}

.score-card__last--bust {
  color: #fecaca;
}

.score-card__last-throws {
  margin-left: 0.35rem;
  font-size: 0.85rem;
  color: rgba(226, 232, 240, 0.65);
}

.badge--glow {
  background: rgba(52, 211, 153, 0.16);
  color: var(--accent);
  border: 1px solid rgba(52, 211, 153, 0.45);
}

@media (max-width: 640px) {
  .scoreboard__meta {
    flex-direction: column;
    align-items: flex-start;
  }
}
</style>
