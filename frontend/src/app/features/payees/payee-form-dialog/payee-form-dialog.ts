import { Component, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { FormField, FormRoot, form, required } from '@angular/forms/signals';
import { Payee } from '../../../core/models/payee.model';

interface PayeeFormModel {
  name: string;
}

@Component({
  selector: 'app-payee-form-dialog',
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
    FormRoot,
    FormField,
  ],
  templateUrl: './payee-form-dialog.html'
})
export class PayeeFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PayeeFormDialogComponent>);
  readonly data = inject<{ payee?: Payee }>(MAT_DIALOG_DATA);

  readonly title = this.data?.payee ? 'Edit Payee' : 'New Payee';
  readonly submitLabel = this.data?.payee ? 'Save' : 'Create';

  readonly model = signal<PayeeFormModel>({
    name: this.data?.payee?.name ?? '',
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
