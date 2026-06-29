// src/app/core/services/api/de-xuat-giam-doc-api.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    DeXuatGiamDocApproveDto,
    DeXuatGiamDocCreateDto,
    DeXuatGiamDocDetailDto,
    DeXuatGiamDocListItemDto,
    DeXuatGiamDocUpdateDto,
} from '../../models/de-xuat-giam-doc.model';

@Injectable({ providedIn: 'root' })
export class DeXuatGiamDocApiService {
    private readonly base = `${environment.apiBaseUrl}/api/DeXuatGiamDoc`;

    constructor(private http: HttpClient) { }

    // ─── Lists ────────────────────────────────────────────────────────────
    getList(): Observable<DeXuatGiamDocListItemDto[]> {
        return this.http.get<DeXuatGiamDocListItemDto[]>(this.base);
    }

    // ─── Detail ───────────────────────────────────────────────────────────
    getDetail(id: number): Observable<DeXuatGiamDocDetailDto> {
        return this.http.get<DeXuatGiamDocDetailDto>(`${this.base}/${id}`);
    }

    // ─── CRUD ─────────────────────────────────────────────────────────────
    create(dto: DeXuatGiamDocCreateDto): Observable<{ id: number; message: string }> {
        return this.http.post<{ id: number; message: string }>(this.base, dto);
    }

    update(id: number, dto: DeXuatGiamDocUpdateDto): Observable<{ message: string }> {
        return this.http.put<{ message: string }>(`${this.base}/${id}`, dto);
    }

    delete(id: number): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.base}/${id}`);
    }

    // ─── Workflow ─────────────────────────────────────────────────────────
    guiDuyet(id: number): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.base}/${id}/gui-duyet`, {});
    }

    thuHoi(id: number): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.base}/${id}/thu-hoi`, {});
    }

    // ─── Approve (Giám đốc) ───────────────────────────────────────────────
    duyet(id: number, dto: DeXuatGiamDocApproveDto): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.base}/${id}/duyet`, dto);
    }

    // ─── Upload file ──────────────────────────────────────────────────────
    uploadFile(id: number, file: File): Observable<{ message: string; url: string; tenGoc: string }> {
        const fd = new FormData();
        fd.append('file', file);
        return this.http.post<{ message: string; url: string; tenGoc: string }>(
            `${this.base}/${id}/upload`,
            fd
        );
    }
}
