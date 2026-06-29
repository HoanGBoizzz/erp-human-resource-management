import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

// ─── DTOs ─────────────────────────────────────────────────────────────────────

export interface CreatePhieuDeXuatDto {
    tenDungCu: string;
    donViTinh: string;
    soLuong: number;
    giaTien: number;
    lyDo: string;
}

export interface UpdatePhieuDeXuatDto {
    tenDungCu: string;
    donViTinh: string;
    soLuong: number;
    giaTien: number;
    lyDo: string;
}

export interface PhieuDeXuatListItem {
    id: number;
    nvHoSoId: number;
    maNhanVien: string;
    hoTenNhanVien: string;
    tenDungCu: string;
    donViTinh: string;
    soLuong: number;
    giaTien: number;
    tongTien: number;
    lyDo: string;
    trangThai: string;
    lyDoTuChoi: string | null;
    hoTenNguoiDuyet: string | null;
    ngayDuyet: string | null;
    createdAt: string;
}

export interface CreatePhieuTamUngDto {
    mucDich: string;
    soTien: number;
    ngayCanTamUng: string;
    lyDo: string;
}

export interface UpdatePhieuTamUngDto {
    mucDich: string;
    soTien: number;
    ngayCanTamUng: string;
    lyDo: string;
}

export interface PhieuTamUngListItem {
    id: number;
    nvHoSoId: number;
    maNhanVien: string;
    hoTenNhanVien: string;
    mucDich: string;
    soTien: number;
    ngayCanTamUng: string;
    lyDo: string;
    trangThai: string;
    lyDoTuChoi: string | null;
    hoTenNguoiDuyet: string | null;
    ngayDuyet: string | null;
    createdAt: string;
}

export interface CreateDonDiMuonDto {
    loai: string;
    ngayApDung: string;
    thoiGianBatDau: string;
    thoiGianKetThuc: string;
    lyDo: string;
}

export interface UpdateDonDiMuonDto {
    loai: string;
    ngayApDung: string;
    thoiGianBatDau: string;
    thoiGianKetThuc: string;
    lyDo: string;
}

export interface DonDiMuonListItem {
    id: number;
    nvHoSoId: number;
    maNhanVien: string;
    hoTenNhanVien: string;
    loai: string;
    tenLoai: string;
    ngayApDung: string;
    thoiGianBatDau: string;
    thoiGianKetThuc: string;
    lyDo: string;
    trangThai: string;
    lyDoTuChoi: string | null;
    hoTenNguoiDuyet: string | null;
    ngayDuyet: string | null;
    createdAt: string;
}

export interface ThongKeNoiLamViec {
    tongDeXuat: number;
    deXuatChoDuyet: number;
    deXuatDaDuyet: number;
    deXuatTuChoi: number;

    tongTamUng: number;
    tamUngChoDuyet: number;
    tamUngDaDuyet: number;
    tamUngTuChoi: number;

    tongDiMuon: number;
    diMuonChoDuyet: number;
    diMuonDaDuyet: number;
    diMuonTuChoi: number;
}

// ─── Service ──────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class NoiLamViecApiService {
    private readonly base = `${environment.apiBaseUrl}/api/noi-lam-viec`;

    constructor(private http: HttpClient) { }

    // ── Thống kê ──────────────────────────────────────────────────────────────

    getThongKe(): Observable<ThongKeNoiLamViec> {
        return this.http.get<ThongKeNoiLamViec>(`${this.base}/thong-ke`);
    }

    // ── Phiếu đề xuất dụng cụ ─────────────────────────────────────────────────

    getDeXuatList(): Observable<PhieuDeXuatListItem[]> {
        return this.http.get<PhieuDeXuatListItem[]>(`${this.base}/de-xuat`);
    }

    createDeXuat(payload: CreatePhieuDeXuatDto): Observable<{ id: number }> {
        return this.http.post<{ id: number }>(`${this.base}/de-xuat`, payload);
    }

    deleteDeXuat(id: number): Observable<void> {
        return this.http.delete<void>(`${this.base}/de-xuat/${id}`);
    }

    updateDeXuat(id: number, payload: UpdatePhieuDeXuatDto): Observable<void> {
        return this.http.put<void>(`${this.base}/de-xuat/${id}`, payload);
    }

    // ── Phiếu tạm ứng ─────────────────────────────────────────────────────────

    getTamUngList(): Observable<PhieuTamUngListItem[]> {
        return this.http.get<PhieuTamUngListItem[]>(`${this.base}/tam-ung`);
    }

    createTamUng(payload: CreatePhieuTamUngDto): Observable<{ id: number }> {
        return this.http.post<{ id: number }>(`${this.base}/tam-ung`, payload);
    }

    deleteTamUng(id: number): Observable<void> {
        return this.http.delete<void>(`${this.base}/tam-ung/${id}`);
    }

    updateTamUng(id: number, payload: UpdatePhieuTamUngDto): Observable<void> {
        return this.http.put<void>(`${this.base}/tam-ung/${id}`, payload);
    }

    // ── Đơn đi muộn / về sớm ──────────────────────────────────────────────────

    getDiMuonList(): Observable<DonDiMuonListItem[]> {
        return this.http.get<DonDiMuonListItem[]>(`${this.base}/don-di-muon`);
    }

    createDiMuon(payload: CreateDonDiMuonDto): Observable<{ id: number }> {
        return this.http.post<{ id: number }>(`${this.base}/don-di-muon`, payload);
    }

    deleteDiMuon(id: number): Observable<void> {
        return this.http.delete<void>(`${this.base}/don-di-muon/${id}`);
    }

    updateDiMuon(id: number, payload: UpdateDonDiMuonDto): Observable<void> {
        return this.http.put<void>(`${this.base}/don-di-muon/${id}`, payload);
    }

    // ── Duyệt / Từ chối (HR) ──────────────────────────────────────────────────

    duyetDeXuat(payload: { phieuId: number; chapNhan: boolean; lyDoTuChoi?: string }): Observable<void> {
        return this.http.put<void>(`${this.base}/de-xuat/duyet`, payload);
    }

    duyetTamUng(payload: { phieuId: number; chapNhan: boolean; lyDoTuChoi?: string }): Observable<void> {
        return this.http.put<void>(`${this.base}/tam-ung/duyet`, payload);
    }

    duyetDiMuon(payload: { donId: number; chapNhan: boolean; lyDoTuChoi?: string }): Observable<void> {
        return this.http.put<void>(`${this.base}/don-di-muon/duyet`, payload);
    }
}
