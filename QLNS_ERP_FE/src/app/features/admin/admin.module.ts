import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AdminRoutingModule } from './admin-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { AuditLogListComponent } from './pages/audit-log-list/audit-log-list.component';
import { AuditLogDetailComponent } from './pages/audit-log-detail/audit-log-detail.component';
import { MaterialDemoComponent, SampleDialogComponent } from './pages/material-demo/material-demo.component';

@NgModule({
    declarations: [
        AuditLogListComponent,
        AuditLogDetailComponent,
        MaterialDemoComponent,
        SampleDialogComponent
    ],
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        AdminRoutingModule,
        SharedModule
    ]
})
export class AdminModule { }
