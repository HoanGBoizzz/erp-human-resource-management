import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NgxChartsModule } from '@swimlane/ngx-charts';

import { GdRoutingModule } from './gd-routing.module';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DuyetBangLuongComponent } from './pages/duyet-bang-luong/duyet-bang-luong.component';
import { DuyetDeXuatComponent } from './pages/duyet-de-xuat/duyet-de-xuat.component';
import { DuyetDuAnComponent } from './pages/duyet-du-an/duyet-du-an.component';
import { BaoCaoComponent } from './pages/bao-cao/bao-cao.component';
import { AuditLogComponent } from './pages/audit-log/audit-log.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from 'src/app/shared/shared.module';


@NgModule({
  declarations: [
    DashboardComponent,
    DuyetBangLuongComponent,
    DuyetDeXuatComponent,
    DuyetDuAnComponent,
    BaoCaoComponent,
    AuditLogComponent
  ],
  imports: [
    CommonModule,
    GdRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule,
    NgxChartsModule
  ]
})
export class GdModule { }
