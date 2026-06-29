import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterOutlet } from '@angular/router';
import { MaterialModule } from './material.module';

import { SidebarComponent } from './components/sidebar/sidebar.component';
import { TopbarComponent } from './components/topbar/topbar.component';
import { StatCardComponent } from './components/stat-card/stat-card.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';
import { TableComponent } from './components/table/table.component';
import { BadgeComponent } from './components/badge/badge.component';
import { ToastComponent } from './components/toast/toast.component';
import { RouteLoadingComponent } from './components/route-loading/route-loading.component';
import { ProfileSidebarComponent } from './components/profile-sidebar/profile-sidebar.component';
import { HasPermissionDirective } from './directives/has-permission.directive';
import { CurrencyVndPipe } from './pipes/currency-vnd.pipe';
import { DateVnPipe } from './pipes/date-vn.pipe';
import { StatusLabelPipe } from './pipes/status-label.pipe';



@NgModule({
  declarations: [
    SidebarComponent,
    TopbarComponent,
    StatCardComponent,
    ConfirmDialogComponent,
    TableComponent,
    BadgeComponent,
    ToastComponent,
    RouteLoadingComponent,
    ProfileSidebarComponent,
    HasPermissionDirective,
    CurrencyVndPipe,
    DateVnPipe,
    StatusLabelPipe
  ],
  imports: [
    CommonModule,
    RouterOutlet,
    ReactiveFormsModule,
    MaterialModule
  ],
  exports: [
    CommonModule,
    MaterialModule,
    SidebarComponent,
    TopbarComponent,
    StatCardComponent,
    ConfirmDialogComponent,
    TableComponent,
    BadgeComponent,
    ToastComponent,
    RouteLoadingComponent,
    ProfileSidebarComponent,
    HasPermissionDirective,
    CurrencyVndPipe,
    DateVnPipe,
    StatusLabelPipe
  ]
})
export class SharedModule { }
