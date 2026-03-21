import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Payee } from '../../../core/models/payee.model';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-payee-form-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './payee-form-dialog.html'
})
export class PayeeFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PayeeFormDialogComponent>);
  readonly data = inject<{ payee?: Payee; users: User[] }>(MAT_DIALOG_DATA);

  readonly form = new FormGroup({
    userId: new FormControl(this.data?.payee?.userId ?? '', [Validators.required]),
    name: new FormControl(this.data?.payee?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
