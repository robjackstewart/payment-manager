import { NO_ERRORS_SCHEMA } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AppComponent } from './app';

describe('AppComponent', () => {
  beforeEach(() => {
    localStorage.clear();
    document.body.classList.remove('dark-theme');

    // jsdom does not implement matchMedia; stub it so AppComponent can instantiate.
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      configurable: true,
      value: vi.fn().mockReturnValue({
        matches: false,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      }),
    });

    TestBed.configureTestingModule({
      imports: [AppComponent],
      providers: [provideRouter([])],
      schemas: [NO_ERRORS_SCHEMA],
    });
  });

  afterEach(() => {
    localStorage.clear();
    document.body.classList.remove('dark-theme');
  });

  function setInnerWidth(width: number): void {
    Object.defineProperty(window, 'innerWidth', { writable: true, configurable: true, value: width });
  }

  function createComponent() {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('isMobile', () => {
    it('is true when innerWidth < 768', () => {
      setInnerWidth(500);
      const fixture = createComponent();
      expect(fixture.componentInstance.isMobile()).toBe(true);
    });

    it('is false when innerWidth >= 768', () => {
      setInnerWidth(1024);
      const fixture = createComponent();
      expect(fixture.componentInstance.isMobile()).toBe(false);
    });
  });

  describe('sidenavMode', () => {
    it('is "over" when isMobile is true', () => {
      setInnerWidth(500);
      const fixture = createComponent();
      expect(fixture.componentInstance.sidenavMode()).toBe('over');
    });

    it('is "side" when isMobile is false', () => {
      setInnerWidth(1024);
      const fixture = createComponent();
      expect(fixture.componentInstance.sidenavMode()).toBe('side');
    });
  });

  describe('sidenavOpened', () => {
    it('is false on mobile', () => {
      setInnerWidth(500);
      const fixture = createComponent();
      expect(fixture.componentInstance.sidenavOpened()).toBe(false);
    });

    it('is true on desktop', () => {
      setInnerWidth(1024);
      const fixture = createComponent();
      expect(fixture.componentInstance.sidenavOpened()).toBe(true);
    });
  });

  describe('isDarkMode', () => {
    it('is true when localStorage theme is "dark"', () => {
      localStorage.setItem('theme', 'dark');
      const fixture = createComponent();
      expect(fixture.componentInstance.isDarkMode()).toBe(true);
    });

    it('is false when localStorage theme is "light"', () => {
      localStorage.setItem('theme', 'light');
      const fixture = createComponent();
      expect(fixture.componentInstance.isDarkMode()).toBe(false);
    });

    it('falls back to system preference when no localStorage entry', () => {
      const mediaQueryMock = { matches: false, addEventListener: vi.fn(), removeEventListener: vi.fn() };
      vi.spyOn(window, 'matchMedia').mockReturnValue(mediaQueryMock as unknown as MediaQueryList);

      const fixture = createComponent();
      expect(fixture.componentInstance.isDarkMode()).toBe(false);

      vi.restoreAllMocks();
    });
  });

  describe('toggleDarkMode()', () => {
    it('flips isDarkMode from false to true', () => {
      localStorage.setItem('theme', 'light');
      const fixture = createComponent();
      const component = fixture.componentInstance;

      expect(component.isDarkMode()).toBe(false);
      component.toggleDarkMode();
      expect(component.isDarkMode()).toBe(true);
    });

    it('flips isDarkMode from true to false', () => {
      localStorage.setItem('theme', 'dark');
      const fixture = createComponent();
      const component = fixture.componentInstance;

      expect(component.isDarkMode()).toBe(true);
      component.toggleDarkMode();
      expect(component.isDarkMode()).toBe(false);
    });
  });

  describe('dark mode effect', () => {
    it('adds "dark-theme" class to body when isDarkMode is true', () => {
      localStorage.setItem('theme', 'dark');
      const fixture = createComponent();
      TestBed.flushEffects();

      expect(document.body.classList.contains('dark-theme')).toBe(true);
    });

    it('removes "dark-theme" class from body when isDarkMode is false', () => {
      document.body.classList.add('dark-theme');
      localStorage.setItem('theme', 'light');
      const fixture = createComponent();
      TestBed.flushEffects();

      expect(document.body.classList.contains('dark-theme')).toBe(false);
    });

    it('persists theme to localStorage via effect', () => {
      localStorage.setItem('theme', 'dark');
      const fixture = createComponent();
      TestBed.flushEffects();

      fixture.componentInstance.toggleDarkMode();
      TestBed.flushEffects();

      expect(localStorage.getItem('theme')).toBe('light');
    });
  });

  describe('pageTitle', () => {
    it('is initially empty string', () => {
      const fixture = createComponent();
      expect(fixture.componentInstance.pageTitle()).toBe('');
    });
  });
});
