import { Component, computed, effect, inject, signal, ViewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet, Router, NavigationEnd, ActivatedRouteSnapshot } from '@angular/router';
import { MatSidenavContainer, MatSidenav, MatSidenavContent } from '@angular/material/sidenav';
import { MatToolbar } from '@angular/material/toolbar';
import { MatNavList, MatListItem, MatListItemIcon, MatListItemTitle } from '@angular/material/list';
import { MatIcon } from '@angular/material/icon';
import { MatIconButton } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatSlideToggle } from '@angular/material/slide-toggle';
import { MatTooltip } from '@angular/material/tooltip';
import { filter } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavContainer,
    MatSidenav,
    MatSidenavContent,
    MatToolbar,
    MatNavList,
    MatListItem,
    MatListItemIcon,
    MatListItemTitle,
    MatIcon,
    MatIconButton,
    MatDivider,
    MatSlideToggle,
    MatTooltip,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class AppComponent {
  @ViewChild(MatSidenav, { static: false }) private readonly sidenav!: MatSidenav;

  private readonly router = inject(Router);
  readonly isMobile = signal(window.innerWidth < 768);
  readonly sidenavMode = computed(() => this.isMobile() ? 'over' : 'side');
  readonly sidenavOpened = computed(() => !this.isMobile());

  readonly isDarkMode = signal(
    localStorage.getItem('theme') === 'dark' ||
    (!localStorage.getItem('theme') && window.matchMedia('(prefers-color-scheme: dark)').matches)
  );

  readonly pageTitle = signal('');
  private readonly navigationEnd = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd)
    ),
    { initialValue: null }
  );

  constructor() {
    window.addEventListener('resize', () => this.isMobile.set(window.innerWidth < 768));

    effect(() => {
      document.body.classList.toggle('dark-theme', this.isDarkMode());
      localStorage.setItem('theme', this.isDarkMode() ? 'dark' : 'light');
    });

    effect(() => {
      const event = this.navigationEnd();
      if (!event) return;
      this.pageTitle.set(this.getActiveTitle());
      if (this.isMobile()) this.sidenav.close();
    });
  }

  private getActiveTitle(): string {
    let snapshot: ActivatedRouteSnapshot = this.router.routerState.snapshot.root;
    while (snapshot.firstChild) snapshot = snapshot.firstChild;
    return snapshot.title ?? '';
  }

  toggleDarkMode(): void {
    this.isDarkMode.update(v => !v);
  }
}

export { AppComponent as App };
export default AppComponent;

