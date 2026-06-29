// =============================================
// Phòng ban (Department) models for FE
// =============================================

export interface PhongBanListDto {
    id: number;
    maPhongBan: string;
    tenPhongBan: string;
    phongBanChaId: number | null;
    tenPhongBanCha: string | null;
    trangThai: boolean;
    ghiChu: string | null;
    soNhanVienDangLam: number;
    tongNhanVien: number;
}

export interface PhongBanDetailDto {
    id: number;
    maPhongBan: string;
    tenPhongBan: string;
    phongBanChaId: number | null;
    tenPhongBanCha: string | null;
    trangThai: boolean;
    ghiChu: string | null;
    danhSachNhanVien: NhanVienTrongPhongBanDto[];
}

export interface NhanVienTrongPhongBanDto {
    nvHoSoId: number;
    nvCongViecId: number;
    maNhanVien: string;
    hoTen: string;
    tenChucVu: string | null;
    trangThaiLamViec: number;  // 1 = Đang làm, 0 = Nghỉ việc
    ngayVaoLam: string;
}

export interface PhongBanCreateDto {
    maPhongBan: string;
    tenPhongBan: string;
    phongBanChaId: number | null;
    ghiChu: string | null;
}

export interface PhongBanUpdateDto {
    tenPhongBan: string;
    phongBanChaId: number | null;
    trangThai: boolean;
    ghiChu: string | null;
}

export interface ChuyenPhongBanDto {
    nvCongViecId: number;
    phongBanMoiId: number;
}
