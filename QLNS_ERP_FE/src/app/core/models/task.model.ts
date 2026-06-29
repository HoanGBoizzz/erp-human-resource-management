// ============================
// MODELS CHO TASK MANAGEMENT
// ============================

/**
 * Trạng thái task
 */
export type TaskTrangThai = 'MOI' | 'DANG_LAM' | 'CHO_REVIEW' | 'HOAN_THANH' | 'HUY';

/**
 * Mức độ ưu tiên task
 */
export type TaskUuTien = 'THAP' | 'BINH_THUONG' | 'CAO' | 'KHAN_CAP';

/**
 * DTO hiển thị danh sách task
 * Mapping từ TaskListItemDto (BE)
 */
export interface TaskListItemDto {
    id: number;
    duAnId: number;
    duAnTen: string;
    tieuDe: string;
    moTa: string | null;
    nhanVienId: number;
    nhanVienTen: string;
    nguoiGiaoId: number;
    nguoiGiaoTen: string;
    ngayBatDau: string | null;
    ngayKetThuc: string | null;
    uuTien: TaskUuTien;
    trangThai: TaskTrangThai;
    phanTramHoanThanh: number;
    ghiChu: string | null;
    ngayHoanThanh: string | null;
}

/**
 * DTO tạo task mới
 * Mapping từ TaskCreateDto (BE)
 */
export interface TaskCreateDto {
    tieuDe: string;
    moTa?: string | null;
    nhanVienId: number;
    ngayBatDau?: string | null;
    ngayKetThuc?: string | null;
    uuTien?: TaskUuTien;
}

/**
 * DTO cập nhật task (nhân viên update tiến độ)
 * Mapping từ TaskUpdateDto (BE)
 */
export interface TaskUpdateDto {
    trangThai?: TaskTrangThai | null;
    phanTramHoanThanh?: number | null;
    ghiChu?: string | null;
}

/**
 * Labels cho trạng thái task
 */
export const TASK_TRANG_THAI_LABELS: Record<TaskTrangThai, string> = {
    'MOI': 'Mới',
    'DANG_LAM': 'Đang làm',
    'CHO_REVIEW': 'Chờ review',
    'HOAN_THANH': 'Hoàn thành',
    'HUY': 'Đã hủy'
};

/**
 * Labels cho mức độ ưu tiên
 */
export const TASK_UU_TIEN_LABELS: Record<TaskUuTien, string> = {
    'THAP': 'Thấp',
    'BINH_THUONG': 'Bình thường',
    'CAO': 'Cao',
    'KHAN_CAP': 'Khẩn cấp'
};

/**
 * CSS classes cho badges trạng thái
 */
export const TASK_TRANG_THAI_BADGE_CLASS: Record<TaskTrangThai, string> = {
    'MOI': 'badge-info',
    'DANG_LAM': 'badge-primary',
    'CHO_REVIEW': 'badge-warning',
    'HOAN_THANH': 'badge-success',
    'HUY': 'badge-danger'
};

/**
 * CSS classes cho badges ưu tiên
 */
export const TASK_UU_TIEN_BADGE_CLASS: Record<TaskUuTien, string> = {
    'THAP': 'badge-secondary',
    'BINH_THUONG': 'badge-info',
    'CAO': 'badge-warning',
    'KHAN_CAP': 'badge-danger'
};
