import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatButton } from '@angular/material/button';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButton],
  templateUrl: './confirm-dialog.html'
})
export class ConfirmDialogComponent {
  readonly data = inject<{ title: string; message: string }>(MAT_DIALOG_DATA);
}
