import { Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-material-demo',
  templateUrl: './material-demo.component.html',
  styleUrls: ['./material-demo.component.scss']
})
export class MaterialDemoComponent {
  
  // Sample data for table
  displayedColumns: string[] = ['id', 'name', 'department', 'actions'];
  sampleData = [
    { id: 1, name: 'Nguyễn Văn A', department: 'IT' },
    { id: 2, name: 'Trần Thị B', department: 'HR' },
    { id: 3, name: 'Lê Văn C', department: 'Sales' },
    { id: 4, name: 'Phạm Thị D', department: 'Marketing' },
  ];

  constructor(
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  openDialog(): void {
    this.dialog.open(SampleDialogComponent, {
      width: '400px',
      data: { message: 'Đây là Material Dialog component!' }
    });
  }

  openSnackbar(): void {
    this.snackBar.open('Snackbar notification thành công!', 'Đóng', {
      duration: 3000,
      horizontalPosition: 'center',
      verticalPosition: 'top',
    });
  }
}

// Sample Dialog Component
@Component({
  selector: 'app-sample-dialog',
  template: `
    <h2 mat-dialog-title>Material Dialog</h2>
    <mat-dialog-content>
      <p>{{ data.message }}</p>
      <p>Dialog component có thể dùng để hiển thị form, confirmation, hoặc bất kỳ nội dung nào.</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Hủy</button>
      <button mat-raised-button color="primary" [mat-dialog-close]="true">Đồng ý</button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content {
      padding: 1.5rem 0;
    }
  `]
})
export class SampleDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: any) {}
}

import { Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
