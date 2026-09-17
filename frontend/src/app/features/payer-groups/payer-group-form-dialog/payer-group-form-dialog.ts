import { Component, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { MatOption } from '@angular/material/core';
import { FormField, FormRoot, form, required } from '@angular/forms/signals';
import { PayerGroup } from '../../../core/models/payer-group.model';
import { Person } from '../../../core/models/person.model';

interface PayerGroupFormModel {
  name: string;
  personIds: string[];
}

@Component({
  selector: 'app-payer-group-form-dialog',
  imports: [
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatDialogClose,
    MatButton,
    MatFormField,
    MatLabel,
    MatError,
    MatInput,
    MatSelect,
    MatOption,
    FormRoot,
    FormField,
  ],
  templateUrl: './payer-group-form-dialog.html'
})
export class PayerGroupFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PayerGroupFormDialogComponent>);
  readonly data = inject<{ payerGroup?: PayerGroup; people?: Person[] }>(MAT_DIALOG_DATA);

  readonly title = this.data?.payerGroup ? 'Edit Payer Group' : 'New Payer Group';
  readonly submitLabel = this.data?.payerGroup ? 'Save' : 'Create';
  readonly people = this.data?.people ?? [];

  readonly model = signal<PayerGroupFormModel>({
    name: this.data?.payerGroup?.name ?? '',
    personIds: this.data?.payerGroup?.memberPersonIds ?? [],
  });

  readonly form = form(this.model, (path) => {
    required(path.name, { message: 'Name is required' });
  });

  readonly nameError = computed(() => this.form.name().errors()[0]?.message ?? '');
  readonly submitDisabled = computed(() => this.form().invalid());

  submit(): void {
    if (this.form().invalid()) return;
    this.dialogRef.close(this.model());
  }
}
