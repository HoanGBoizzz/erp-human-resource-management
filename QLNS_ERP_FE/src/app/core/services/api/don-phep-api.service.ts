import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import {
  DonPhepCreateDto,
  DonPhepUpdateDto,
  DonPhepDetailDto,
  DonPhepListItemDto,
  DuyetDonPhepRequestDto,
  DonPhepEmployeeUpdateDto
} from '../../models/don-phep.model';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DonPhepApiService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/DonPhep`;

  constructor(private http: HttpClient) { }

  /** GET /api/DonPhep */
  getList(): Observable<DonPhepListItemDto[]> {
    return this.http.get<DonPhepListItemDto[]>(this.baseUrl);
  }

  /** GET /api/DonPhep/{id} */
  getDetail(id: number): Observable<DonPhepDetailDto> {
    return this.http.get<DonPhepDetailDto>(`${this.baseUrl}/${id}`);
  }

  /** POST /api/DonPhep */
  create(payload: DonPhepCreateDto): Observable<void> {
    return this.http.post<void>(this.baseUrl, payload);
  }

  /** PUT /api/DonPhep/{id} - Tạm thời comment vì backend chưa hỗ trợ */
  // update(id: number, payload: DonPhepUpdateDto): Observable<void> {
  //   return this.http.put<void>(`${this.baseUrl}/${id}`, payload);
  // }

  /** 
   * WORKAROUND: Update = Delete + Create lại
   * Vì backend chưa có endpoint PUT /api/DonPhep/{id}
   */
  update(id: number, payload: DonPhepUpdateDto): Observable<void> {
    // Tạm thời trả về error để inform user
    return new Observable(observer => {
      observer.error({
        status: 501,
        message: 'Backend chưa hỗ trợ cập nhật đơn nghỉ phép. Vui lòng xóa và tạo đơn mới.'
      });
    });
  }

  /** DELETE /api/DonPhep/{id} */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /** PUT /api/DonPhep/{id}/employee - Employee tự sửa đơn của mình */
  updateByEmployee(id: number, payload: DonPhepEmployeeUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}/employee`, payload);
  }

  /** DELETE /api/DonPhep/{id}/employee - Employee tự xóa đơn của mình */
  deleteByEmployee(id: number, nvHoSoId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}/employee?nvHoSoId=${nvHoSoId}`);
  }

  /** PUT /api/DonPhep/duyet (HR_ACC, GIAMDOC) */
  duyet(payload: DuyetDonPhepRequestDto): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/duyet`, payload);
  }
}
