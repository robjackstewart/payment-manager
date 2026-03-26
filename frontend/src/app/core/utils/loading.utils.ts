export const LOADING = Symbol('LOADING');
export type Loading = typeof LOADING;
export type LoadingState<T> = Loading | T;

export function isLoaded<T>(value: LoadingState<T>): value is T {
  return value !== LOADING;
}
