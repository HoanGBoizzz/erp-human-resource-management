export type TrangThaiLamViec = 0 | 1 | number;

export interface PagingRequestDto {
    pageIndex: number;
    pageSize: number;
    keyword?: string;
}

export interface PagedResultDto<T> {
    items: T[];
    totalCount: number;
    pageIndex: number;
    pageSize: number;
}

export interface NhanVienListItemDto {
    id: number;
    maNhanVien: string;
    hoTen: string;
    phongBanId?: number | null;
    tenPhongBan?: string | null;
    chucVuId?: number | null;
    tenChucVu?: string | null;
    trangThaiLamViec: TrangThaiLamViec;
    ngayVaoLam?: string | null;
    ngayNghiViec?: string | null;
}

export interface NhanVienDetailDto {
    id: number;
    maNhanVien: string;
    hoTen: string;
    anhCaNhanUrl?: string | null;
    ngaySinh?: string | null;
    gioiTinh?: number | null;
    diaChi?: string | null;
    soDienThoai?: string | null;
    emailCaNhan?: string | null;
    soTaiKhoanNganHang?: string | null;
    tenNganHang?: string | null;        // tên ngân hàng (từ NvLuongHienTai)
    chiNhanhNganHang?: string | null;   // chi nhánh (từ NvLuongHienTai)
    anhStkUrl?: string | null;  // URL ảnh sao kê tài khoản ngân hàng
    hopDongUrl?: string | null; // File hợp đồng lao động

    nvCongViecId?: number | null;
    phongBanId?: number | null;
    tenPhongBan?: string | null;
    chucVuId?: number | null;
    tenChucVu?: string | null;
    ngayVaoLam?: string | null;
    ngayNghiViec?: string | null;
    loaiHopDong?: string | null;
    ngayKyHopDong?: string | null;
    ngayHetHanHopDong?: string | null;
    trangThaiLamViec?: TrangThaiLamViec | null;
    ghiChu?: string | null;
}

export interface NhanVienCreateDto {
    maNhanVien: string;
    hoTen: string;
    anhCaNhanUrl?: string | null;
    ngaySinh?: string | null;
    gioiTinh?: number | null;
    diaChi?: string | null;
    soDienThoai?: string | null;
    emailCaNhan?: string | null;
    soTaiKhoanNganHang?: string | null;
    anhStkUrl?: string | null;

    phongBanId: number;
    chucVuId: number;
    ngayVaoLam: string;
    loaiHopDong?: string | null;
    ngayKyHopDong?: string | null;
    ngayHetHanHopDong?: string | null;
    ghiChu?: string | null;
}

export interface NhanVienUpdateDto {
    hoTen: string;
    anhCaNhanUrl?: string | null;
    ngaySinh?: string | null;
    gioiTinh?: number | null;
    diaChi?: string | null;
    soDienThoai?: string | null;
    emailCaNhan?: string | null;
    soTaiKhoanNganHang?: string | null;
    anhStkUrl?: string | null;

    phongBanId: number;
    chucVuId: number;
    ngayVaoLam: string;
    ngayNghiViec?: string | null;
    loaiHopDong?: string | null;
    ngayKyHopDong?: string | null;
    ngayHetHanHopDong?: string | null;
    trangThaiLamViec: TrangThaiLamViec;
    ghiChu?: string | null;
}
