/**
 * Model cho Audit Log (Nhật ký hệ thống)
 */

export interface AuditLog {
    id: number;
    taiKhoanId: number;
    tenDangNhap: string;
    thoiGian: Date | string;
    bang: string;
    doiTuongId?: number;
    tenDoiTuong?: string;      // ✅ Tên đối tượng được thao tác
    truong?: string;
    giaTriCu?: string;
    giaTriMoi?: string;
    hanhDong: string;          // Các hành động: Thêm mới, Cập nhật, Xóa, Tạo đơn, Phê duyệt, Từ chối, v.v.
    ghiChu?: string;
}

export interface AuditLogFilter {
    pageIndex: number;
    pageSize: number;
    keyword?: string;
    taiKhoanId?: number;
    bang?: string;
    hanhDong?: string;
    tuNgay?: string;
    denNgay?: string;
}

export interface AuditLogPagedResult {
    items: AuditLog[];
    totalCount: number;
    pageIndex: number;
    pageSize: number;
}

/**
 * Danh sách các bảng/đối tượng trong hệ thống
 */
export const TABLE_NAMES = [
    { value: '', label: 'Tất cả' },
    { value: 'Tài khoản', label: 'Tài khoản' },
    { value: 'Hồ sơ nhân viên', label: 'Hồ sơ nhân viên' },
    { value: 'Chấm công', label: 'Chấm công' },
    { value: 'Bảng lương tháng', label: 'Bảng lương' },
    { value: 'Đơn phép', label: 'Đơn phép' },
    { value: 'Dự án', label: 'Dự án' },
];

/**
 * Danh sách loại hành động với icon và gradient
 */
export const ACTION_TYPES: ActionType[] = [
    { value: '', label: 'Tất cả hành động', color: '#64748b', gradient: '', icon: '' },
    { value: 'Thêm mới', label: 'Thêm mới', color: '#10b981', gradient: 'linear-gradient(135deg, #10b981 0%, #059669 100%)', icon: 'bi-plus-circle-fill' },
    { value: 'Cập nhật', label: 'Cập nhật', color: '#3b82f6', gradient: 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)', icon: 'bi-pencil-fill' },
    { value: 'Xóa', label: 'Xóa', color: '#ef4444', gradient: 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)', icon: 'bi-trash-fill' },
    { value: 'Tạo đơn', label: 'Tạo đơn', color: '#8b5cf6', gradient: 'linear-gradient(135deg, #8b5cf6 0%, #7c3aed 100%)', icon: 'bi-file-earmark-plus-fill' },
    { value: 'Phê duyệt', label: 'Phê duyệt', color: '#22c55e', gradient: 'linear-gradient(135deg, #22c55e 0%, #16a34a 100%)', icon: 'bi-check-circle-fill' },
    { value: 'Từ chối', label: 'Từ chối', color: '#f97316', gradient: 'linear-gradient(135deg, #f97316 0%, #ea580c 100%)', icon: 'bi-x-circle-fill' },
    { value: 'Xóa đơn', label: 'Xóa đơn', color: '#ef4444', gradient: 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)', icon: 'bi-file-earmark-x-fill' },
    { value: 'Gửi duyệt', label: 'Gửi duyệt', color: '#06b6d4', gradient: 'linear-gradient(135deg, #06b6d4 0%, #0891b2 100%)', icon: 'bi-send-fill' },
    { value: 'Tính lương', label: 'Tính lương', color: '#eab308', gradient: 'linear-gradient(135deg, #eab308 0%, #ca8a04 100%)', icon: 'bi-calculator-fill' },
];

/**
 * Interface cho Action Type
 */
export interface ActionType {
    value: string;
    label: string;
    color: string;
    gradient: string;
    icon: string;
}
