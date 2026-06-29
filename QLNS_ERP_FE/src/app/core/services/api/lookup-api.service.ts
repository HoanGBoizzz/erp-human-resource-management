import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface PhongBanLookupDto {
  id: number;
  maPhongBan: string;
  tenPhongBan: string;
}

export interface ChucVuLookupDto {
  id: number;
  maChucVu: string;
  tenChucVu: string;
}

@Injectable({
  providedIn: 'root'
})
export class LookupApiService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/Lookup`;

  constructor(private http: HttpClient) { }

  getPhongBanList(): Observable<PhongBanLookupDto[]> {
    return this.http.get<PhongBanLookupDto[]>(`${this.baseUrl}/phong-ban`);
  }

  getChucVuList(): Observable<ChucVuLookupDto[]> {
    return this.http.get<ChucVuLookupDto[]>(`${this.baseUrl}/chuc-vu`);
  }
}
