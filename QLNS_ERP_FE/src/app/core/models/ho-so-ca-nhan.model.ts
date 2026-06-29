export interface NvCongViecDto {
    id: number;
    nvHoSoId: number;
    phongBanId: number;
    tenPhongBan?: string | null;
    chucVuId: number;
    tenChucVu?: string | null;
    ngayVaoLam: string;
    ngayNghiViec?: string | null;
    loaiHopDong?: string | null;
    ngayKyHopDong?: string | null;
    ngayHetHanHopDong?: string | null;
    trangThaiLamViec: number;
    ghiChu?: string | null;
}

export interface HoSoCaNhanDto {
    tenDangNhap: string;
    vaiTroId: number;
    maVaiTro: string;
    tenVaiTro: string;

    nvHoSoId: number;
    maNhanVien: string;
    hoTen: string;
    ngaySinh?: string | null;
    gioiTinh?: number | null;
    diaChi?: string | null;
    soDienThoai?: string | null;
    emailCaNhan?: string | null;
    soTaiKhoanNganHang?: string | null;
    tenNganHang?: string | null;        // tên ngân hàng (từ NvLuongHienTai)
    chiNhanhNganHang?: string | null;   // chi nhánh (từ NvLuongHienTai)
    anhCaNhanUrl?: string | null;
    anhStkUrl?: string | null;  // URL ảnh sao kê tài khoản ngân hàng
    hopDongUrl?: string | null; // File hợp đồng lao động

    congViecHienTai?: NvCongViecDto | null;
}

export interface UpdateMyBankAccountDto {
    soTaiKhoanNganHang: string | null;
}

/**
 * DTO đổi mật khẩu
 * TODO: BE cần implement endpoint PUT /api/Me/change-password
 */
export interface ChangePasswordDto {
    currentPassword: string;
    newPassword: string;
}
