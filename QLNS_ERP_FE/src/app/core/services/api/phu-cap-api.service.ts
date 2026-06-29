import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    PhuCapLoaiDto, PhuCapLoaiCreateDto,
    NvPhuCapDto, NvPhuCapCreateDto, NvPhuCapUpdateDto
} from '../../models/luong.model';

@Injectable({ providedIn: 'root' })
export class PhuCapApiService {
    private base = `${environment.apiBaseUrl}/api/PhuCap`;

    constructor(private http: HttpClient) { }

    // ─── Loại phụ cấp ──────────────────────────────────────
    getAllLoai(): Observable<PhuCapLoaiDto[]> {
        return this.http.get<PhuCapLoaiDto[]>(`${this.base}/loai`);
    }

    createLoai(payload: PhuCapLoaiCreateDto): Observable<PhuCapLoaiDto> {
        return this.http.post<PhuCapLoaiDto>(`${this.base}/loai`, payload);
    }

    updateLoai(id: number, payload: PhuCapLoaiCreateDto): Observable<any> {
        return this.http.put(`${this.base}/loai/${id}`, payload);
    }

    toggleLoai(id: number): Observable<any> {
        return this.http.patch(`${this.base}/loai/${id}/toggle`, {});
    }

    // ─── Phụ cấp nhân viên ──────────────────────────────────
    getAll(): Observable<NvPhuCapDto[]> {
        return this.http.get<NvPhuCapDto[]>(this.base);
    }

    getByNv(nvId: number): Observable<NvPhuCapDto[]> {
        return this.http.get<NvPhuCapDto[]>(`${this.base}/nhan-vien/${nvId}`);
    }

    create(payload: NvPhuCapCreateDto): Observable<NvPhuCapDto> {
        return this.http.post<NvPhuCapDto>(this.base, payload);
    }

    update(id: number, payload: NvPhuCapUpdateDto): Observable<any> {
        return this.http.put(`${this.base}/${id}`, payload);
    }

    delete(id: number): Observable<any> {
        return this.http.delete(`${this.base}/${id}`);
    }
}
