export type MatchMode = 'X01' | 'Cricket';
export type MatchStatus = 'Pending' | 'InProgress' | 'Completed';

export interface PlayerResponse {
  id: number;
  displayName: string;
}

export interface ThrowResponse {
  id: number;
  multiplier: number;
  segment: number;
  scoreValue: number;
}

export interface TurnResponse {
  id: number;
  turnNumber: number;
  playerId: number;
  totalScored: number;
  wasBust: boolean;
  throws: ThrowResponse[];
}

export interface X01PlayerStateResponse {
  playerId: number;
  remaining: number;
  threeDartAverage: number;
}

export interface CricketPlayerStateResponse {
  playerId: number;
  n15: number;
  n16: number;
  n17: number;
  n18: number;
  n19: number;
  n20: number;
  bullMarks: number;
  points: number;
}

export interface LegStateResponse {
  id: number;
  legNumber: number;
  startingPlayerId: number;
  winnerPlayerId: number | null;
  turns: TurnResponse[];
  x01State?: X01PlayerStateResponse[] | null;
  cricketState?: CricketPlayerStateResponse[] | null;
}

export interface MatchDetailResponse {
  id: number;
  mode: MatchMode;
  targetScore: number;
  doubleOut: boolean;
  status: MatchStatus;
  startedAt: string;
  finishedAt: string | null;
  players: PlayerResponse[];
  legs: LegStateResponse[];
}

export interface PlayerLegSummary {
  playerId: number;
  threeDartAverage: number;
  remaining?: number | null;
  cricketPoints?: number | null;
}

export interface LegSummaryResponse {
  legId: number;
  turns: TurnResponse[];
  playerSummaries: PlayerLegSummary[];
}

export interface SubmitThrow {
  multiplier: number;
  segment: number;
}

export interface SubmitTurnRequest {
  throws: SubmitThrow[];
}

export interface CreateMatchRequest {
  mode: MatchMode;
  targetScore: number;
  doubleOut: boolean;
  playerIds: number[];
  startingPlayerId?: number | null;
}
