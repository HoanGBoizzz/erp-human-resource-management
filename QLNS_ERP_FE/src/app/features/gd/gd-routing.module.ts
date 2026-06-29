import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { DuyetBangLuongComponent } from './pages/duyet-bang-luong/duyet-bang-luong.component';
import { BaoCaoComponent } from './pages/bao-cao/bao-cao.component';
import { DuyetDeXuatComponent } from './pages/duyet-de-xuat/duyet-de-xuat.component';
import { DuyetDuAnComponent } from './pages/duyet-du-an/duyet-du-an.component';
import { AuditLogComponent } from './pages/audit-log/audit-log.component';

const routes: Routes = [
  { path: 'dashboard', component: DashboardComponent },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {path:'duyet-bang-luong',component:DuyetBangLuongComponent},
  {path:'bao-cao',component:BaoCaoComponent},
  {path:'duyet-de-xuat',component:DuyetDeXuatComponent},
  {path:'duyet-du-an',component:DuyetDuAnComponent},
  {path:'audit-log',component:AuditLogComponent}
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class GdRoutingModule { }
