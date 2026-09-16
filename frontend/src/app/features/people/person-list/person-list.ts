import { Component, computed, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { firstValueFrom, forkJoin } from 'rxjs';
import { MatTable, MatColumnDef, MatHeaderCell, MatHeaderCellDef, MatCell, MatCellDef, MatHeaderRow, MatHeaderRowDef, MatRow, MatRowDef } from '@angular/material/table';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { PersonService } from '../../../core/services/person.service';
import { PayerGroupService } from '../../../core/services/payer-group.service';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { Person } from '../../../core/models/person.model';
import { LOADING, LoadingState, isLoaded } from '../../../core/utils/loading.utils';

interface PersonViewModel {
  groupNames: string;
  _raw: Person;
}

@Component({
  selector: 'app-person-list',
  standalone: true,
  imports: [
    MatTable,
    MatColumnDef,
    MatHeaderCell,
    MatHeaderCellDef,
    MatCell,
    MatCellDef,
    MatHeaderRow,
    MatHeaderRowDef,
    MatRow,
    MatRowDef,
    MatButton,
    MatIconButton,
    MatIcon,
    MatCard,
    MatCardContent,
    MatProgressSpinner,
    MatTooltip,
  ],
  templateUrl: './person-list.html',
  styleUrl: './person-list.scss'
})
export class PersonListComponent {
  private readonly personService = inject(PersonService);
  private readonly payerGroupService = inject(PayerGroupService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly breakpointService = inject(BreakpointService);

  private readonly reloadTrigger = signal(0);

  private readonly allDataResource = rxResource({
    params: () => this.reloadTrigger(),
    stream: () => forkJoin({
      people: this.personService.getAll(),
      payerGroups: this.payerGroupService.getAll(),
    })
  });

  readonly isLoading = computed(() => this.allDataResource.isLoading());

  private readonly allData = computed(() => this.allDataResource.value());

  readonly people = computed<LoadingState<Person[]>>(() =>
    this.allDataResource.isLoading() ? LOADING : (this.allData()?.people ?? [])
  );
  readonly payerGroups = computed(() => this.allData()?.payerGroups ?? []);

  readonly peopleForTable = computed<PersonViewModel[]>(() => {
    if (!isLoaded(this.people())) return [];
    const groups = this.payerGroups();
    return (this.people() as Person[]).map(p => {
      const names = groups.filter(g => g.memberPersonIds.includes(p.id)).map(g => g.name);
      return { groupNames: names.length > 0 ? names.join(', ') : 'None', _raw: p };
    });
  });

  readonly displayedColumns = ['name', 'groups', 'actions'];

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const { PersonFormDialogComponent } = await import('../person-form-dialog/person-form-dialog');
    const ref = this.dialog.open(PersonFormDialogComponent, { width: '450px' });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.personService.create(result));
      this.snackBar.open('Person created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to create person', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(person: Person): Promise<void> {
    const { PersonFormDialogComponent } = await import('../person-form-dialog/person-form-dialog');
    const ref = this.dialog.open(PersonFormDialogComponent, {
      width: '450px',
      data: { person }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.personService.update(person.id, result));
      this.snackBar.open('Person updated', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to update person', 'Close', { duration: 3000 });
    }
  }

  async deletePerson(person: Person): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Person', message: `Are you sure you want to delete "${person.name}"?` }
    });
    const confirmed = await firstValueFrom(ref.afterClosed());
    if (!confirmed) return;
    try {
      await firstValueFrom(this.personService.delete(person.id));
      this.snackBar.open('Person deleted', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to delete person', 'Close', { duration: 3000 });
    }
  }
}
