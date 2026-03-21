import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { User } from '../../../core/models/user.model';

@Component({
  selector: 'app-user-form-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './user-form-dialog.html'
})
export class UserFormDialogComponent {
  readonly dialogRef = inject(MatDialogRef<UserFormDialogComponent>);
  readonly data = inject<{ user?: User } | null>(MAT_DIALOG_DATA);

  readonly form = new FormGroup({
    name: new FormControl(this.data?.user?.name ?? '', [Validators.required])
  });

  submit(): void {
    if (this.form.valid) {
      this.dialogRef.close(this.form.value);
    }
  }
}
