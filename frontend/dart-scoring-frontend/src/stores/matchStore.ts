import { computed, ref, watch } from 'vue';
import { defineStore } from 'pinia';
import api from '@/api/client';
import type {
  CreateMatchRequest,
  LegStateResponse,
  LegSummaryResponse,
  MatchDetailResponse,
  PlayerResponse,
  SubmitTurnRequest,
  MatchMode,
  TurnResponse
} from '@/types/api';
import { useToastStore } from './toastStore';

const STORAGE_KEY = 'dart-scoring-state-v1';

interface PersistedState {
  match: MatchDetailResponse | null;
  legId: number | null;
}

const sortTurns = (turns: TurnResponse[]) =>
  [...turns].sort((a, b) => (a.turnNumber - b.turnNumber) || (a.id - b.id));

export const useMatchStore = defineStore('match', () => {
  const players = ref<PlayerResponse[]>([]);
  const activeMatch = ref<MatchDetailResponse | null>(null);
  const activeLeg = ref<LegStateResponse | null>(null);
  const legSummary = ref<LegSummaryResponse | null>(null);
  const isLoading = ref(false);

  const toastStore = useToastStore();

  const mode = computed<MatchMode | null>(() => activeMatch.value?.mode ?? null);
  const matchPlayers = computed(() => activeMatch.value?.players ?? []);

  const currentPlayerId = computed(() => {
    if (!activeMatch.value || !activeLeg.value) {
      return null;
    }

    const orderedPlayers = matchPlayers.value;
    if (!orderedPlayers.length) {
      return null;
    }

    const sortedTurns = sortTurns(activeLeg.value.turns);
    if (sortedTurns.length === 0) {
      return activeLeg.value.startingPlayerId;
    }

    const lastTurn = sortedTurns[sortedTurns.length - 1];
    const lastIndex = orderedPlayers.findIndex(p => p.id === lastTurn.playerId);
    if (lastIndex === -1) {
      return orderedPlayers[0].id;
    }

    const next = (lastIndex + 1) % orderedPlayers.length;
    return orderedPlayers[next]?.id ?? null;
  });

  function persist() {
    const payload: PersistedState = {
      match: activeMatch.value,
      legId: activeLeg.value?.id ?? null
    };
    localStorage.setItem(STORAGE_KEY, JSON.stringify(payload));
  }

  function restore() {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return;
    }

    try {
      const parsed = JSON.parse(raw) as PersistedState;
      if (parsed.match) {
        activeMatch.value = parsed.match;
        const leg = parsed.legId
          ? parsed.match.legs.find(l => l.id === parsed.legId)
          : parsed.match.legs.at(-1) ?? null;
        activeLeg.value = leg ?? null;
      }
    } catch (error) {
      console.warn('Failed to restore match state', error);
      localStorage.removeItem(STORAGE_KEY);
    }
  }

  async function fetchPlayers() {
    if (players.value.length > 0) {
      return;
    }

    const { data } = await api.get<PlayerResponse[]>('/api/players');
    players.value = data;
  }

  async function loadMatch(matchId: number) {
    isLoading.value = true;
    try {
      const { data } = await api.get<MatchDetailResponse>(`/api/matches/${matchId}`);
      setActiveMatch(data);
    } finally {
      isLoading.value = false;
    }
  }

  function setActiveMatch(match: MatchDetailResponse | null) {
    activeMatch.value = match;
    if (match) {
      activeLeg.value = match.legs.at(-1) ?? null;
    } else {
      activeLeg.value = null;
    }
    persist();
  }

  async function createMatch(request: CreateMatchRequest) {
    isLoading.value = true;
    try {
      const { data } = await api.post<MatchDetailResponse>('/api/matches', request);
      setActiveMatch(data);
      toastStore.success('Match ready. Good darts!');
    } finally {
      isLoading.value = false;
    }
  }

  async function submitTurn(legId: number, payload: SubmitTurnRequest) {
    if (!activeMatch.value) {
      return;
    }

    const { data } = await api.post<LegStateResponse>(`/api/legs/${legId}/turns`, payload);
    updateLegState(data);
    toastStore.success('Turn recorded');
  }

  async function undoTurn(turnId: number) {
    if (!activeMatch.value || !activeLeg.value) {
      return;
    }

    await api.post(`/api/turns/${turnId}/undo`);
    await loadMatch(activeMatch.value.id);
    toastStore.info('Last turn undone');
  }

  async function loadSummary(legId: number) {
    const { data } = await api.get<LegSummaryResponse>(`/api/legs/${legId}/summary`);
    legSummary.value = data;
  }

  function updateLegState(state: LegStateResponse) {
    if (!activeMatch.value) {
      return;
    }

    const legs = activeMatch.value.legs.map(leg => (leg.id === state.id ? state : leg));
    activeMatch.value = { ...activeMatch.value, legs };
    activeLeg.value = state;
    persist();
  }

  function clear() {
    activeMatch.value = null;
    activeLeg.value = null;
    legSummary.value = null;
    persist();
  }

  watch(activeMatch, persist, { deep: true });
  restore();

  return {
    players,
    mode,
    activeMatch,
    activeLeg,
    currentPlayerId,
    matchPlayers,
    legSummary,
    isLoading,
    fetchPlayers,
    loadMatch,
    createMatch,
    submitTurn,
    undoTurn,
    loadSummary,
    setActiveMatch,
    updateLegState,
    clear
  };
});
