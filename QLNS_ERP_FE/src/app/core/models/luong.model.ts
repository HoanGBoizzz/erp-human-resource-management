export type TrangThaiLuong =
    | 'KHAC'
    | 'TAM_TINH'
    | 'CHO_DUYET_GIAM_DOC'
    | 'DA_DUYET'
    | 'TU_CHOI'
    | 'DA_TINH'
    | 'DA_KHOA'
    | string;

/**
 * DTO từ BE: LuongCuaToiDto
 * GET /api/luong/me
 */
export interface LuongCuaToiDto {
    thang: number;
    nam: number;

    luongCoBan: number;
    phuCapCoDinh: number;

    tongCong: number;
    tongOt: number;

    thuong: number;
    khauTru: number;

    tongLuong: number;
    trangThai: TrangThaiLuong;
}

/**
 * DTO từ BE: BangLuongThangListItemDto
 * GET /api/luong - Dùng cho danh sách (list view)
 */
export interface BangLuongThangListItemDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    maNhanVien?: string | null;
    tenPhongBan?: string | null;
    thang: number;
    nam: number;
    tongLuong: number;

    // Chi tiết lương
    tongCong: number;
    tongOt: number;
    luongCoBanTinh: number;
    phuCapTinh: number;
    thuong: number;
    khauTru: number;
    trangThai: TrangThaiLuong;

    ngayTinhLuong?: string | null;
    ngayGuiDuyet?: string | null;
    ngayDuyetGiamDoc?: string | null;
    ngayKhoaLuong?: string | null;

    // Chi tiết khấu trừ (cho print slip)
    khauTruDiMuon?: number;
    khauTruThuongPhat?: number;
    // Cờ công thức
    coTinhPhuCap?: boolean;
    coTinhOT?: boolean;
    coTinhThuong?: boolean;
    coTinhKhauTru?: boolean;
}

/**
 * DTO từ BE: BangLuongThangDto
 * POST /api/luong/tinh - Kết quả tính lương
 */
export interface BangLuongThangDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    thang: number;
    nam: number;

    tongCong: number;
    tongOt: number;

    luongCoBanTinh: number;
    phuCapTinh: number;
    thuong: number;
    khauTru: number;

    tongLuong: number;
    trangThai: TrangThaiLuong;

    // Chi tiết breakdown
    chiTietPhuCap?: { ten: string; soTien: number }[];
    khauTruDiMuon?: number;
    khauTruThuongPhat?: number;
    soLanDiMuon?: number;
    chiTietKhauTruItems?: { ten: string; soTien: number }[];
    // Cờ công thức
    coTinhPhuCap?: boolean;
    coTinhOT?: boolean;
    coTinhThuong?: boolean;
    coTinhKhauTru?: boolean;
}

/**
 * DTO từ BE: BangLuongThangDetailDto
 * Dùng cho chi tiết bảng lương
 */
export interface BangLuongThangDetailDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    maNhanVien?: string | null;
    tenPhongBan?: string | null;

    thang: number;
    nam: number;

    tongCong: number;
    tongOt: number;

    luongCoBanTinh: number;
    phuCapTinh: number;
    thuong: number;
    khauTru: number;

    tongLuong: number;
    trangThai: TrangThaiLuong;

    ngayTinhLuong?: string | null;
    ngayGuiDuyet?: string | null;
    ngayDuyetGiamDoc?: string | null;
    ngayKhoaLuong?: string | null;

    nguoiTinh?: string | null;
    nguoiGuiDuyet?: string | null;
    nguoiDuyet?: string | null;
    nguoiKhoa?: string | null;

    ghiChu?: string | null;
    lyDoTuChoi?: string | null;
}

/**
 * DTO gửi lên POST /api/luong/tinh
 */
export interface TinhLuongRequestDto {
    thang: number;
    nam: number;
    nvHoSoId: number;
}

/**
 * DTO gửi lên POST /api/luong/{id}/gui-duyet
 */
export interface GuiDuyetLuongRequestDto {
    ghiChu?: string | null;
}

/**
 * DTO gửi lên POST /api/luong/{id}/duyet (GIÁM ĐỐC)
 */
export interface DuyetLuongRequestDto {
    dongY: boolean;
    lyDoTuChoi?: string | null;
}

export interface LuongThongKeVm {
    nam: number;
    tongLuongNam: number;
    tongOtNam: number;
    tongCongNam: number;
    thangGanNhat: number | null;
    luongThangGanNhat: number | null;
}

export interface LuongFilterVm {
    nam: number;
    thang: number | 'ALL';
    trangThai: TrangThaiLuong | 'ALL';
    keyword: string;
}

/**
 * DTO từ BE: LuongTongLuongThangDto
 * GET /api/luong/tong-luong-thang
 */
export interface LuongTongLuongThangDto {
    thang: number;
    nam: number;
    soBangLuong: number;
    tongLuongTatCa: number;
    tongLuongTamTinh: number;
    tongLuongChoDuyet: number;
    tongLuongDaDuyet: number;
    tongLuongTuChoi: number;
    tongLuongDaKhoa: number;
    tongLuongKhac: number;
}

/**
 * LuongThongKeTrangThaiDto
 */
export interface LuongThongKeTrangThaiDto {
    thang: number;
    nam: number;
    tamTinh: number;
    choDuyet: number;
    daDuyet: number;
    tuChoi: number;
    daKhoa: number;
    khac: number;
    tong: number;
}

// ─── PHỤ CẤP ─────────────────────────────────────────────────────────────────

export interface PhuCapLoaiDto {
    id: number;
    tenPhuCap: string;
    moTa?: string | null;
    laCoDinh: boolean;
    donVi: string;
    thuTu: number;
    dangHoatDong: boolean;
}

export interface PhuCapLoaiCreateDto {
    tenPhuCap: string;
    moTa?: string | null;
    laCoDinh: boolean;
    donVi: string;
    thuTu: number;
}

export interface NvPhuCapDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    phuCapLoaiId: number;
    tenPhuCap: string;
    soTien: number;
    ngayBatDau: string;
    ngayKetThuc?: string | null;
    dangApDung: boolean;
    ghiChu?: string | null;
    createdAt: string;
}

export interface NvPhuCapCreateDto {
    nvHoSoId: number;
    phuCapLoaiId: number;
    soTien: number;
    ngayBatDau: string;
    ngayKetThuc?: string | null;
    ghiChu?: string | null;
}

export interface NvPhuCapUpdateDto {
    soTien: number;
    ngayBatDau: string;
    ngayKetThuc?: string | null;
    dangApDung: boolean;
    ghiChu?: string | null;
}

// ─── THƯỎNG / KHẤU TRỪ ────────────────────────────────────────────────────────

export interface BangLuongItemDto {
    id: number;
    bangLuongThangId: number;
    loai: 'THUONG' | 'KHAU_TRU';
    lyDo: string;
    soTien: number;
    createdAt: string;
}

export interface BangLuongItemCreateDto {
    loai: 'THUONG' | 'KHAU_TRU';
    lyDo: string;
    soTien: number;
}

// ─── LƯƠNG CƠ BẢN (NvLuongHienTai) ──────────────────────────────────────────

export interface LuongCoBanDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    maNhanVien: string;
    tenPhongBan?: string | null;
    luongCoBan: number;
    phuCapCoDinh: number;
    soTaiKhoanNganHang?: string | null;
    tenNganHang?: string | null;
    chiNhanhNganHang?: string | null;
    ngayBatDauHieuLuc: string;
    ngayKetThucHieuLuc?: string | null;
    dangApDung: boolean;
}

export interface LuongCoBanUpdateDto {
    luongCoBan: number;
    phuCapCoDinh: number;
    soTaiKhoanNganHang?: string | null;
    tenNganHang?: string | null;
    chiNhanhNganHang?: string | null;
    ngayBatDauHieuLuc: string;
}
