import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { HoSoCaNhanDto, UpdateMyBankAccountDto, ChangePasswordDto } from '../../models/ho-so-ca-nhan.model';
import { AuthService } from '../auth.service';

@Injectable({ providedIn: 'root' })
export class MeApiService {
    private readonly baseUrl = `${environment.apiBaseUrl}/api/NhanVien`;
    private readonly meUrl = `${environment.apiBaseUrl}/api/Me`;

    constructor(
        private http: HttpClient,
        private authService: AuthService
    ) { }

    /**
     * Lấy hồ sơ cá nhân của user đang đăng nhập
     * 
     * ✅ CÁCH MỚI: Gọi /api/NhanVien/me/profile
     * - BE lấy employeeId từ JWT claims
     * - Không cần truyền ID → Bảo mật hơn
     * - Tất cả roles (EMPLOYEE, HR_ACC, GIAM_DOC) đều được truy cập
     * 
     * ⚠️ LƯU Ý BE:
     * - BE phải gọi GetHoSoCaNhanAsync() thay vì GetByIdAsync()
     * - GetByIdAsync() trả về NhanVienDetailDto (thiếu TenDangNhap, VaiTro)
     * - GetHoSoCaNhanAsync() trả về HoSoCaNhanDto (đầy đủ thông tin)
     * 
     * ❌ CÁCH CŨ: /api/NhanVien/ho-so-ca-nhan/{id}
     * - Endpoint này chỉ cho HR_ACC và GIAM_DOC
     * - EMPLOYEE gọi sẽ bị 403 Forbidden
     */
    getMyProfile(): Observable<HoSoCaNhanDto> {
        const currentUser = this.authService.currentUser;

        console.log('[MeApiService] Current user:', currentUser);
        console.log('[MeApiService] Getting my profile from /api/NhanVien/me/profile');

        // ✅ Gọi endpoint mới dành cho current user
        // BE sẽ tự lấy employeeId từ JWT token claims
        const url = `${this.baseUrl}/me/profile`;

        return this.http.get<HoSoCaNhanDto>(
            `${url}?t=${new Date().getTime()}`
        ).pipe(
            tap({
                next: (data) => {
                    console.log('[MeApiService] ✅ Profile loaded:', data);

                    // ✅ Validate response structure
                    if (!data.tenDangNhap) {
                        console.warn('[MeApiService] ⚠️ Missing tenDangNhap - BE may return NhanVienDetailDto instead of HoSoCaNhanDto');
                    }
                    if (!data.soTaiKhoanNganHang) {
                        console.warn('[MeApiService] ⚠️ soTaiKhoanNganHang is null or empty');
                    } else {
                        console.log('[MeApiService] ✅ STK:', data.soTaiKhoanNganHang);
                    }
                    if (!data.congViecHienTai) {
                        console.warn('[MeApiService] ⚠️ congViecHienTai is null - Employee may not have work assignment');
                    } else {
                        console.log('[MeApiService] ✅ Work info:', {
                            phongBan: data.congViecHienTai.tenPhongBan,
                            chucVu: data.congViecHienTai.tenChucVu,
                            loaiHopDong: data.congViecHienTai.loaiHopDong
                        });
                    }
                },
                error: (err) => {
                    console.error('[MeApiService] ❌ Error loading profile:', err);
                    if (err.status === 204) {
                        console.error('[MeApiService] 204 No Content - BE không trả về dữ liệu!');
                        console.error('[MeApiService] Có thể: CongViecHienTai NULL hoặc employeeId không tồn tại');
                        console.error('[MeApiService] Kiểm tra BE logs và DB');
                    }
                    if (err.status === 500) {
                        console.error('[MeApiService] 500 Internal Server Error - BE có lỗi khi query data');
                        console.error('[MeApiService] Kiểm tra BE logs và Service method');
                    }
                }
            })
        );
    }

    getProfileById(nvHoSoId: number): Observable<HoSoCaNhanDto> {
        return this.http.get<HoSoCaNhanDto>(`${this.baseUrl}/ho-so-ca-nhan/${nvHoSoId}`);
    }

    updateMyBankAccount(dto: UpdateMyBankAccountDto): Observable<{ message: string }> {
        return this.http.put<{ message: string }>(`${this.baseUrl}/ho-so-ca-nhan/so-tai-khoan`, dto);
    }

    uploadMyAvatar(file: File): Observable<{ message: string; url: string }> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<{ message: string; url: string }>(
            `${this.baseUrl}/ho-so-ca-nhan/avatar`,
            formData
        );
    }

    /**
     * Upload ảnh sao kê tài khoản ngân hàng (STK)
     * POST /api/NhanVien/ho-so-ca-nhan/upload-anh-stk
     */
    uploadMyAnhStk(file: File): Observable<{ url: string; message: string }> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<{ url: string; message: string }>(
            `${this.baseUrl}/ho-so-ca-nhan/upload-anh-stk`,
            formData
        );
    }

    /**
     * Xóa ảnh sao kê tài khoản ngân hàng (STK)
     * DELETE /api/NhanVien/ho-so-ca-nhan/anh-stk
     */
    deleteMyAnhStk(): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.baseUrl}/ho-so-ca-nhan/anh-stk`);
    }

    /**
     * Đổi mật khẩu
     * TODO: BE cần implement endpoint PUT /api/Me/change-password
     * 
     * @param dto {currentPassword, newPassword}
     * @returns Observable<{message: string}>
     */
    changePassword(dto: ChangePasswordDto): Observable<{ message: string }> {
        // TODO: Uncomment khi BE có endpoint
        // return this.http.put<{ message: string }>(`${this.meUrl}/change-password`, dto);

        throw new Error('Chức năng đổi mật khẩu chưa được triển khai ở Backend!');
    }
}
