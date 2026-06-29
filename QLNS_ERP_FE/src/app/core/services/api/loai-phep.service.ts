import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

/**
 * DTO khớp với LoaiPhepDto từ BE
 */
export interface LoaiPhep {
  id: number;
  maLoaiPhep: string;
  tenLoaiPhep: string;
}

@Injectable({
  providedIn: 'root'
})
export class LoaiPhepService {
  /**
   * Endpoint: GET /api/DonPhep/loai-phep
   * Được expose từ DonPhepController
   */
  private readonly apiUrl = `${environment.apiBaseUrl}/api/DonPhep/loai-phep`;

  constructor(private http: HttpClient) { }

  /**
   * Lấy danh sách loại phép
   * GET /api/DonPhep/loai-phep
   */
  getList(): Observable<LoaiPhep[]> {
    return this.http.get<LoaiPhep[]>(this.apiUrl);
  }
}
