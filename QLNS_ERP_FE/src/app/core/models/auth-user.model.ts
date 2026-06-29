import { RoleCode } from 'src/app/shared/enums/role.enum';

export interface LoginRequestDto {
  username: string;
  password: string;
}

export interface LoginResponseDto {
  username: string;
  role: RoleCode | string;
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO string
  employeeId?: number; // ID nhân viên (nếu role là EMPLOYEE)
}

export interface AuthUser {
  username: string;
  role: RoleCode;
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  employeeId?: number; // ID nhân viên (nếu role là EMPLOYEE)
}
