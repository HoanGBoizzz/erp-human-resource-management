import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    PhongBanListDto,
    PhongBanDetailDto,
    PhongBanCreateDto,
    PhongBanUpdateDto
} from 'src/app/core/models/phong-ban.model';

@Injectable({
    providedIn: 'root'
})
export class PhongBanApiService {
    private readonly baseUrl = `${environment.apiBaseUrl}/api/PhongBan`;

    constructor(private http: HttpClient) { }

    /**
     * Lấy danh sách tất cả phòng ban
     */
    getAll(): Observable<PhongBanListDto[]> {
        return this.http.get<PhongBanListDto[]>(this.baseUrl);
    }

    /**
     * Lấy chi tiết phòng ban kèm danh sách nhân viên
     */
    getById(id: number): Observable<PhongBanDetailDto> {
        return this.http.get<PhongBanDetailDto>(`${this.baseUrl}/${id}`);
    }

    /**
     * Tạo mới phòng ban
     */
    create(dto: PhongBanCreateDto): Observable<PhongBanListDto> {
        return this.http.post<PhongBanListDto>(this.baseUrl, dto);
    }

    /**
     * Cập nhật phòng ban
     */
    update(id: number, dto: PhongBanUpdateDto): Observable<PhongBanListDto> {
        return this.http.put<PhongBanListDto>(`${this.baseUrl}/${id}`, dto);
    }

    /**
     * Xóa phòng ban
     */
    delete(id: number): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.baseUrl}/${id}`);
    }

    /**
     * Điều chuyển nhân viên sang phòng ban khác
     */
    chuyenNhanVien(formData: FormData): Observable<{ message: string }> {
        return this.http.post<{ message: string }>(`${this.baseUrl}/chuyen-nhan-vien`, formData);
    }

    xoaNhanVienKhoiPhongBan(nvCongViecId: number): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.baseUrl}/nhan-vien/${nvCongViecId}`);
    }
}
