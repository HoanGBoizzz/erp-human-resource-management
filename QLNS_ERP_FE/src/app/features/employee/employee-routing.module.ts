import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { BangCongComponent } from './pages/bang-cong/bang-cong.component';
import { DonPhepComponent } from './pages/don-phep/don-phep.component';
import { BangLuongComponent } from './pages/bang-luong/bang-luong.component';
import { DuAnComponent } from './pages/du-an/du-an.component';
import { EmployeeFaceRegistrationComponent } from './pages/employee-face-registration/employee-face-registration.component';
import { PhieuDeXuatComponent } from './pages/phieu-de-xuat/phieu-de-xuat.component';
import { PhieuTamUngComponent } from './pages/phieu-tam-ung/phieu-tam-ung.component';
import { DonDiMuonComponent } from './pages/don-di-muon/don-di-muon.component';
import { NoiLamViecThongKeComponent } from './pages/noi-lam-viec-thong-ke/noi-lam-viec-thong-ke.component';

const routes: Routes = [
  { path: 'dashboard', component: DashboardComponent },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'cham-cong', component: BangCongComponent },
  { path: 'face-registration', component: EmployeeFaceRegistrationComponent },
  { path: 'nghi-phep', component: DonPhepComponent },
  { path: 'luong', component: BangLuongComponent },
  { path: 'du-an', component: DuAnComponent },
  // Nơi làm việc
  { path: 'noi-lam-viec-thong-ke', component: NoiLamViecThongKeComponent },
  { path: 'phieu-de-xuat', component: PhieuDeXuatComponent },
  { path: 'phieu-tam-ung', component: PhieuTamUngComponent },
  { path: 'don-di-muon', component: DonDiMuonComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class EmployeeRoutingModule { }

