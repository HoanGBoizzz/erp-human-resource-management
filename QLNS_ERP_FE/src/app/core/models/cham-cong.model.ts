export interface BangCongThangSummaryDto {
    id: number;
    thang: number;
    nam: number;
    trangThaiCong: string;
    ngayChotCong?: string | null;
}

export interface ChamCongNgayDto {
    id: number;
    nvHoSoId: number;
    hoTen: string;
    ngay: string;
    gioVao?: string | null;
    gioRa?: string | null;
    gioVaoOt?: string | null;
    gioRaOt?: string | null;
    soGioOt: number;
    trangThai: string;
    sourceModule?: string | null;
    isLockedByModule: boolean;
    ghiChu?: string | null;
}

export interface BangCongThangDetailDto {
    id: number;
    thang: number;
    nam: number;
    trangThaiCong: string;
    ngayChotCong?: string | null;
    taiKhoanChotId?: number | null;
    tenNguoiChot?: string | null;
    ngayCongs: ChamCongNgayDto[];
}

export interface ChamCongOfEmployeeDto {
    chamCongId: number;
    nvHoSoId: number;
    hoTen: string;
    ngay: string;
    gioVao?: string | null;
    gioRa?: string | null;
    gioVaoOt?: string | null;
    gioRaOt?: string | null;
    soGioOt: number;
    trangThai: string;
    ghiChu?: string | null;
    isLockedByModule: boolean;
}

export interface LockBangCongRequestDto {
    bangCongThangId: number;
    lock: boolean;
}

export interface ChamCongPagedRequestDto {
    bangCongThangId: number;
    pageIndex: number;
    pageSize: number;
    keyword?: string;
    trangThai?: string;
}

export interface ChamCongPagedResponseDto {
    items: ChamCongNgayDto[];
    totalRecords: number;
    pageIndex: number;
    pageSize: number;
    totalPages: number;
}

export interface ChamCongConfigDto {
    gioVao: string;
    gioRa: string;
    lateGraceMinutes: number;
    earlyLeaveGraceMinutes: number;
}
