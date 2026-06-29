// src/app/core/services/api/account-warning-api.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    AccountWarningDto,
    AccountWarningListItemDto
} from '../../models/account-warning.model';

@Injectable({ providedIn: 'root' })
export class AccountWarningApiService {
    private readonly baseUrl = `${environment.apiBaseUrl}/api/admin/accounts`;

    constructor(private http: HttpClient) { }

    /**
     * Lấy danh sách tài khoản bị cảnh báo/cấm
     * GET /api/admin/accounts/canh-bao
     */
    getWarnedAccounts(): Observable<{ accounts: AccountWarningListItemDto[] }> {
        return this.http.get<{ accounts: AccountWarningListItemDto[] }>(`${this.baseUrl}/canh-bao`);
    }

    /**
     * Đánh cảnh báo/cấm tài khoản
     * POST /api/admin/accounts/{id}/canh-bao
     */
    setWarning(accountId: number, dto: AccountWarningDto): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.baseUrl}/${accountId}/canh-bao`, dto);
    }

    /**
     * Mở khóa/gỡ cảnh báo tài khoản
     * POST /api/admin/accounts/{id}/mo-khoa
     */
    unlockAccount(accountId: number): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.baseUrl}/${accountId}/mo-khoa`, {});
    }

    /**
     * Gỡ cảnh báo hoàn toàn (reset all)
     * POST /api/admin/accounts/{id}/clear-warning
     */
    clearWarning(accountId: number): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.baseUrl}/${accountId}/clear-warning`, {});
    }
}
