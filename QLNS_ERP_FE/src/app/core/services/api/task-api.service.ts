// src/app/core/services/api/task-api.service.ts

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import {
    TaskCreateDto,
    TaskListItemDto,
    TaskUpdateDto
} from '../../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskApiService {
    private readonly baseUrl = `${environment.apiBaseUrl}/api/tasks`;
    private readonly duAnUrl = `${environment.apiBaseUrl}/api/DuAn`;

    constructor(private http: HttpClient) { }

    // ===== EMPLOYEE: Task của tôi =====

    /**
     * Lấy danh sách task được giao cho nhân viên hiện tại
     * GET /api/tasks/cua-toi
     */
    getMyTasks(): Observable<{ tasks: TaskListItemDto[] }> {
        return this.http.get<{ tasks: TaskListItemDto[] }>(`${this.baseUrl}/cua-toi`);
    }

    /**
     * Cập nhật tiến độ task (nhân viên)
     * PUT /api/tasks/{id}
     */
    updateTask(taskId: number, dto: TaskUpdateDto): Observable<{ message: string }> {
        return this.http.put<{ message: string }>(`${this.baseUrl}/${taskId}`, dto);
    }

    // ===== TRUONG_PHONG/ADMIN: Quản lý task trong dự án =====

    /**
     * Lấy danh sách task của một dự án
     * GET /api/DuAn/{duAnId}/tasks
     */
    getTasksByProject(duAnId: number): Observable<{ tasks: TaskListItemDto[] }> {
        return this.http.get<{ tasks: TaskListItemDto[] }>(`${this.duAnUrl}/${duAnId}/tasks`);
    }

    /**
     * Tạo task mới trong dự án (Trưởng phòng/Admin)
     * POST /api/DuAn/{duAnId}/tasks
     */
    createTask(duAnId: number, dto: TaskCreateDto): Observable<{ message: string; task: TaskListItemDto }> {
        return this.http.post<{ message: string; task: TaskListItemDto }>(`${this.duAnUrl}/${duAnId}/tasks`, dto);
    }

    /**
     * Xóa task (Trưởng phòng/Admin)
     * DELETE /api/DuAn/{duAnId}/tasks/{taskId}
     */
    deleteTask(duAnId: number, taskId: number): Observable<{ message: string }> {
        return this.http.delete<{ message: string }>(`${this.duAnUrl}/${duAnId}/tasks/${taskId}`);
    }
}
