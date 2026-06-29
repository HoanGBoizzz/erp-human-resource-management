export type DonPhepTrangThai = 'CHO_DUYET' | 'DA_DUYET' | 'TU_CHOI' | string;

/**
 * DTO trả về từ GET /api/DonPhep
 * khớp DonPhepListItemDto bên BE
 */
export interface DonPhepListItemDto {
  id: number;
  nvHoSoId: number;
  hoTen: string;
  tenLoaiPhep: string;
  tuNgay: string;   // ISO string
  denNgay: string;  // ISO string
  soNgay: number;
  trangThai: DonPhepTrangThai;
}

/**
 * DTO trả về từ GET /api/DonPhep/{id}
 * khớp DonPhepDetailDto bên BE
 */
export interface DonPhepDetailDto {
  id: number;
  nvHoSoId: number;
  hoTen: string;

  loaiPhepId: number;
  tenLoaiPhep: string;

  tuNgay: string;
  denNgay: string;
  soNgay: number;

  lyDo: string;
  trangThai: DonPhepTrangThai;

  nguoiDuyetId?: number | null;
  tenNguoiDuyet?: string | null;
  ngayDuyet?: string | null;
  lyDoTuChoi?: string | null;
  soLanCapNhat?: number;
}

/**
 * DTO gửi lên POST /api/DonPhep
 * khớp DonPhepCreateDto bên BE (cần NvHoSoId)
 */
export interface DonPhepCreateDto {
  nvHoSoId: number;
  loaiPhepId: number;
  tuNgay: string;  // yyyy-MM-dd hoặc ISO
  denNgay: string; // yyyy-MM-dd hoặc ISO
  lyDo: string;
}

/**
 * DTO gửi lên PUT /api/DonPhep/{id}
 */
export interface DonPhepUpdateDto {
  loaiPhepId: number;
  tuNgay: string;
  denNgay: string;
  lyDo: string;
}

/**
 * DTO gửi lên PUT /api/DonPhep/{id}/employee (EMPLOYEE)
 * Cho phép employee tự sửa đơn của mình khi đang chờ duyệt
 */
export interface DonPhepEmployeeUpdateDto {
  nvHoSoId: number;
  loaiPhepId: number;
  tuNgay: string;
  denNgay: string;
  lyDo: string;
}

/**
 * DTO gửi lên PUT /api/DonPhep/duyet (HR/GD)
 */
export interface DuyetDonPhepRequestDto {
  donPhepId: number;
  chapNhan: boolean;
  lyDoTuChoi?: string | null;
}

/**
 * ViewModel FE: thống kê tính tại FE (vì BE chưa có endpoint tổng hợp)
 */
export interface DonPhepThongKeVm {
  tongPhepNam: number;
  daSuDung: number;
  conLai: number;
}
