// src/app/core/models/de-xuat-giam-doc.model.ts

export type DeXuatTrangThai =
    | 'NHAP'
    | 'CHO_DUYET'
    | 'DA_DUYET'
    | 'TU_CHOI'
    | 'DA_THU_HOI';

export interface DeXuatGiamDocListItemDto {
    id: number;
    tenDeXuat: string;
    moTa?: string;
    ngayDeXuat: string;
    trangThai: DeXuatTrangThai;
    tepTinUrl?: string;
    tepTinTenGoc?: string;
    tenNguoiTao?: string;
    createdAt: string;
    ngayGuiDuyet?: string;
    ngayDuyet?: string;
}

export interface DeXuatGiamDocDetailDto {
    id: number;
    tenDeXuat: string;
    moTa?: string;
    ngayDeXuat: string;
    trangThai: DeXuatTrangThai;
    tepTinUrl?: string;
    tepTinTenGoc?: string;
    tepTinMime?: string;
    tepTinSize?: number;
    taiKhoanTaoId: number;
    tenNguoiTao?: string;
    taiKhoanDuyetId?: number;
    tenNguoiDuyet?: string;
    ngayGuiDuyet?: string;
    ngayDuyet?: string;
    lyDoTuChoi?: string;
    createdAt: string;
    updatedAt: string;
}

export interface DeXuatGiamDocCreateDto {
    tenDeXuat: string;
    moTa?: string;
    ngayDeXuat: string;
}

export interface DeXuatGiamDocUpdateDto {
    tenDeXuat: string;
    moTa?: string;
    ngayDeXuat: string;
}

export interface DeXuatGiamDocApproveDto {
    dongY: boolean;
    lyDoTuChoi?: string;
}

// Nhãn hiển thị trạng thái
export const DEXUAT_TRANGTHAI_LABEL: Record<DeXuatTrangThai, string> = {
    NHAP: 'Nháp',
    CHO_DUYET: 'Chờ duyệt',
    DA_DUYET: 'Đã duyệt',
    TU_CHOI: 'Từ chối',
    DA_THU_HOI: 'Đã thu hồi',
};

// Badge CSS class theo trạng thái
export const DEXUAT_TRANGTHAI_BADGE: Record<DeXuatTrangThai, string> = {
    NHAP: 'badge-draft',
    CHO_DUYET: 'badge-pending',
    DA_DUYET: 'badge-approved',
    TU_CHOI: 'badge-rejected',
    DA_THU_HOI: 'badge-recalled',
};
