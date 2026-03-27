import { NO_ERRORS_SCHEMA } from '@angular/core';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { AppComponent } from './app';
import { BreakpointService } from './core/services/breakpoint.service';

interface SetupOptions {
  isMobile?: boolean;
  theme?: string;
  addDarkThemeToBody?: boolean;
  matchMedia?: { matches: boolean; addEventListener: ReturnType<typeof vi.fn>; removeEventListener: ReturnType<typeof vi.fn> };
}

function setup({ isMobile = false, theme, addDarkThemeToBody = false, matchMedia: matchMediaResult }: SetupOptions = {}) {
  localStorage.clear();
  document.body.classList.remove('dark-theme');
  if (theme !== undefined) localStorage.setItem('theme', theme);
  if (addDarkThemeToBody) document.body.classList.add('dark-theme');

  // jsdom does not implement matchMedia; stub it so AppComponent can instantiate.
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: vi.fn().mockReturnValue(
      matchMediaResult ?? { matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() },
    ),
  });

  const isMobileSignal = signal(isMobile);
  const breakpointService = { isMobile: isMobileSignal };

  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [AppComponent],
    providers: [
      provideRouter([]),
      { provide: BreakpointService, useValue: breakpointService },
    ],
    schemas: [NO_ERRORS_SCHEMA],
  });

  const fixture = TestBed.createComponent(AppComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, isMobileSignal };
}

describe('AppComponent', () => {
  describe('isMobile', () => {
    it('is true when BreakpointService reports mobile', () => {
      const { component } = setup({ isMobile: true });
      expect(component.isMobile()).toBe(true);
    });

    it('is false when BreakpointService reports desktop', () => {
      const { component } = setup({ isMobile: false });
      expect(component.isMobile()).toBe(false);
    });
  });

  describe('sidenavMode', () => {
    it('is "over" when isMobile is true', () => {
      const { component } = setup({ isMobile: true });
      expect(component.sidenavMode()).toBe('over');
    });

    it('is "side" when isMobile is false', () => {
      const { component } = setup();
      expect(component.sidenavMode()).toBe('side');
    });
  });

  describe('sidenavOpen', () => {
    it('is false on mobile', () => {
      const { component } = setup({ isMobile: true });
      expect(component.sidenavOpen()).toBe(false);
    });

    it('is true on desktop', () => {
      const { component } = setup();
      expect(component.sidenavOpen()).toBe(true);
    });

    it('closes when breakpoint changes from desktop to mobile', () => {
      const { component, isMobileSignal } = setup({ isMobile: false });
      expect(component.sidenavOpen()).toBe(true);

      isMobileSignal.set(true);
      TestBed.tick();

      expect(component.sidenavOpen()).toBe(false);
    });

    it('opens when breakpoint changes from mobile to desktop', () => {
      const { component, isMobileSignal } = setup({ isMobile: true });
      expect(component.sidenavOpen()).toBe(false);

      isMobileSignal.set(false);
      TestBed.tick();

      expect(component.sidenavOpen()).toBe(true);
    });
  });

  describe('isDarkMode', () => {
    it('is true when localStorage theme is "dark"', () => {
      const { component } = setup({ theme: 'dark' });
      expect(component.isDarkMode()).toBe(true);
    });

    it('is false when localStorage theme is "light"', () => {
      const { component } = setup({ theme: 'light' });
      expect(component.isDarkMode()).toBe(false);
    });

    it('falls back to system preference when no localStorage entry', () => {
      const { component } = setup({ matchMedia: { matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() } });
      expect(component.isDarkMode()).toBe(false);
    });
  });

  describe('toggleSidenav()', () => {
    it('opens the sidenav when it is closed', () => {
      const { component } = setup({ isMobile: true });

      expect(component.sidenavOpen()).toBe(false);
      component.toggleSidenav();
      expect(component.sidenavOpen()).toBe(true);
    });

    it('closes the sidenav when it is open', () => {
      const { component } = setup();

      expect(component.sidenavOpen()).toBe(true);
      component.toggleSidenav();
      expect(component.sidenavOpen()).toBe(false);
    });
  });

  describe('toggleDarkMode()', () => {
    it('flips isDarkMode from false to true', () => {
      const { component } = setup({ theme: 'light' });

      expect(component.isDarkMode()).toBe(false);
      component.toggleDarkMode();
      expect(component.isDarkMode()).toBe(true);
    });

    it('flips isDarkMode from true to false', () => {
      const { component } = setup({ theme: 'dark' });

      expect(component.isDarkMode()).toBe(true);
      component.toggleDarkMode();
      expect(component.isDarkMode()).toBe(false);
    });
  });

  describe('dark mode effect', () => {
    it('adds "dark-theme" class to body when isDarkMode is true', () => {
      setup({ theme: 'dark' });
      TestBed.tick();

      expect(document.body.classList.contains('dark-theme')).toBe(true);
    });

    it('removes "dark-theme" class from body when isDarkMode is false', () => {
      setup({ addDarkThemeToBody: true, theme: 'light' });
      TestBed.tick();

      expect(document.body.classList.contains('dark-theme')).toBe(false);
    });

    it('persists theme to localStorage via effect', () => {
      const { fixture } = setup({ theme: 'dark' });
      TestBed.tick();

      fixture.componentInstance.toggleDarkMode();
      TestBed.tick();

      expect(localStorage.getItem('theme')).toBe('light');
    });
  });

  describe('pageTitle', () => {
    it('is initially empty string', () => {
      const { component } = setup();
      expect(component.pageTitle()).toBe('');
    });
  });
});
