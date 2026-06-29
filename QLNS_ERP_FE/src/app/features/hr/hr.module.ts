import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { HrRoutingModule } from './hr-routing.module';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { NhanVienComponent } from './pages/nhan-vien/nhan-vien.component';
import { ChamCongComponent } from './pages/cham-cong/cham-cong.component';
import { DonPhepComponent } from './pages/don-phep/don-phep.component';
import { BangLuongComponent } from './pages/bang-luong/bang-luong.component';
import { DuAnComponent } from './pages/du-an/du-an.component';
import { TaiKhoanComponent } from './pages/tai-khoan/tai-khoan.component';
import { ThamSoComponent } from './pages/tham-so/tham-so.component';
import { CanhBaoTaiKhoanComponent } from './pages/canh-bao-tai-khoan/canh-bao-tai-khoan.component';
import { PhongBanComponent } from './pages/phong-ban/phong-ban.component';
import { FaceRegistrationComponent } from 'src/app/features/hr/pages/face-registration/face-registration.component';
import { LuongCoBanComponent } from './pages/luong-co-ban/luong-co-ban.component';
import { PhuCapComponent } from './pages/phu-cap/phu-cap.component';
import { YeuCauNoiLamViecComponent } from './pages/yeu-cau-noi-lam-viec/yeu-cau-noi-lam-viec.component';
import { SharedModule } from 'src/app/shared/shared.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DeXuatGiamDocComponent } from './pages/de-xuat-giam-doc/de-xuat-giam-doc.component';
import { TinhLuongComponent } from './pages/tinh-luong/tinh-luong.component';
import { CauHinhLuongComponent } from './pages/cau-hinh-luong/cau-hinh-luong.component';


@NgModule({
  declarations: [
    DashboardComponent,
    NhanVienComponent,
    ChamCongComponent,
    DonPhepComponent,
    BangLuongComponent,
    DuAnComponent,
    TaiKhoanComponent,
    ThamSoComponent,
    CanhBaoTaiKhoanComponent,
    PhongBanComponent,
    FaceRegistrationComponent,
    LuongCoBanComponent,
    PhuCapComponent,
    YeuCauNoiLamViecComponent,
    DeXuatGiamDocComponent,
    TinhLuongComponent,
    CauHinhLuongComponent,
  ],
  imports: [
    CommonModule,
    HrRoutingModule,
    SharedModule,
    FormsModule,
    ReactiveFormsModule
  ]
})
export class HrModule { }