import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface ThamSoHeThongDto {
  id: number;
  maThamSo: string;
  giaTri: string;
  moTa?: string;
  ngayBatDauHieuLuc: string;
  ngayKetThucHieuLuc?: string;
}

export interface ThamSoCreateDto {
  maThamSo: string;
  giaTri: string;
  moTa?: string;
  ngayBatDauHieuLuc: string;
  ngayKetThucHieuLuc?: string;
}

export interface ThamSoUpdateDto {
  giaTri: string;
  moTa?: string;
  ngayBatDauHieuLuc: string;
  ngayKetThucHieuLuc?: string;
}

@Injectable({ providedIn: 'root' })
export class ThamSoApiService {
  private base = `${environment.apiBaseUrl}/api/tham-so-he-thong`;

  constructor(private http: HttpClient) { }

  getAll(): Observable<ThamSoHeThongDto[]> {
    return this.http.get<ThamSoHeThongDto[]>(this.base);
  }

  create(dto: ThamSoCreateDto): Observable<ThamSoHeThongDto> {
    return this.http.post<ThamSoHeThongDto>(this.base, dto);
  }

  update(id: number, dto: ThamSoUpdateDto): Observable<void> {
    return this.http.put<void>(`${this.base}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
