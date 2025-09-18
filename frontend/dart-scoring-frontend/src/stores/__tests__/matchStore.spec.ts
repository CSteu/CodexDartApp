import { setActivePinia, createPinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useMatchStore } from '@/stores/matchStore';
import type {
  LegStateResponse,
  MatchDetailResponse,
  PlayerResponse,
  TurnResponse,
  X01PlayerStateResponse
} from '@/types/api';

vi.mock('@/api/client', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn()
  }
}));

import api from '@/api/client';

const STORAGE_KEY = 'dart-scoring-state-v1';
const mockedApi = api as unknown as {
  get: ReturnType<typeof vi.fn>;
  post: ReturnType<typeof vi.fn>;
};

const players: PlayerResponse[] = [
  { id: 1, displayName: 'Alice' },
  { id: 2, displayName: 'Bob' }
];

function baseLeg(overrides?: Partial<LegStateResponse>): LegStateResponse {
  return {
    id: 1,
    legNumber: 1,
    startingPlayerId: 1,
    winnerPlayerId: null,
    turns: [],
    x01State: players.map(player => ({
      playerId: player.id,
      remaining: 501,
      threeDartAverage: 0
    } satisfies X01PlayerStateResponse)),
    cricketState: null,
    ...overrides
  };
}

function baseMatch(overrides?: Partial<MatchDetailResponse>): MatchDetailResponse {
  const leg = overrides?.legs?.[0] ?? baseLeg();
  return {
    id: 1,
    mode: 'X01',
    targetScore: 501,
    doubleOut: true,
    status: 'InProgress',
    startedAt: new Date().toISOString(),
    finishedAt: null,
    players,
    legs: [leg],
    ...overrides
  };
}

beforeEach(() => {
  localStorage.clear();
  setActivePinia(createPinia());
  vi.clearAllMocks();
});

describe('matchStore', () => {
  it('determines the next player to throw based on turn order', () => {
    const store = useMatchStore();
    const initialLeg = baseLeg();

    store.setActiveMatch(baseMatch({ legs: [initialLeg] }));
    expect(store.currentPlayerId).toBe(1);

    const firstTurn: TurnResponse = {
      id: 10,
      turnNumber: 1,
      playerId: 1,
      totalScored: 60,
      wasBust: false,
      throws: []
    };

    const secondLegState = baseLeg({
      turns: [firstTurn],
      x01State: [
        { playerId: 1, remaining: 441, threeDartAverage: 60 },
        { playerId: 2, remaining: 501, threeDartAverage: 0 }
      ]
    });

    store.updateLegState(secondLegState);
    expect(store.currentPlayerId).toBe(2);

    const secondTurn: TurnResponse = {
      id: 11,
      turnNumber: 2,
      playerId: 2,
      totalScored: 45,
      wasBust: false,
      throws: []
    };

    const thirdLegState = baseLeg({
      turns: [firstTurn, secondTurn],
      x01State: [
        { playerId: 1, remaining: 441, threeDartAverage: 60 },
        { playerId: 2, remaining: 456, threeDartAverage: 45 }
      ]
    });

    store.updateLegState(thirdLegState);
    expect(store.currentPlayerId).toBe(1);
  });

  it('persists the active match to localStorage', () => {
    const store = useMatchStore();
    const match = baseMatch();

    store.setActiveMatch(match);

    const raw = localStorage.getItem(STORAGE_KEY);
    expect(raw).toBeTruthy();
    const parsed = JSON.parse(raw as string);
    expect(parsed.match.id).toBe(match.id);
    expect(parsed.legId).toBe(match.legs[0].id);
  });

  it('calls the API to undo the last turn and refreshes the match', async () => {
    const store = useMatchStore();
    const firstTurn: TurnResponse = {
      id: 50,
      turnNumber: 1,
      playerId: 1,
      totalScored: 60,
      wasBust: false,
      throws: []
    };
    const initialMatch = baseMatch({
      legs: [
        baseLeg({
          turns: [firstTurn],
          x01State: [
            { playerId: 1, remaining: 441, threeDartAverage: 60 },
            { playerId: 2, remaining: 501, threeDartAverage: 0 }
          ]
        })
      ]
    });

    store.setActiveMatch(initialMatch);

    const refreshedMatch = baseMatch({
      legs: [
        baseLeg({
          turns: [],
          x01State: [
            { playerId: 1, remaining: 501, threeDartAverage: 0 },
            { playerId: 2, remaining: 501, threeDartAverage: 0 }
          ]
        })
      ]
    });

    mockedApi.post.mockResolvedValue({});
    mockedApi.get.mockResolvedValue({ data: refreshedMatch });

    await store.undoTurn(firstTurn.id);

    expect(mockedApi.post).toHaveBeenCalledWith(`/api/turns/${firstTurn.id}/undo`);
    expect(mockedApi.get).toHaveBeenCalledWith(`/api/matches/${initialMatch.id}`);
    expect(store.activeMatch?.legs[0].turns.length).toBe(0);
  });
});
