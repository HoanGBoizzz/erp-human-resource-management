// ============================
// MODELS CHO QUẢN LÝ TÀI KHOẢN
// ============================

/**
 * Danh sách tài khoản - dùng cho table list
 */
export interface AccountListItem {
    id: number;                          // TAI_KHOAN.id
    tenDangNhap: string;                 // TAI_KHOAN.ten_dang_nhap
    vaiTroId: number;                    // TAI_KHOAN.vai_tro_id
    maVaiTro: string;                    // VAI_TRO.ma_vai_tro
    tenVaiTro: string;                   // VAI_TRO.ten_vai_tro
    trangThai: boolean;                  // TAI_KHOAN.trang_thai
    nvHoSoId: number | null;             // TAI_KHOAN.nv_ho_so_id
    hoTen: string | null;                // NV_HO_SO.ho_ten
    lanDangNhapCuoi: string | null;      // TAI_KHOAN.lan_dang_nhap_cuoi
}

/**
 * Chi tiết tài khoản
 */
export interface AccountDetail {
    id: number;                          // TAI_KHOAN.id
    tenDangNhap: string;                 // TAI_KHOAN.ten_dang_nhap
    vaiTroId: number;                    // TAI_KHOAN.vai_tro_id
    maVaiTro: string;                    // VAI_TRO.ma_vai_tro
    tenVaiTro: string;                   // VAI_TRO.ten_vai_tro
    trangThai: boolean;                  // TAI_KHOAN.trang_thai
    nvHoSoId: number | null;             // TAI_KHOAN.nv_ho_so_id
    hoTen: string | null;                // NV_HO_SO.ho_ten
    anhCaNhanUrl: string | null;         // NV_HO_SO.anh_ca_nhan_url
    lanDangNhapCuoi: string | null;      // TAI_KHOAN.lan_dang_nhap_cuoi
    createdAt: string;                   // TAI_KHOAN.created_at
    updatedAt: string;                   // TAI_KHOAN.updated_at
    canViewPassword: boolean;            // Có thể xem mật khẩu tạm không (chưa đăng nhập)
}

/**
 * DTO tạo tài khoản mới
 */
export interface AccountCreateDto {
    tenDangNhap: string;                 // TAI_KHOAN.ten_dang_nhap
    matKhau: string;                     // raw password - sẽ hash vào mat_khau_hash + mat_khau_salt
    vaiTroId: number;                    // FK VAI_TRO.id
    nvHoSoId: number | null;             // FK NV_HO_SO.id
    trangThai: boolean;                  // TAI_KHOAN.trang_thai
}

/**
 * DTO cập nhật tài khoản (không dùng để đổi mật khẩu)
 */
export interface AccountUpdateDto {
    vaiTroId: number;                    // TAI_KHOAN.vai_tro_id
    nvHoSoId: number | null;             // TAI_KHOAN.nv_ho_so_id
    trangThai: boolean;                  // TAI_KHOAN.trang_thai
}

/**
 * DTO reset mật khẩu
 */
export interface ResetPasswordRequest {
    newPassword: string;
}

/**
 * Danh sách vai trò (để hiển thị trong dropdown)
 * Mapping từ RoleListItemDto (BE)
 */
export interface VaiTro {
    id: number;
    maVaiTro: string;
    tenVaiTro: string;
    moTa: string | null;
    mucDoUuTien: number;
    trangThai: boolean;
}

/**
 * Nhân viên trong dropdown (bao gồm thông tin đã có TK hay chưa)
 */
export interface EmployeeDropdownItem {
    id: number;
    maNhanVien: string;
    hoTen: string;
    daCoTaiKhoan: boolean;           // true nếu đã được gán cho TK nào đó
    taiKhoanId: number | null;       // ID tài khoản đã gán (null nếu chưa)
    tenDangNhap: string | null;      // Tên đăng nhập của TK đã gán (null nếu chưa)
}

/**
 * DTO tạo nhân viên nhanh
 */
export interface QuickEmployeeCreateDto {
    hoTen: string;
    maNhanVien?: string | null;      // Tự sinh nếu null
}

/**
 * Response khi tạo nhân viên nhanh
 */
export interface QuickEmployeeResponse {
    id: number;
    maNhanVien: string;
    hoTen: string;
    message: string;
}
