import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { EmployeeRoutingModule } from './employee-routing.module';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { BangCongComponent } from './pages/bang-cong/bang-cong.component';
import { DonPhepComponent } from './pages/don-phep/don-phep.component';
import { BangLuongComponent } from './pages/bang-luong/bang-luong.component';
import { DuAnComponent } from './pages/du-an/du-an.component';
import { HoSoComponent } from './pages/ho-so/ho-so.component';
import { EmployeeFaceRegistrationComponent } from './pages/employee-face-registration/employee-face-registration.component';
import { PhieuDeXuatComponent } from './pages/phieu-de-xuat/phieu-de-xuat.component';
import { PhieuTamUngComponent } from './pages/phieu-tam-ung/phieu-tam-ung.component';
import { DonDiMuonComponent } from './pages/don-di-muon/don-di-muon.component';
import { NoiLamViecThongKeComponent } from './pages/noi-lam-viec-thong-ke/noi-lam-viec-thong-ke.component';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from 'src/app/shared/shared.module';


@NgModule({
  declarations: [
    DashboardComponent,
    BangCongComponent,
    DonPhepComponent,
    BangLuongComponent,
    DuAnComponent,
    HoSoComponent,
    EmployeeFaceRegistrationComponent,
    PhieuDeXuatComponent,
    PhieuTamUngComponent,
    DonDiMuonComponent,
    NoiLamViecThongKeComponent,
  ],
  imports: [
    CommonModule,
    EmployeeRoutingModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule
  ]
})
export class EmployeeModule { }

