import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import {
  DashboardResponse,
  DirectorDashboardDto,
  EmployeeDashboardDto,
  HrDashboardDto
} from '../../models/dashboard.model';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class DashboardApiService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/Dashboard`;

  constructor(private http: HttpClient) { }

  /**
   * GET /api/Dashboard
   * BE trả về: { role, data }
   * 
   * ⚠️ LƯU Ý:
   * - BE phải lấy employeeId từ JWT token claims
   * - Nếu role EMPLOYEE → BE query NvHoSos theo employeeId đó
   * - Nếu hoTen rỗng → BE đang query sai nhân viên
   * - Check BE logs để xem employeeId và hoTen
   */
  getDashboard(): Observable<
    DashboardResponse<EmployeeDashboardDto | HrDashboardDto | DirectorDashboardDto>
  > {
    console.log('[DashboardApiService] Calling GET /api/Dashboard');

    return this.http.get<
      DashboardResponse<EmployeeDashboardDto | HrDashboardDto | DirectorDashboardDto>
    >(this.baseUrl).pipe(
      tap({
        next: (res) => {
          console.log('[DashboardApiService] ✅ Response:', res);
          if ('hoTen' in res.data) {
            console.log(`[DashboardApiService] Employee name: "${res.data.hoTen}"`);
            if (!res.data.hoTen) {
              console.warn('[DashboardApiService] ⚠️ hoTen is empty! BE may query wrong employee.');
            }
          }
        },
        error: (err) => {
          console.error('[DashboardApiService] ❌ Error:', err);
        }
      })
    );
  }
}
