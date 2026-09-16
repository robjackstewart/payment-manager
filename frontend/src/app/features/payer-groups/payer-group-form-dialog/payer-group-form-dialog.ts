import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatDialogRef } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';
import { MatFormField, MatLabel, MatError } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { MatOption } from '@angular/material/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PayerGroup } from '../../../core/models/payer-group.model';
import { Person } from '../../../core/models/person.model';

@Component({
  selector: 'app-payer-group-form-dialog',
  standalone: true,
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
    ReactiveFormsModule,
  ],
  templateUrl: './payer-group-form-dialog.html'
})
export class PayerGroupFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PayerGroupFormDialogComponent>);
  readonly data = inject<{ payerGroup?: PayerGroup; people?: Person[] }>(MAT_DIALOG_DATA);

  readonly title = this.data?.payerGroup ? 'Edit Payer Group' : 'New Payer Group';
  readonly submitLabel = this.data?.payerGroup ? 'Save' : 'Create';
  readonly people = this.data?.people ?? [];

  readonly form = new FormGroup({
    name: new FormControl(this.data?.payerGroup?.name ?? '', [Validators.required]),
    personIds: new FormControl<string[]>(this.data?.payerGroup?.memberPersonIds ?? []),
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
