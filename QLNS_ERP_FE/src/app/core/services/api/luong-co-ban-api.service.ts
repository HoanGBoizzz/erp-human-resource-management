import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { LuongCoBanDto, LuongCoBanUpdateDto } from '../../models/luong.model';

@Injectable({ providedIn: 'root' })
export class LuongCoBanApiService {
    private base = `${environment.apiBaseUrl}/api/luong-co-ban`;

    constructor(private http: HttpClient) { }

    getAll(): Observable<LuongCoBanDto[]> {
        return this.http.get<LuongCoBanDto[]>(this.base);
    }

    getByNv(nvId: number): Observable<LuongCoBanDto[]> {
        return this.http.get<LuongCoBanDto[]>(`${this.base}/nhan-vien/${nvId}`);
    }

    upsert(nvId: number, payload: LuongCoBanUpdateDto): Observable<any> {
        return this.http.post(`${this.base}/nhan-vien/${nvId}`, payload);
    }
}
