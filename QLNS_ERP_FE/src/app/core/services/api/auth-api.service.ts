import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { LoginRequestDto, LoginResponseDto } from '../../models/auth-user.model';
import { Observable } from 'rxjs';

export interface ChangePasswordDto {
  oldPassword: string;
  newPassword: string;
}

@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) { }

  login(payload: LoginRequestDto): Observable<LoginResponseDto> {
    return this.http.post<LoginResponseDto>(`${this.baseUrl}/api/Auth/login`, payload);
  }

  /**
   * Đổi mật khẩu cá nhân
   * PUT /api/auth/change-password
   */
  changePassword(dto: ChangePasswordDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/api/Auth/change-password`, dto);
  }
}
