import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { AuditLog, AuditLogFilter, AuditLogPagedResult } from '../../models/audit-log.model';

@Injectable({
    providedIn: 'root'
})
export class AuditLogService {
    private readonly apiUrl = `${environment.apiBaseUrl}/api/admin/AuditLog`;

    constructor(private http: HttpClient) { }

    /**
     * Lấy danh sách audit logs với filter và phân trang
     * GET api/admin/auditlog
     */
    getPaged(filter: AuditLogFilter): Observable<AuditLogPagedResult> {
        let params = new HttpParams()
            .set('pageIndex', filter.pageIndex.toString())
            .set('pageSize', filter.pageSize.toString());

        if (filter.keyword) {
            params = params.set('keyword', filter.keyword);
        }
        if (filter.taiKhoanId) {
            params = params.set('taiKhoanId', filter.taiKhoanId.toString());
        }
        if (filter.bang) {
            params = params.set('bang', filter.bang);
        }
        if (filter.hanhDong) {
            params = params.set('hanhDong', filter.hanhDong);
        }
        if (filter.tuNgay) {
            params = params.set('tuNgay', filter.tuNgay);
        }
        if (filter.denNgay) {
            params = params.set('denNgay', filter.denNgay);
        }

        return this.http.get<AuditLogPagedResult>(this.apiUrl, { params });
    }

    /**
     * Lấy chi tiết một audit log
     * GET api/admin/auditlog/{id}
     */
    getById(id: number): Observable<AuditLog> {
        return this.http.get<AuditLog>(`${this.apiUrl}/${id}`);
    }
}
