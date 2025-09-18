import type { SubmitThrow, ThrowResponse } from '@/types/api';

type DartLike = Pick<SubmitThrow, 'multiplier' | 'segment'>;

export function formatDart(dart: DartLike): string {
  if (dart.segment === 50) {
    return 'Inner Bull';
  }
  if (dart.segment === 25) {
    return dart.multiplier === 2 ? 'Double Bull' : 'Outer Bull';
  }

  const prefix = dart.multiplier === 1 ? 'S' : dart.multiplier === 2 ? 'D' : 'T';
  return `${prefix}${dart.segment}`;
}

export function dartScore(dart: DartLike): number {
  if (dart.segment === 50) {
    return 50;
  }
  if (dart.segment === 25) {
    return Math.min(dart.multiplier, 2) * 25;
  }
  return dart.multiplier * dart.segment;
}

export function isDouble(dart: DartLike): boolean {
  if (dart.segment === 50) {
    return true;
  }
  if (dart.segment === 25) {
    return dart.multiplier === 2;
  }
  return dart.multiplier === 2;
}

export function formatThrowResponse(dart: ThrowResponse): string {
  return formatDart({ multiplier: dart.multiplier, segment: dart.segment });
}

export function formatSubmitThrow(dart: SubmitThrow): string {
  return formatDart({ multiplier: dart.multiplier, segment: dart.segment });
}

export function joinDarts(darts: DartLike[], separator = ', '): string {
  return darts.map(formatDart).join(separator);
}

export function totalForThrows(darts: DartLike[]): number {
  return darts.reduce((acc, dart) => acc + dartScore(dart), 0);
}

export function toDartLike(dart: SubmitThrow | ThrowResponse): DartLike {
  return { multiplier: dart.multiplier, segment: dart.segment };
}

export function formatThrowsFromResponses(throws: ThrowResponse[]): string {
  return joinDarts(throws.map(toDartLike));
}

export function formatThrowsFromSubmissions(throws: SubmitThrow[]): string {
  return joinDarts(throws.map(toDartLike));
}

export type CheckoutRoute = DartLike[];

export function formatCheckoutRoute(route: CheckoutRoute): string {
  return joinDarts(route, ' • ');
}
