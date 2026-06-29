// src/app/core/models/du-an.models.ts

export type DuAnTrangThai =
    | 'DANG_NHAP'
    | 'CHO_DUYET_GIAM_DOC'
    | 'DA_DUYET'
    | 'TU_CHOI'
    | string;

// ================== RESPONSE DTOs ==================
export interface DuAnListItemDto {
    id: number;
    maDuAn: string;
    tenDuAn: string;
    trangThaiDuAn: DuAnTrangThai;
    tenNhanVienPhuTrach: string | null;
    ngayBatDau: string | null;
    ngayKetThuc: string | null;
    tepTinDinhKemUrl: string | null;
    tepTinDinhKemTenGoc: string | null;
}

export interface DuAnMyListItemDto {
    id: number;
    maDuAn: string;
    tenDuAn: string;
    trangThaiDuAn: DuAnTrangThai;
    tenNhanVienPhuTrach: string | null;
    ngayBatDau: string | null;
    ngayKetThuc: string | null;
    vaiTroTrongDuAn: string;
    ngayThamGia: string | null;
    tepTinDinhKemUrl: string | null;
    tepTinDinhKemTenGoc: string | null;
}

export interface DuAnThanhVienDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    vaiTroTrongDuAn: string;
    ngayThamGia: string | null;
    ngayRoiDi: string | null;
}

export interface DuAnNhatKyTrangThaiDto {
    trangThaiCu: string;
    trangThaiMoi: string;
    ghiChu: string | null;
    thoiGian: string;
    nguoiThucHien: string;
}

export interface DuAnFileDto {
    id: number;
    duAnId: number;
    tenFile: string;
    duongDanFile: string;
    kichThuoc: number | null;
    loaiFile: string | null;
    ngayTao: string;
    taiKhoanTaoId: number | null;
    tenNguoiTao: string | null;
}

export interface DuAnDetailDto {
    id: number;
    maDuAn: string;
    tenDuAn: string;
    moTa: string | null;
    nganSach: number | null;
    trangThaiDuAn: DuAnTrangThai;
    nvPhuTrachId: number | null;
    tenNvPhuTrach: string | null;

    ngayGuiDuyet: string | null;
    ngayDuyet: string | null;
    lyDoTuChoi: string | null;

    ngayBatDau: string | null;
    ngayKetThuc: string | null;

    tepTinDinhKemUrl: string | null;
    tepTinDinhKemTenGoc: string | null;
    tepTinDinhKemMime: string | null;
    tepTinDinhKemSize: number | null;

    thanhViens: DuAnThanhVienDto[];
    nhatKyTrangThais: DuAnNhatKyTrangThaiDto[];
    files: DuAnFileDto[];
}

export interface DuAnMyApprovedListDto {
    id: number;
    maDuAn: string;
    tenDuAn: string;
    ngayDuyet: string; // BE trả DateTime Value
    trangThaiDuAn: DuAnTrangThai;
}

// ================== REQUEST DTOs ==================
export interface DuAnCreateDto {
    maDuAn: string;
    tenDuAn: string;
    moTa: string | null;
    nganSach: number | null;
    ngayBatDau: string | null;
    ngayKetThuc: string | null;
    nvPhuTrachId: number | null;
}

export interface DuAnUpdateDto {
    tenDuAn: string;
    moTa: string | null;
    nganSach: number | null;
    ngayBatDau: string | null;
    ngayKetThuc: string | null;
    nvPhuTrachId: number | null;
}

export interface DuAnGuiDuyetRequestDto {
    ghiChu: string | null;
}

export interface DuAnApproveRequestDto {
    dongY: boolean;
    lyDoTuChoi: string | null;
}

export interface DuAnAddMemberDto {
    nvHoSoId: number;
    vaiTroTrongDuAn: string;
}

export interface DuAnUpdateMemberRoleDto {
    vaiTroTrongDuAn: string;
}

// Helper: init detail default (tránh optional chaining warning)
export const DU_AN_DETAIL_DEFAULT: DuAnDetailDto = {
    id: 0,
    maDuAn: '',
    tenDuAn: '',
    moTa: null,
    nganSach: null,
    trangThaiDuAn: 'DANG_NHAP',
    nvPhuTrachId: null,
    tenNvPhuTrach: null,
    ngayGuiDuyet: null,
    ngayDuyet: null,
    lyDoTuChoi: null,
    ngayBatDau: null,
    ngayKetThuc: null,
    tepTinDinhKemUrl: null,
    tepTinDinhKemTenGoc: null,
    tepTinDinhKemMime: null,
    tepTinDinhKemSize: null,
    thanhViens: [],
    nhatKyTrangThais: [],
    files: [],
};
