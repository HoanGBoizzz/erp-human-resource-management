// src/app/core/services/api/du-an-api.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
  DuAnAddMemberDto,
  DuAnApproveRequestDto,
  DuAnCreateDto,
  DuAnDetailDto,
  DuAnFileDto,
  DuAnGuiDuyetRequestDto,
  DuAnListItemDto,
  DuAnMyApprovedListDto,
  DuAnMyListItemDto,
  DuAnUpdateDto,
  DuAnUpdateMemberRoleDto,
} from '../../models/du-an.model';

@Injectable({ providedIn: 'root' })
export class DuAnApiService {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/DuAn`;

  constructor(private http: HttpClient) { }

  // ===== LISTS =====
  getList(): Observable<DuAnListItemDto[]> {
    return this.http.get<DuAnListItemDto[]>(this.baseUrl);
  }

  getMy(): Observable<DuAnMyListItemDto[]> {
    return this.http.get<DuAnMyListItemDto[]>(`${this.baseUrl}/my`);
  }

  myApproved(): Observable<DuAnMyApprovedListDto[]> {
    return this.http.get<DuAnMyApprovedListDto[]>(`${this.baseUrl}/my-approved`);
  }

  // ===== DETAIL =====
  getDetail(id: number): Observable<DuAnDetailDto> {
    // NOTE ICU: không hiển thị /api/DuAn/{id} trong template; service thì OK.
    return this.http.get<DuAnDetailDto>(`${this.baseUrl}/${id}`);
  }

  // ===== CRUD (HR_ACC) =====
  create(dto: DuAnCreateDto): Observable<{ id: number }> {
    return this.http.post<{ id: number }>(this.baseUrl, dto);
  }

  update(id: number, dto: DuAnUpdateDto): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${id}`, dto);
  }

  guiDuyet(id: number, dto: DuAnGuiDuyetRequestDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/gui-duyet`, dto);
  }

  recall(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/thu-hoi`, {});
  }

  // ===== GIAM_DOC =====
  approve(id: number, dto: DuAnApproveRequestDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/approve`, dto);
  }

  // ===== MEMBERS (HR_ACC) =====
  addMember(duAnId: number, dto: DuAnAddMemberDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${duAnId}/members`, dto);
  }

  updateMemberRole(
    duAnId: number,
    nvId: number,
    dto: DuAnUpdateMemberRoleDto
  ): Observable<{ message: string }> {
    return this.http.put<{ message: string }>(`${this.baseUrl}/${duAnId}/members/${nvId}`, dto);
  }

  removeMember(duAnId: number, nvId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${duAnId}/members/${nvId}`);
  }

  // ===== ATTACHMENT (HR_ACC) - DEPRECATED =====
  uploadAttachment(duAnId: number, file: File): Observable<{ message: string; url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ message: string; url: string }>(
      `${this.baseUrl}/${duAnId}/attachment`,
      formData
    );
  }

  // ===== MULTI-FILE (HR_ACC) =====
  uploadFiles(duAnId: number, files: File[]): Observable<{ message: string; files: any[] }> {
    const formData = new FormData();
    files.forEach(f => formData.append('files', f));
    return this.http.post<{ message: string; files: any[] }>(
      `${this.baseUrl}/${duAnId}/files`,
      formData
    );
  }

  getFiles(duAnId: number): Observable<DuAnFileDto[]> {
    return this.http.get<DuAnFileDto[]>(`${this.baseUrl}/${duAnId}/files`);
  }

  deleteFile(duAnId: number, fileId: number): Observable<{ message: string }> {
    return this.http.delete<{ message: string }>(`${this.baseUrl}/${duAnId}/files/${fileId}`);
  }
}
