import { TestBed } from '@angular/core/testing';
import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { describe, expect, it, vi } from 'vitest';
import { Subject } from 'rxjs';
import { BreakpointService } from './breakpoint.service';

function setup() {
  TestBed.resetTestingModule();

  const state$ = new Subject<BreakpointState>();
  const breakpointObserver = {
    observe: vi.fn().mockReturnValue(state$.asObservable()),
  };

  TestBed.configureTestingModule({
    providers: [
      BreakpointService,
      { provide: BreakpointObserver, useValue: breakpointObserver },
    ],
  });

  const service = TestBed.inject(BreakpointService);
  return { service, state$, breakpointObserver };
}

describe('BreakpointService', () => {
  it('isMobile defaults to false before any emission', () => {
    const { service } = setup();
    expect(service.isMobile()).toBe(false);
  });

  it('isMobile becomes true when observer emits a matching state', () => {
    const { service, state$ } = setup();
    TestBed.runInInjectionContext(() => {
      state$.next({ matches: true, breakpoints: {} });
    });
    expect(service.isMobile()).toBe(true);
  });

  it('isMobile becomes false when observer emits a non-matching state', () => {
    const { service, state$ } = setup();
    TestBed.runInInjectionContext(() => {
      state$.next({ matches: true, breakpoints: {} });
      state$.next({ matches: false, breakpoints: {} });
    });
    expect(service.isMobile()).toBe(false);
  });

  it('observes the 767px max-width breakpoint', () => {
    const { breakpointObserver } = setup();
    expect(breakpointObserver.observe).toHaveBeenCalledWith('(max-width: 767px)');
  });
});
