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
import { PayerGroupService } from '../../../core/services/payer-group.service';
import { PersonService } from '../../../core/services/person.service';
import { BreakpointService } from '../../../core/services/breakpoint.service';
import { PayerGroup } from '../../../core/models/payer-group.model';
import { LOADING, LoadingState, isLoaded } from '../../../core/utils/loading.utils';

interface PayerGroupViewModel {
  memberNames: string;
  _raw: PayerGroup;
}

@Component({
  selector: 'app-payer-group-list',
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
  templateUrl: './payer-group-list.html',
  styleUrl: './payer-group-list.scss'
})
export class PayerGroupListComponent {
  private readonly payerGroupService = inject(PayerGroupService);
  private readonly personService = inject(PersonService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  readonly breakpointService = inject(BreakpointService);

  private readonly reloadTrigger = signal(0);

  private readonly allDataResource = rxResource({
    params: () => this.reloadTrigger(),
    stream: () => forkJoin({
      payerGroups: this.payerGroupService.getAll(),
      people: this.personService.getAll(),
    })
  });

  private readonly allData = computed(() => this.allDataResource.value());

  readonly payerGroups = computed<LoadingState<PayerGroup[]>>(() =>
    this.allDataResource.isLoading() ? LOADING : (this.allData()?.payerGroups ?? [])
  );
  readonly isLoading = computed(() => this.allDataResource.isLoading());
  readonly people = computed(() => this.allData()?.people ?? []);

  private readonly peopleMap = computed(() => {
    const map: Record<string, string> = {};
    for (const p of this.people()) map[p.id] = p.name;
    return map;
  });

  readonly payerGroupsForTable = computed<PayerGroupViewModel[]>(() => {
    if (!isLoaded(this.payerGroups())) return [];
    return (this.payerGroups() as PayerGroup[]).map(g => ({
      memberNames: g.memberPersonIds.length > 0
        ? g.memberPersonIds.map(id => this.peopleMap()[id] ?? id).join(', ')
        : 'None',
      _raw: g,
    }));
  });

  readonly displayedColumns = ['name', 'members', 'actions'];

  private reload(): void { this.reloadTrigger.update(n => n + 1); }

  async openCreateDialog(): Promise<void> {
    const { PayerGroupFormDialogComponent } = await import('../payer-group-form-dialog/payer-group-form-dialog');
    const ref = this.dialog.open(PayerGroupFormDialogComponent, { width: '450px', data: { people: this.people() } });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      const created = await firstValueFrom(this.payerGroupService.create({ name: result.name }));
      if (result.personIds?.length > 0) {
        await firstValueFrom(this.payerGroupService.setMembers(created.id, { personIds: result.personIds }));
      }
      this.snackBar.open('Payer group created', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to create payer group', 'Close', { duration: 3000 });
    }
  }

  async openEditDialog(payerGroup: PayerGroup): Promise<void> {
    const { PayerGroupFormDialogComponent } = await import('../payer-group-form-dialog/payer-group-form-dialog');
    const ref = this.dialog.open(PayerGroupFormDialogComponent, {
      width: '450px',
      data: { payerGroup, people: this.people() }
    });
    const result = await firstValueFrom(ref.afterClosed());
    if (!result) return;
    try {
      await firstValueFrom(this.payerGroupService.update(payerGroup.id, { name: result.name }));
      await firstValueFrom(this.payerGroupService.setMembers(payerGroup.id, { personIds: result.personIds ?? [] }));
      this.snackBar.open('Payer group updated', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to update payer group', 'Close', { duration: 3000 });
    }
  }

  async deletePayerGroup(payerGroup: PayerGroup): Promise<void> {
    const { ConfirmDialogComponent } = await import('../../../shared/confirm-dialog/confirm-dialog');
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Payer Group', message: `Are you sure you want to delete "${payerGroup.name}"?` }
    });
    const confirmed = await firstValueFrom(ref.afterClosed());
    if (!confirmed) return;
    try {
      await firstValueFrom(this.payerGroupService.delete(payerGroup.id));
      this.snackBar.open('Payer group deleted', 'Close', { duration: 2000 });
      this.reload();
    } catch {
      this.snackBar.open('Failed to delete payer group', 'Close', { duration: 3000 });
    }
  }
}
