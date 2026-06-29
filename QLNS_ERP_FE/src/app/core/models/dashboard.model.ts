export type DashboardRole = 'EMPLOYEE' | 'HR_ACC' | 'GIAM_DOC' | string;

export interface DashboardResponse<T> {
    role: DashboardRole;
    data: T;
}

// =========== EMPLOYEE ===========
export interface EmployeeDashboardDto {
    employeeId: number;
    hoTen: string;

    soNgayChamCong: number;
    tongOt: number;
    soNgayVang: number;

    tongLuong: number;
    trangThaiLuong: string; // "CHUA_CO" | ...

    donChoDuyet: number;
    donDaDuyet: number;
    donTuChoi: number;

    soDuAnThamGia: number;
}

// =========== HR ===========
export interface HrDashboardDto {
    tongNhanVien: number;
    dangLam: number;
    daNghi: number;

    tongBangCong: number;
    dangNhapLieu: number;
    daChotCong: number;

    choDuyet: number;
    daDuyet: number;
    tuChoi: number;

    canTinh: number;
    choDuyetLuong: number;
    daKhoa: number;

    deXuatChoDuyet: number;
    deXuatDuyet: number;
    deXuatTuChoi: number;
}

// =========== DIRECTOR ===========
export interface DirectorDashboardDto {
    tongNhanVien: number;
    nghiViecTrongThang: number;

    tongLuongThang: number;
    tongOtThang: number;
    bangLuongChoDuyet: number;

    tongDuAn: number;
    duAnChoDuyet: number;
    duAnDaDuyet: number;
    duAnTuChoi: number;

    nhatKyGanNhat: number;
}
/** ====== Helpers: Type Guards (để component type-safe theo role) ====== */
export function isEmployeeDashboard(
    res: DashboardResponse<any>
): res is DashboardResponse<EmployeeDashboardDto> {
    return res?.role === 'EMPLOYEE';
}

export function isHrDashboard(
    res: DashboardResponse<any>
): res is DashboardResponse<HrDashboardDto> {
    return res?.role === 'HR_ACC';
}

export function isDirectorDashboard(
    res: DashboardResponse<any>
): res is DashboardResponse<DirectorDashboardDto> {
    return res?.role === 'GIAM_DOC';
}
