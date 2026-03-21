import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { UserService } from './core/services/user.service';
import { UserContextService } from './core/services/user-context.service';
import { User } from './core/models/user.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatSelectModule,
    MatFormFieldModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class AppComponent implements OnInit {
  private readonly userService = inject(UserService);
  readonly userContext = inject(UserContextService);
  readonly isMobile = signal(window.innerWidth < 768);
  readonly users = signal<User[]>([]);

  constructor() {
    window.addEventListener('resize', () => this.isMobile.set(window.innerWidth < 768));
  }

  ngOnInit(): void {
    this.userService.getAll().subscribe(users => this.users.set(users));
  }

  compareUsers(a: User | null, b: User | null): boolean {
    return a?.id === b?.id;
  }
}

export { AppComponent as App };
export default AppComponent;

