import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  AccountListItem,
  AccountDetail,
  AccountCreateDto,
  AccountUpdateDto,
  ResetPasswordRequest,
  VaiTro,
  EmployeeDropdownItem,
  QuickEmployeeCreateDto,
  QuickEmployeeResponse
} from '../../models/tai-khoan.model';
import {
  NhanVienListItemDto,
  PagedResultDto,
  PagingRequestDto
} from '../../models/nhan-vien.model';

@Injectable({ providedIn: 'root' })
export class TaiKhoanApiService {
  private baseUrl = `${environment.apiBaseUrl}/api/admin/Accounts`;

  constructor(private http: HttpClient) { }

  /**
   * GET /api/admin/accounts?pageIndex=1&pageSize=20&keyword=abc
   * Danh sách tài khoản (có phân trang + keyword, không cache để real-time)
   */
  getPaged(pageIndex: number = 1, pageSize: number = 20, keyword?: string): Observable<PagedResultDto<AccountListItem>> {
    let params = new HttpParams()
      .set('pageIndex', pageIndex.toString())
      .set('pageSize', pageSize.toString())
      .set('_t', Date.now().toString()); // Thêm timestamp để tránh cache

    if (keyword && keyword.trim()) {
      params = params.set('keyword', keyword.trim());
    }

    // Headers để tránh cache
    const headers = {
      'Cache-Control': 'no-cache, no-store, must-revalidate',
      'Pragma': 'no-cache'
    };

    return this.http.get<PagedResultDto<AccountListItem>>(this.baseUrl, { params, headers });
  }

  /**
   * GET /api/admin/accounts/{id}
   * Chi tiết 1 tài khoản (không cache để lấy real-time lanDangNhapCuoi)
   */
  getById(id: number): Observable<AccountDetail> {
    // Thêm timestamp để tránh browser cache
    const params = new HttpParams().set('_t', Date.now().toString());

    // Thêm headers để tránh cache
    const headers = {
      'Cache-Control': 'no-cache, no-store, must-revalidate',
      'Pragma': 'no-cache',
      'Expires': '0'
    };

    return this.http.get<AccountDetail>(`${this.baseUrl}/${id}`, { params, headers });
  }

  /**
   * POST /api/admin/accounts
   * Tạo tài khoản mới
   */
  create(dto: AccountCreateDto): Observable<AccountDetail> {
    return this.http.post<AccountDetail>(this.baseUrl, dto);
  }

  /**
   * PUT /api/admin/accounts/{id}
   * Cập nhật vai trò / gán nhân viên / trạng thái
   */
  update(id: number, dto: AccountUpdateDto): Observable<AccountDetail> {
    return this.http.put<AccountDetail>(`${this.baseUrl}/${id}`, dto);
  }

  /**
   * PUT /api/admin/accounts/{id}/reset-password
   * Reset mật khẩu
   */
  resetPassword(id: number, dto: ResetPasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/reset-password`, dto);
  }

  /**
   * DELETE /api/admin/accounts/{id}
   * Xóa tài khoản
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * GET /api/admin/accounts/{id}/temp-password
   * Xem mật khẩu tạm của tài khoản chưa đăng nhập
   */
  getTempPassword(id: number): Observable<{ tenDangNhap: string, matKhauTam: string }> {
    return this.http.get<{ tenDangNhap: string, matKhauTam: string }>(`${this.baseUrl}/${id}/temp-password`);
  }

  // ============================
  // HELPER APIs (nếu cần)
  // ============================

  /**
   * GET /api/admin/roles?pageIndex=1&pageSize=1000
   * Lấy danh sách vai trò (để hiển thị trong dropdown)
   * Sử dụng RolesController từ BE
   */
  getVaiTros(): Observable<VaiTro[]> {
    const url = `${environment.apiBaseUrl}/api/admin/roles`;
    const params = new HttpParams()
      .set('pageIndex', '1')
      .set('pageSize', '1000')
      .set('keyword', '');

    console.log('[TaiKhoanApiService] Calling getVaiTros:', url);

    // BE trả về PagedResultDto, cần lấy items
    return this.http.get<PagedResultDto<VaiTro>>(url, { params }).pipe(
      map(response => response.items.filter(v => v.trangThai))
    );
  }

  /**
   * GET /api/NhanVien?pageIndex=1&pageSize=1000
   * Lấy danh sách nhân viên (sử dụng API hiện có - DEPRECATED, dùng getEmployeesForDropdownV2)
   * Dùng cho dropdown gán tài khoản
   */
  getNhanViensForDropdown(): Observable<PagedResultDto<NhanVienListItemDto>> {
    const params = new HttpParams()
      .set('pageIndex', '1')
      .set('pageSize', '1000')
      .set('keyword', '');

    return this.http.get<PagedResultDto<NhanVienListItemDto>>(`${environment.apiBaseUrl}/api/NhanVien`, { params });
  }

  /**
   * GET /api/admin/accounts/employees-for-dropdown
   * Danh sách nhân viên với thông tin đã có TK hay chưa
   */
  getEmployeesForDropdownV2(): Observable<EmployeeDropdownItem[]> {
    return this.http.get<EmployeeDropdownItem[]>(`${this.baseUrl}/employees-for-dropdown`);
  }

  /**
   * POST /api/admin/accounts/quick-employee
   * Tạo nhân viên nhanh (chỉ cần họ tên) để gán cho tài khoản
   */
  createQuickEmployee(dto: QuickEmployeeCreateDto): Observable<QuickEmployeeResponse> {
    return this.http.post<QuickEmployeeResponse>(`${this.baseUrl}/quick-employee`, dto);
  }
}
