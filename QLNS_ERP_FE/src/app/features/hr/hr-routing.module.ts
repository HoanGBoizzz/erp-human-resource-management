import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { BangLuongComponent } from './pages/bang-luong/bang-luong.component';
import { ChamCongComponent } from './pages/cham-cong/cham-cong.component';
import { DonPhepComponent } from './pages/don-phep/don-phep.component';
import { DuAnComponent } from './pages/du-an/du-an.component';
import { TaiKhoanComponent } from './pages/tai-khoan/tai-khoan.component';
import { ThamSoComponent } from './pages/tham-so/tham-so.component';
import { NhanVienComponent } from './pages/nhan-vien/nhan-vien.component';
import { CanhBaoTaiKhoanComponent } from './pages/canh-bao-tai-khoan/canh-bao-tai-khoan.component';
import { PhongBanComponent } from './pages/phong-ban/phong-ban.component';
import { FaceRegistrationComponent } from 'src/app/features/hr/pages/face-registration/face-registration.component';
import { LuongCoBanComponent } from './pages/luong-co-ban/luong-co-ban.component';
import { PhuCapComponent } from './pages/phu-cap/phu-cap.component';
import { YeuCauNoiLamViecComponent } from './pages/yeu-cau-noi-lam-viec/yeu-cau-noi-lam-viec.component';
import { DeXuatGiamDocComponent } from './pages/de-xuat-giam-doc/de-xuat-giam-doc.component';
import { TinhLuongComponent } from './pages/tinh-luong/tinh-luong.component';
import { CauHinhLuongComponent } from './pages/cau-hinh-luong/cau-hinh-luong.component';

const routes: Routes = [
  { path: 'dashboard', component: DashboardComponent },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'bang-luong', component: BangLuongComponent },
  { path: 'cham-cong', component: ChamCongComponent },
  { path: 'don-phep', component: DonPhepComponent },
  { path: 'du-an', component: DuAnComponent },
  { path: 'nhan-vien', component: NhanVienComponent },
  { path: 'phong-ban', component: PhongBanComponent },
  { path: 'tai-khoan', component: TaiKhoanComponent },
  { path: 'canh-bao-tai-khoan', component: CanhBaoTaiKhoanComponent },
  { path: 'tham-so', component: ThamSoComponent },
  { path: 'face-registration', component: FaceRegistrationComponent },
  { path: 'luong-co-ban', component: LuongCoBanComponent },
  { path: 'tinh-luong', component: TinhLuongComponent },
  { path: 'cau-hinh-luong', component: CauHinhLuongComponent },
  { path: 'phu-cap', component: PhuCapComponent },
  { path: 'thuong-phat', redirectTo: 'phu-cap', pathMatch: 'full' },
  { path: 'yeu-cau-noi-lam-viec', component: YeuCauNoiLamViecComponent },
  { path: 'de-xuat-giam-doc', component: DeXuatGiamDocComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class HrRoutingModule { }
