import { Injectable } from '@angular/core';

export type TrangThaiPhieu = 'CHO_DUYET' | 'DA_DUYET' | 'TU_CHOI';

export interface PhieuDeXuat {
    id: number;
    tenDungCu: string;
    donViTinh: string;
    soLuong: number;
    giaTien: number;
    tongTien: number;
    lyDo: string;
    ngayTao: string;
    trangThai: TrangThaiPhieu;
}

export interface PhieuTamUng {
    id: number;
    mucDich: string;
    soTien: number;
    ngayCanTamUng: string;
    lyDo: string;
    ngayTao: string;
    trangThai: TrangThaiPhieu;
}

export interface DonDiMuon {
    id: number;
    loai: 'DI_MUON' | 'VE_SOM' | 'CA_HAI';
    ngayApDung: string;
    thoiGianBatDau: string;
    thoiGianKetThuc: string;
    lyDo: string;
    ngayTao: string;
    trangThai: TrangThaiPhieu;
}

const KEY_DE_XUAT = 'nlv_phieu_de_xuat';
const KEY_TAM_UNG = 'nlv_phieu_tam_ung';
const KEY_DI_MUON = 'nlv_don_di_muon';

@Injectable({ providedIn: 'root' })
export class NoiLamViecService {

    // ── PhieuDeXuat ────────────────────────────────────────────────────────────

    getDeXuatList(): PhieuDeXuat[] {
        try {
            return JSON.parse(localStorage.getItem(KEY_DE_XUAT) || '[]');
        } catch { return []; }
    }

    saveDeXuat(item: Omit<PhieuDeXuat, 'id' | 'ngayTao' | 'trangThai'>): PhieuDeXuat {
        const list = this.getDeXuatList();
        const newItem: PhieuDeXuat = {
            ...item,
            id: Date.now(),
            tongTien: item.soLuong * item.giaTien,
            ngayTao: new Date().toISOString(),
            trangThai: 'CHO_DUYET'
        };
        list.unshift(newItem);
        localStorage.setItem(KEY_DE_XUAT, JSON.stringify(list));
        return newItem;
    }

    deleteDeXuat(id: number): void {
        const list = this.getDeXuatList().filter(x => x.id !== id);
        localStorage.setItem(KEY_DE_XUAT, JSON.stringify(list));
    }

    // ── PhieuTamUng ───────────────────────────────────────────────────────────

    getTamUngList(): PhieuTamUng[] {
        try {
            return JSON.parse(localStorage.getItem(KEY_TAM_UNG) || '[]');
        } catch { return []; }
    }

    saveTamUng(item: Omit<PhieuTamUng, 'id' | 'ngayTao' | 'trangThai'>): PhieuTamUng {
        const list = this.getTamUngList();
        const newItem: PhieuTamUng = {
            ...item,
            id: Date.now(),
            ngayTao: new Date().toISOString(),
            trangThai: 'CHO_DUYET'
        };
        list.unshift(newItem);
        localStorage.setItem(KEY_TAM_UNG, JSON.stringify(list));
        return newItem;
    }

    deleteTamUng(id: number): void {
        const list = this.getTamUngList().filter(x => x.id !== id);
        localStorage.setItem(KEY_TAM_UNG, JSON.stringify(list));
    }

    // ── DonDiMuon ─────────────────────────────────────────────────────────────

    getDiMuonList(): DonDiMuon[] {
        try {
            return JSON.parse(localStorage.getItem(KEY_DI_MUON) || '[]');
        } catch { return []; }
    }

    saveDiMuon(item: Omit<DonDiMuon, 'id' | 'ngayTao' | 'trangThai'>): DonDiMuon {
        const list = this.getDiMuonList();
        const newItem: DonDiMuon = {
            ...item,
            id: Date.now(),
            ngayTao: new Date().toISOString(),
            trangThai: 'CHO_DUYET'
        };
        list.unshift(newItem);
        localStorage.setItem(KEY_DI_MUON, JSON.stringify(list));
        return newItem;
    }

    deleteDiMuon(id: number): void {
        const list = this.getDiMuonList().filter(x => x.id !== id);
        localStorage.setItem(KEY_DI_MUON, JSON.stringify(list));
    }

    // ── Thống kê ─────────────────────────────────────────────────────────────

    getThongKe() {
        const deXuat = this.getDeXuatList();
        const tamUng = this.getTamUngList();
        const diMuon = this.getDiMuonList();

        return {
            deXuat: {
                total: deXuat.length,
                choDuyet: deXuat.filter(x => x.trangThai === 'CHO_DUYET').length,
                daDuyet: deXuat.filter(x => x.trangThai === 'DA_DUYET').length,
                tuChoi: deXuat.filter(x => x.trangThai === 'TU_CHOI').length,
            },
            tamUng: {
                total: tamUng.length,
                choDuyet: tamUng.filter(x => x.trangThai === 'CHO_DUYET').length,
                daDuyet: tamUng.filter(x => x.trangThai === 'DA_DUYET').length,
                tuChoi: tamUng.filter(x => x.trangThai === 'TU_CHOI').length,
            },
            diMuon: {
                total: diMuon.length,
                choDuyet: diMuon.filter(x => x.trangThai === 'CHO_DUYET').length,
                daDuyet: diMuon.filter(x => x.trangThai === 'DA_DUYET').length,
                tuChoi: diMuon.filter(x => x.trangThai === 'TU_CHOI').length,
            }
        };
    }
}
