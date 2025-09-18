import type { CheckoutRoute } from './dartUtils';
import { dartScore } from './dartUtils';

type ShotOption = {
  multiplier: number;
  segment: number;
  score: number;
  id: string;
  isDouble: boolean;
};

type CheckoutTable = Map<number, CheckoutRoute[]>;

type CheckoutGenerationResult = {
  table: CheckoutTable;
};

function createShot(multiplier: number, segment: number): ShotOption {
  const score = dartScore({ multiplier, segment });
  return {
    multiplier,
    segment,
    score,
    id: `${segment}x${multiplier}`,
    isDouble: multiplier === 2 || segment === 50
  };
}

const orderedSegments = Array.from({ length: 20 }, (_, index) => 20 - index);

const singles = orderedSegments.map(segment => createShot(1, segment));
const doubles = orderedSegments.map(segment => createShot(2, segment));
const triples = orderedSegments.map(segment => createShot(3, segment));
const outerBull = createShot(1, 25);
const innerBull = createShot(1, 50);

singles.push(outerBull);

const setupShotsDoubleOut: ShotOption[] = [...triples, ...singles];
const setupShotsStraightOut: ShotOption[] = [...triples, ...doubles, ...singles, innerBull];

const finishingShotsDoubleOut: ShotOption[] = [innerBull, ...doubles];
const finishingShotsStraightOut: ShotOption[] = [innerBull, ...doubles, ...triples, ...singles];

function compareShotsDesc(a: ShotOption, b: ShotOption): number {
  if (a.score !== b.score) {
    return b.score - a.score;
  }
  if (a.multiplier !== b.multiplier) {
    return b.multiplier - a.multiplier;
  }
  return b.segment - a.segment;
}

function compareRoutes(a: ShotOption[], b: ShotOption[]): number {
  if (a.length !== b.length) {
    return a.length - b.length;
  }

  for (let index = 0; index < Math.min(a.length, b.length); index++) {
    const shotA = a[index]!;
    const shotB = b[index]!;
    const diff = compareShotsDesc(shotA, shotB);
    if (diff !== 0) {
      return diff;
    }
  }

  return 0;
}

function buildCheckoutTable(requireDouble: boolean): CheckoutGenerationResult {
  const table = new Map<number, ShotOption[][]>();
  const seenKeys = new Map<number, Set<string>>();

  const maxTarget = requireDouble ? 170 : 180;
  const setupShots = requireDouble ? setupShotsDoubleOut : setupShotsStraightOut;
  const finishingShots = requireDouble ? finishingShotsDoubleOut : finishingShotsStraightOut;

  const tryAddRoute = (target: number, route: ShotOption[]) => {
    if (target <= 0 || target > maxTarget) {
      return;
    }
    if (requireDouble && target < 2) {
      return;
    }

    const key = route.map(shot => shot.id).join('|');
    let bucketKeys = seenKeys.get(target);
    if (!bucketKeys) {
      bucketKeys = new Set<string>();
      seenKeys.set(target, bucketKeys);
    }
    if (bucketKeys.has(key)) {
      return;
    }

    bucketKeys.add(key);

    const bucket = table.get(target) ?? [];
    bucket.push(route.slice());
    table.set(target, bucket);
  };

  for (const finisher of finishingShots) {
    tryAddRoute(finisher.score, [finisher]);

    for (const first of setupShots) {
      tryAddRoute(first.score + finisher.score, [first, finisher]);
    }

    for (let i = 0; i < setupShots.length; i += 1) {
      const first = setupShots[i]!;
      for (let j = i; j < setupShots.length; j += 1) {
        const second = setupShots[j]!;
        tryAddRoute(first.score + second.score + finisher.score, [first, second, finisher]);
      }
    }
  }

  const checkoutTable: CheckoutTable = new Map();

  for (const [target, routes] of table.entries()) {
    routes.sort(compareRoutes);
    checkoutTable.set(
      target,
      routes.map(route => route.map(({ multiplier, segment }) => ({ multiplier, segment })))
    );
  }

  return { table: checkoutTable };
}

const doubleOutTable = buildCheckoutTable(true).table;
const straightOutTable = buildCheckoutTable(false).table;

export interface CheckoutSuggestion {
  routes: CheckoutRoute[];
  note: string;
  finishable: boolean;
}

export function getCheckoutSuggestions(remaining: number, doubleOut: boolean): CheckoutSuggestion {
  if (remaining <= 0) {
    return {
      routes: [],
      note: 'Leg complete – enjoy the roar of the crowd.',
      finishable: true
    };
  }

  if (doubleOut) {
    if (remaining > 170) {
      return {
        routes: [],
        note: 'No finish above 170. Stack scores to leave a preferred double.',
        finishable: false
      };
    }

    const routes = doubleOutTable.get(remaining) ?? [];
    if (routes.length > 0) {
      return {
        routes,
        note: 'Double-out routes available in three darts.',
        finishable: true
      };
    }

    return {
      routes: [],
      note: remaining % 2 === 0
        ? 'Set up a comfortable double for next visit.'
        : 'No checkout this visit – leave an even number or bull.',
      finishable: false
    };
  }

  if (remaining > 180) {
    return {
      routes: [],
      note: 'Too high for a straight-out finish. Keep piling on the scores.',
      finishable: false
    };
  }

  const routes = straightOutTable.get(remaining) ?? [];
  if (routes.length > 0) {
    return {
      routes,
      note: 'Straight-out finish options in hand.',
      finishable: true
    };
  }

  return {
    routes: [],
    note: 'Aim to leave a simple finish – even numbers give the most options.',
    finishable: false
  };
}
