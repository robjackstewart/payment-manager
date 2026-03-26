import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { AppComponent } from './app';

interface SetupOptions {
  innerWidth?: number;
  theme?: string;
  addDarkThemeToBody?: boolean;
  matchMedia?: { matches: boolean; addEventListener: ReturnType<typeof vi.fn>; removeEventListener: ReturnType<typeof vi.fn> };
}

function setup({ innerWidth = 1024, theme, addDarkThemeToBody = false, matchMedia: matchMediaResult }: SetupOptions = {}) {
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

  Object.defineProperty(window, 'innerWidth', { writable: true, configurable: true, value: innerWidth });

  TestBed.resetTestingModule();
  TestBed.configureTestingModule({
    imports: [AppComponent],
    providers: [provideRouter([])],
    schemas: [NO_ERRORS_SCHEMA],
  });

  const fixture = TestBed.createComponent(AppComponent);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance };
}

describe('AppComponent', () => {
  describe('isMobile', () => {
    it('is true when innerWidth < 768', () => {
      const { component } = setup({ innerWidth: 500 });
      expect(component.isMobile()).toBe(true);
    });

    it('is false when innerWidth >= 768', () => {
      const { component } = setup({ innerWidth: 1024 });
      expect(component.isMobile()).toBe(false);
    });
  });

  describe('sidenavMode', () => {
    it('is "over" when isMobile is true', () => {
      const { component } = setup({ innerWidth: 500 });
      expect(component.sidenavMode()).toBe('over');
    });

    it('is "side" when isMobile is false', () => {
      const { component } = setup();
      expect(component.sidenavMode()).toBe('side');
    });
  });

  describe('sidenavOpen', () => {
    it('is false on mobile', () => {
      const { component } = setup({ innerWidth: 500 });
      expect(component.sidenavOpen()).toBe(false);
    });

    it('is true on desktop', () => {
      const { component } = setup();
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
      const { component } = setup({ innerWidth: 500 });

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
