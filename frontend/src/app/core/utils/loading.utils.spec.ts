import { describe, it, expect } from 'vitest';
import { LOADING, isLoaded } from './loading.utils';

describe('isLoaded', () => {
  it('returns false for LOADING', () => {
    expect(isLoaded(LOADING)).toBe(false);
  });

  it('returns true for an empty array', () => {
    expect(isLoaded([])).toBe(true);
  });

  it('returns true for null', () => {
    expect(isLoaded(null)).toBe(true);
  });

  it('returns true for 0', () => {
    expect(isLoaded(0)).toBe(true);
  });

  it('returns true for an empty string', () => {
    expect(isLoaded('')).toBe(true);
  });

  it('returns true for an object', () => {
    expect(isLoaded({ some: 'object' })).toBe(true);
  });
});

describe('LOADING symbol', () => {
  it('is a unique symbol and not equal to another Symbol("LOADING")', () => {
    expect(LOADING).not.toBe(Symbol('LOADING'));
  });
});

describe('isLoaded type narrowing', () => {
  it('narrows the type to T inside the true branch', () => {
    const val: typeof LOADING | { name: string } = { name: 'test' };
    if (isLoaded(val)) {
      expect(val.name).toBe('test');
    } else {
      throw new Error('Expected isLoaded to return true');
    }
  });
});
