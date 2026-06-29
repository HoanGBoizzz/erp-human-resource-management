import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import {
  LuongCuaToiDto,
  BangLuongThangDto,
  BangLuongThangDetailDto,
  BangLuongThangListItemDto,
  TinhLuongRequestDto,
  GuiDuyetLuongRequestDto,
  DuyetLuongRequestDto,
  LuongTongLuongThangDto,
  LuongThongKeTrangThaiDto
} from '../../models/luong.model';

@Injectable({ providedIn: 'root' })
export class LuongApiService {
  private baseUrl = `${environment.apiBaseUrl}/api/Luong`;

  constructor(private http: HttpClient) { }

  /**
   * EMPLOYEE: GET /api/luong/me
   */
  getMySalary(): Observable<LuongCuaToiDto[]> {
    return this.http.get<LuongCuaToiDto[]>(`${this.baseUrl}/me`);
  }

  /**
   * HR: GET /api/luong (list all)
   */
  getList(): Observable<BangLuongThangListItemDto[]> {
    return this.http.get<BangLuongThangListItemDto[]>(this.baseUrl);
  }

  /**
   * HR, GD: GET /api/luong/{id}
   */
  getDetail(id: number): Observable<BangLuongThangDetailDto> {
    return this.http.get<BangLuongThangDetailDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * HR: POST /api/luong/tinh
   */
  tinhLuong(payload: TinhLuongRequestDto): Observable<BangLuongThangDto> {
    return this.http.post<BangLuongThangDto>(`${this.baseUrl}/tinh`, payload);
  }

  /**
   * HR: POST /api/luong/{id}/gui-duyet
   */
  guiDuyet(id: number, payload: GuiDuyetLuongRequestDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/gui-duyet`, payload);
  }

  /**
   * GIÁM ĐỐC: POST /api/luong/{id}/duyet
   */
  duyetLuong(id: number, payload: DuyetLuongRequestDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/duyet`, payload);
  }

  /**
   * HR: POST /api/luong/{id}/thu-hoi - Thu hồi bảng lương đã gửi duyệt
   */
  thuHoiLuong(id: number): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${id}/thu-hoi`, {});
  }

  /**
   * HR, GD: GET /api/luong/tong-luong-thang - Tổng lương theo tháng
   */
  getTongLuongThang(thang: number, nam: number): Observable<LuongTongLuongThangDto> {
    return this.http.get<LuongTongLuongThangDto>(`${this.baseUrl}/tong-luong-thang`, {
      params: { thang: thang.toString(), nam: nam.toString() }
    });
  }

  /**
   * HR, GD: GET /api/luong/thong-ke-trang-thai - Thống kê trạng thái
   */
  getThongKeTrangThai(thang: number, nam: number): Observable<LuongThongKeTrangThaiDto> {
    return this.http.get<LuongThongKeTrangThaiDto>(`${this.baseUrl}/thong-ke-trang-thai`, {
      params: { thang: thang.toString(), nam: nam.toString() }
    });
  }

  // ─── ITEMS (THƯỎNG / KHẤU TRỪ) ─────────────────────────────────────────────

  getItems(bangLuongId: number): Observable<import('../../models/luong.model').BangLuongItemDto[]> {
    return this.http.get<any[]>(`${environment.apiBaseUrl}/api/luong/${bangLuongId}/items`);
  }

  addItem(bangLuongId: number, payload: import('../../models/luong.model').BangLuongItemCreateDto): Observable<import('../../models/luong.model').BangLuongItemDto> {
    return this.http.post<any>(`${environment.apiBaseUrl}/api/luong/${bangLuongId}/items`, payload);
  }

  deleteItem(bangLuongId: number, itemId: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/api/luong/${bangLuongId}/items/${itemId}`);
  }
}
