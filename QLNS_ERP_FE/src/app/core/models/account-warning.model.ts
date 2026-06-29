// ============================
// MODELS CHO ACCOUNT WARNING MANAGEMENT
// ============================

/**
 * Trạng thái cảnh báo tài khoản
 */
export type TrangThaiCanhBao = 'BINH_THUONG' | 'CANH_BAO' | 'CAM' | 'DA_MO_KHOA';

/**
 * DTO hiển thị danh sách tài khoản bị cảnh báo
 * Mapping từ AccountWarningListItemDto (BE)
 */
export interface AccountWarningListItemDto {
    id: number;
    tenDangNhap: string;
    hoTen: string | null;
    trangThaiCanhBao: TrangThaiCanhBao;
    lyDoCanhBao: string | null;
    nguoiCanhBao: string | null;
    ngayCanhBao: string | null;
    soLanDangNhapSai: number;
    thoiGianKhoa: string | null;
}

/**
 * DTO đánh cảnh báo/cấm tài khoản
 * Mapping từ AccountWarningDto (BE)
 */
export interface AccountWarningDto {
    trangThai: 'CANH_BAO' | 'CAM';
    lyDo: string;
}

/**
 * Labels cho trạng thái cảnh báo
 */
export const TRANG_THAI_CANH_BAO_LABELS: Record<TrangThaiCanhBao, string> = {
    'BINH_THUONG': 'Bình thường',
    'CANH_BAO': 'Cảnh báo',
    'CAM': 'Bị cấm',
    'DA_MO_KHOA': 'Đã mở khóa'
};

/**
 * CSS classes cho badges trạng thái cảnh báo
 */
export const TRANG_THAI_CANH_BAO_BADGE_CLASS: Record<TrangThaiCanhBao, string> = {
    'BINH_THUONG': 'badge-success',
    'CANH_BAO': 'badge-warning',
    'CAM': 'badge-danger',
    'DA_MO_KHOA': 'badge-info'
};
