import { Injectable } from '@angular/core';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';
import { Router } from '@angular/router';
import { AuthApiService } from './api/auth-api.service';
import { AuthUser, LoginRequestDto, LoginResponseDto } from '../models/auth-user.model';
import { RoleCode } from 'src/app/shared/enums/role.enum';
import { UserAvatarService } from './user-avatar.service';

const STORAGE_KEY = 'qlns_auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly user$ = new BehaviorSubject<AuthUser | null>(this.loadFromStorage());

  constructor(
    private authApi: AuthApiService,
    private router: Router,
    private avatarService: UserAvatarService,
  ) { }

  get currentUser(): AuthUser | null {
    return this.user$.value;
  }

  get isLoggedIn(): boolean {
    const u = this.currentUser;
    if (!u) return false;
    // nếu cần check hết hạn:
    // return new Date(u.expiresAt).getTime() > Date.now();
    return true;
  }

  get role(): RoleCode | null {
    return this.currentUser?.role ?? null;
  }

  get employeeId(): number | null {
    return this.currentUser?.employeeId ?? null;
  }

  login(payload: LoginRequestDto): Observable<AuthUser> {
    return this.authApi.login(payload).pipe(
      map((res: LoginResponseDto) => {
        console.log('[AuthService] Login response:', res);

        let roleStr = (res.role || '').toUpperCase();
        if (roleStr === 'HR_KETOAN' || roleStr === 'KETOAN' || roleStr === 'HR') roleStr = RoleCode.HR_ACC;
        if (roleStr === 'GIAMDOC' || roleStr.includes('GIAM_DOC')) roleStr = RoleCode.GIAM_DOC;
        if (roleStr === 'NHANVIEN' || roleStr === 'NHAN_VIEN') roleStr = RoleCode.EMPLOYEE;

        const role = (roleStr as RoleCode) || RoleCode.EMPLOYEE;
        const user: AuthUser = {
          username: res.username,
          role,
          accessToken: res.accessToken,
          refreshToken: res.refreshToken,
          expiresAt: res.expiresAt,
          employeeId: res.employeeId, // Lưu employeeId từ response
        };

        console.log('[AuthService] ✅ User object created:', user);
        console.log('[AuthService] EmployeeId:', user.employeeId);

        return user;
      }),
      tap((user) => {
        this.saveToStorage(user);
        this.user$.next(user);
        console.log('[AuthService] ✅ User saved to localStorage');
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.avatarService.clearAvatar(); // Xóa avatar của user hiện tại, tránh user khác nhìn thấy
    this.user$.next(null);
    this.router.navigateByUrl('/auth/login');
  }

  navigateByRole(role: RoleCode): void {
    switch (role) {
      case RoleCode.EMPLOYEE:
        this.router.navigateByUrl('/employee/dashboard');
        break;
      case RoleCode.HR_ACC:
        this.router.navigateByUrl('/hr/dashboard');
        break;
      case RoleCode.GIAM_DOC:
      case RoleCode.SUPER_ADMIN:
        this.router.navigateByUrl('/gd/dashboard');
        break;
      default:
        this.router.navigateByUrl('/auth/login');
        break;
    }
  }

  private saveToStorage(user: AuthUser): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  }

  private loadFromStorage(): AuthUser | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    try {
      const obj = JSON.parse(raw) as AuthUser;
      return obj ?? null;
    } catch {
      return null;
    }
  }
}
