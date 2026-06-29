// Face Recognition Models - Chấm công bằng khuôn mặt

// ============ Request DTOs ============
export interface RegisterFaceDto {
    nvHoSoId: number;
    imageFile: File;
}

export interface FaceCheckInDto {
    imageFile: File;
}

export interface FaceCheckOutDto {
    imageFile: File;
}

export interface FaceLogFilterDto {
    tuNgay?: string;  // yyyy-MM-dd
    denNgay?: string; // yyyy-MM-dd
    nvHoSoId?: number;
    loai?: 'VAO' | 'RA';
    trangThai?: 'THANH_CONG' | 'THAT_BAI';
    pageIndex?: number;
    pageSize?: number;
}

// ============ Response DTOs ============
export interface RegisterFaceResponseDto {
    success: boolean;
    message: string;
    faceId?: number;
    qualityScore?: number;
}

export interface FaceRecognitionResultDto {
    success: boolean;
    message: string;
    // BE fields (camelCase from JSON serialization)
    nvHoSoId?: number;
    tenNhanVien?: string;
    confidenceScore?: number;
    thoiGianChamCong?: string;
    loaiChamCong?: string; // VAO | RA | VAO_OT | RA_OT
    chamCongId?: number;
    logId?: number;
    requireOtConfirmation?: boolean; // true = hiển thị dialog xác nhận tăng ca
    // Legacy fields (giữ tương thích với kiosk hiển thị)
    nhanVienId?: number;
    maNhanVien?: string;
    confidence?: number;
    timestamp?: Date;
    gioVao?: string;
    gioRa?: string;
    soGioLam?: number;
}

export interface FaceDataDto {
    id?: number;
    nvHoSoId: number;
    tenNhanVien?: string;
    maNhanVien?: string;
    faceImageUrl?: string;
    faceImageThumbnail?: string;
    qualityScore?: number;
    soLuongAnh?: number;
    chatLuongTrungBinh?: number;
    ngayDangKy?: Date;
    isActive: boolean;
}

export interface ChamCongFaceLogDto {
    id: number;
    nvHoSoId: number;
    tenNhanVien: string;
    maNhanVien: string;
    loai: 'VAO' | 'RA';
    trangThai: 'THANH_CONG' | 'THAT_BAI';
    thoiGian: Date;
    confidence?: number;
    faceImageUrl?: string;
    errorMessage?: string;
    deviceInfo?: string;
}

export interface FaceLogPagedResponseDto {
    items: ChamCongFaceLogDto[];
    totalCount: number;
    pageIndex: number;
    pageSize: number;
    totalPages: number;
}

export interface VerifyFaceResponseDto {
    success: boolean;
    message: string;
    nhanVienId?: number;
    tenNhanVien?: string;
    confidence?: number;
}

// ============ UI Helper Models ============
export interface FaceRegistrationState {
    isLoading: boolean;
    error?: string;
    success?: boolean;
    successMessage?: string;
    selectedEmployee?: any;
    capturedImage?: string; // base64
    previewImage?: string;
}

export interface KioskState {
    mode: 'check-in' | 'check-out' | 'idle';
    isProcessing: boolean;
    capturedImage?: string;
    result?: FaceRecognitionResultDto;
    errorMessage?: string;
    countdown?: number;
}

export interface WebcamConfig {
    width: number;
    height: number;
    facingMode: 'user' | 'environment';
    imageFormat: 'jpeg' | 'png';
    quality: number;
}
