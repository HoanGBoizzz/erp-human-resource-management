import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import {
    RegisterFaceResponseDto,
    FaceRecognitionResultDto,
    FaceDataDto,
    ChamCongFaceLogDto,
    FaceLogFilterDto,
    FaceLogPagedResponseDto,
    VerifyFaceResponseDto
} from '../../models/face-recognition.model';

@Injectable({ providedIn: 'root' })
export class FaceRecognitionApiService {
    private readonly apiUrl = `${environment.apiBaseUrl}/api/face-recognition`;

    constructor(private http: HttpClient) { }

    // ============================================
    // ADMIN: Quản lý đăng ký khuôn mặt (HR, GIÁM ĐỐC)
    // ============================================

    /**
     * Đăng ký khuôn mặt cho nhân viên
     * POST /api/face-recognition/register/{nvId}
     */
    registerFace(nvId: number, imageFile: File): Observable<RegisterFaceResponseDto> {
        const formData = new FormData();
        formData.append('image', imageFile, imageFile.name);
        return this.http.post<RegisterFaceResponseDto>(`${this.apiUrl}/register/${nvId}`, formData);
    }

    /**
     * Nhân viên tự đăng ký khuôn mặt của mình
     * POST /api/face-recognition/register-self
     */
    registerSelfFace(imageFile: File): Observable<RegisterFaceResponseDto> {
        const formData = new FormData();
        formData.append('image', imageFile, imageFile.name);
        return this.http.post<RegisterFaceResponseDto>(`${this.apiUrl}/register-self`, formData);
    }

    /**
     * Lấy danh sách nhân viên đã đăng ký khuôn mặt
     * GET /api/face-recognition/registered
     */
    getRegisteredEmployees(): Observable<FaceDataDto[]> {
        return this.http.get<FaceDataDto[]>(`${this.apiUrl}/registered`);
    }

    /**
     * Lấy chi tiết face data của 1 nhân viên
     * GET /api/face-recognition/employee/{nvId}
     */
    getEmployeeFaceData(nvId: number): Observable<FaceDataDto[]> {
        return this.http.get<FaceDataDto[]>(`${this.apiUrl}/employee/${nvId}`);
    }

    /**
     * Xóa 1 ảnh khuôn mặt
     * DELETE /api/face-recognition/face/{faceId}
     */
    deleteFaceData(faceId: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/face/${faceId}`);
    }

    /**
     * Xóa tất cả ảnh khuôn mặt của nhân viên
     * DELETE /api/face-recognition/employee/{nvId}
     */
    deleteAllFaceData(nvId: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/employee/${nvId}`);
    }

    /**
     * Nhân viên tự xóa ảnh khuôn mặt
     * DELETE /api/face-recognition/my-face/{faceId}
     */
    deleteMyFaceData(faceId: number): Observable<void> {
        return this.http.delete<void>(`${this.apiUrl}/my-face/${faceId}`);
    }

    /**
     * Lấy dữ liệu khuôn mặt của chính mình (dành cho EMPLOYEE)
     * GET /api/face-recognition/my-face-data
     */
    getMyFaceData(): Observable<FaceDataDto[]> {
        return this.http.get<FaceDataDto[]>(`${this.apiUrl}/my-face-data`);
    }

    /**
     * Xem log chấm công bằng face (có phân trang)
     * GET /api/face-recognition/logs
     */
    getFaceLogs(filter: FaceLogFilterDto): Observable<FaceLogPagedResponseDto> {
        let params = new HttpParams()
            .set('pageIndex', (filter.pageIndex || 1).toString())
            .set('pageSize', (filter.pageSize || 20).toString());

        if (filter.tuNgay) {
            params = params.set('tuNgay', filter.tuNgay);
        }
        if (filter.denNgay) {
            params = params.set('denNgay', filter.denNgay);
        }
        if (filter.nvHoSoId) {
            params = params.set('nvHoSoId', filter.nvHoSoId.toString());
        }
        if (filter.loai) {
            params = params.set('loai', filter.loai);
        }
        if (filter.trangThai) {
            params = params.set('trangThai', filter.trangThai);
        }

        return this.http.get<FaceLogPagedResponseDto>(`${this.apiUrl}/logs`, { params });
    }

    /**
     * Test nhận diện khuôn mặt (không tạo chấm công)
     * POST /api/face-recognition/verify/{nvId}
     */
    verifyFace(nvId: number, imageFile: File): Observable<VerifyFaceResponseDto> {
        const formData = new FormData();
        formData.append('image', imageFile, imageFile.name);
        return this.http.post<VerifyFaceResponseDto>(`${this.apiUrl}/verify/${nvId}`, formData);
    }

    // ============================================
    // PUBLIC: Chấm công bằng khuôn mặt (Kiosk)
    // ============================================

    /**
     * Chấm công vào bằng khuôn mặt
     * POST /api/face-recognition/attendance/check-in
     * @param xacNhanOt Truyền true khi nhân viên đã xác nhận vào tăng ca
     */
    checkInByFace(imageFile: File, xacNhanOt = false): Observable<FaceRecognitionResultDto> {
        const formData = new FormData();
        formData.append('image', imageFile, imageFile.name);
        const params = new HttpParams().set('xacNhanOt', xacNhanOt ? 'true' : 'false');
        return this.http.post<FaceRecognitionResultDto>(`${this.apiUrl}/attendance/check-in`, formData, { params });
    }

    /**
     * Chấm công ra bằng khuôn mặt
     * POST /api/face-recognition/attendance/check-out
     */
    checkOutByFace(imageFile: File, xacNhanOtOut = false): Observable<FaceRecognitionResultDto> {
        const formData = new FormData();
        formData.append('image', imageFile, imageFile.name);
        const url = xacNhanOtOut
            ? `${this.apiUrl}/attendance/check-out?xacNhanOtOut=true`
            : `${this.apiUrl}/attendance/check-out`;
        return this.http.post<FaceRecognitionResultDto>(url, formData);
    }
}
