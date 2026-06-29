import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import {
  BangCongThangDetailDto,
  BangCongThangSummaryDto,
  ChamCongNgayDto,
  ChamCongOfEmployeeDto,
  LockBangCongRequestDto,
  ChamCongPagedRequestDto,
  ChamCongPagedResponseDto,
  ChamCongConfigDto
} from '../../models/cham-cong.model';

@Injectable({ providedIn: 'root' })
export class ChamCongApiService {
  private readonly adminUrl = `${environment.apiBaseUrl}/api/ChamCong`;
  private readonly meUrl = `${environment.apiBaseUrl}/api/Me`;

  constructor(private http: HttpClient) { }

  // =========================
  // ADMIN (HR / GIÁM ĐỐC)
  // =========================
  getBangCongThang(nam: number): Observable<BangCongThangSummaryDto[]> {
    const params = new HttpParams().set('nam', nam);
    return this.http.get<BangCongThangSummaryDto[]>(`${this.adminUrl}/bang-cong`, { params });
  }

  getBangCongDetail(id: number): Observable<BangCongThangDetailDto> {
    return this.http.get<BangCongThangDetailDto>(`${this.adminUrl}/bang-cong/${id}`);
  }

  getBangCongPaged(request: ChamCongPagedRequestDto): Observable<ChamCongPagedResponseDto> {
    // Đảm bảo pageIndex và pageSize là số hợp lệ
    const pageIndex = Number(request.pageIndex) || 1;
    const pageSize = Number(request.pageSize) || 20;

    let params = new HttpParams()
      .set('bangCongThangId', request.bangCongThangId.toString())
      .set('pageIndex', pageIndex.toString())
      .set('pageSize', pageSize.toString());

    if (request.keyword) {
      params = params.set('keyword', request.keyword);
    }
    if (request.trangThai && request.trangThai !== 'ALL') {
      params = params.set('trangThai', request.trangThai);
    }

    return this.http.get<ChamCongPagedResponseDto>(`${this.adminUrl}/bang-cong-paged`, { params });
  }

  getChamCongNhanVienTrongNgay(nvId: number, ngay: string): Observable<ChamCongOfEmployeeDto> {
    const params = new HttpParams().set('ngay', ngay);
    return this.http.get<ChamCongOfEmployeeDto>(`${this.adminUrl}/nhan-vien/${nvId}/ngay`, { params });
  }

  updateChamCong(chamCongId: number, dto: any): Observable<void> {
    return this.http.put<void>(`${this.adminUrl}/cap-nhat/${chamCongId}`, dto);
  }

  deleteChamCong(chamCongId: number): Observable<void> {
    return this.http.delete<void>(`${this.adminUrl}/cap-nhat/${chamCongId}`);
  }

  lockBangCong(dto: LockBangCongRequestDto): Observable<void> {
    return this.http.put<void>(`${this.adminUrl}/lock`, dto);
  }

  getConfig(): Observable<ChamCongConfigDto> {
    return this.http.get<ChamCongConfigDto>(`${this.adminUrl}/config`);
  }

  updateConfig(dto: ChamCongConfigDto): Observable<any> {
    return this.http.post<any>(`${this.adminUrl}/config`, dto);
  }

  // =========================
  // EMPLOYEE (CỦA TÔI)
  // =========================
  getMyYears(): Observable<number[]> {
    return this.http.get<number[]>(`${this.meUrl}/cham-cong/nam-list`);
  }

  getMyMonths(nam: number): Observable<number[]> {
    const params = new HttpParams().set('nam', nam);
    return this.http.get<number[]>(`${this.meUrl}/cham-cong/thang-list`, { params });
  }

  getMyTimesheetMonth(thang: number, nam: number): Observable<ChamCongNgayDto[]> {
    const params = new HttpParams().set('thang', thang).set('nam', nam);
    return this.http.get<ChamCongNgayDto[]>(`${this.meUrl}/cham-cong/thang`, { params });
  }

  getMyTimesheetDay(ngay: string): Observable<ChamCongOfEmployeeDto> {
    const params = new HttpParams().set('ngay', ngay);
    return this.http.get<ChamCongOfEmployeeDto>(`${this.meUrl}/cham-cong/ngay`, { params });
  }
}
