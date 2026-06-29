import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import {
  NhanVienCreateDto,
  NhanVienDetailDto,
  NhanVienListItemDto,
  NhanVienUpdateDto,
  PagedResultDto,
  PagingRequestDto,
} from 'src/app/core/models/nhan-vien.model';
import { HoSoCaNhanDto } from 'src/app/core/models/ho-so-ca-nhan.model';
import { HttpParams } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class NhanVienApiService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/NhanVien`;

  constructor(private http: HttpClient) { }

  getPaged(request: PagingRequestDto): Observable<PagedResultDto<NhanVienListItemDto>> {
    const params = new HttpParams()
      .set('pageIndex', request.pageIndex)
      .set('pageSize', request.pageSize)
      .set('keyword', request.keyword || '');

    return this.http.get<PagedResultDto<NhanVienListItemDto>>(this.baseUrl, { params });
  }

  // Get all employees without pagination (for dropdowns)
  getAllNhanVien(): Observable<NhanVienListItemDto[]> {
    const params = new HttpParams()
      .set('pageIndex', '1')
      .set('pageSize', '9999')
      .set('keyword', '');

    return this.http.get<PagedResultDto<NhanVienListItemDto>>(this.baseUrl, { params }).pipe(
      map(response => response.items || [])
    );
  }

  getById(id: number): Observable<NhanVienDetailDto> {
    return this.http.get<NhanVienDetailDto>(`${this.baseUrl}/${id}`);
  }

  // Lấy đầy đủ hồ sơ nhân viên (bao gồm avatar, số TK) - dùng cho HR xem chi tiết
  getFullProfileById(nvHoSoId: number): Observable<HoSoCaNhanDto> {
    return this.http.get<HoSoCaNhanDto>(`${this.baseUrl}/ho-so-ca-nhan/${nvHoSoId}?t=${new Date().getTime()}`);
  }

  create(dto: NhanVienCreateDto): Observable<NhanVienDetailDto> {
    return this.http.post<NhanVienDetailDto>(this.baseUrl, dto);
  }

  update(id: number, dto: NhanVienUpdateDto): Observable<NhanVienDetailDto> {
    return this.http.put<NhanVienDetailDto>(`${this.baseUrl}/${id}`, dto);
  }

  markAsResigned(id: number, ngayNghiViec?: string | null): Observable<void> {
    const params = ngayNghiViec ? new HttpParams().set('ngayNghiViec', ngayNghiViec) : undefined;
    return this.http.put<void>(`${this.baseUrl}/${id}/nghi-viec`, null, { params });
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  uploadHopDong(id: number, file: File): Observable<{ hopDongUrl: string; message: string }> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<{ hopDongUrl: string; message: string }>(`${this.baseUrl}/${id}/hop-dong`, formData);
  }
}
