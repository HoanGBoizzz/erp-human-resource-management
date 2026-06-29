// src/app/features/employee/pages/my-tasks/my-tasks.component.ts

import { Component, OnInit } from '@angular/core';
import { finalize } from 'rxjs';
import {
    TaskListItemDto,
    TaskTrangThai,
    TaskUuTien,
    TaskUpdateDto,
    TASK_TRANG_THAI_LABELS,
    TASK_UU_TIEN_LABELS,
    TASK_TRANG_THAI_BADGE_CLASS,
    TASK_UU_TIEN_BADGE_CLASS
} from 'src/app/core/models/task.model';
import { TaskApiService } from 'src/app/core/services/api/task-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';

@Component({
    selector: 'app-my-tasks',
    templateUrl: './my-tasks.component.html',
    styleUrls: ['./my-tasks.component.scss']
})
export class MyTasksComponent implements OnInit {
    loading = false;
    errorMsg = '';

    tasks: TaskListItemDto[] = [];
    filtered: TaskListItemDto[] = [];

    // Filters
    searchQ = '';
    filterStatus: TaskTrangThai | '' = '';
    filterUuTien: TaskUuTien | '' = '';

    // KPIs
    kpiTotal = 0;
    kpiInProgress = 0;
    kpiCompleted = 0;

    // Modal cập nhật task
    showUpdateModal = false;
    updatingTask: TaskListItemDto | null = null;
    updateForm: TaskUpdateDto = {
        trangThai: null,
        phanTramHoanThanh: null,
        ghiChu: null
    };
    saving = false;

    // Constants for template
    readonly trangThaiOptions: TaskTrangThai[] = ['MOI', 'DANG_LAM', 'CHO_REVIEW', 'HOAN_THANH', 'HUY'];
    readonly uuTienOptions: TaskUuTien[] = ['THAP', 'BINH_THUONG', 'CAO', 'KHAN_CAP'];

    constructor(
        private api: TaskApiService,
        private toast: ToastService
    ) { }

    ngOnInit(): void {
        this.loadTasks();
    }

    loadTasks(): void {
        this.loading = true;
        this.errorMsg = '';

        this.api.getMyTasks()
            .pipe(finalize(() => this.loading = false))
            .subscribe({
                next: (res) => {
                    this.tasks = res.tasks || [];
                    this.applyFilter();
                    this.updateKPIs();
                    this.toast.success('Đã tải danh sách task!');
                },
                error: (err) => {
                    console.error('Error loading tasks:', err);
                    this.errorMsg = 'Không thể tải danh sách task';
                    this.toast.danger('Không thể tải danh sách task');
                }
            });
    }

    applyFilter(): void {
        let result = [...this.tasks];

        // Search by title or project name
        if (this.searchQ.trim()) {
            const q = this.searchQ.trim().toLowerCase();
            result = result.filter(t =>
                t.tieuDe.toLowerCase().includes(q) ||
                t.duAnTen.toLowerCase().includes(q) ||
                (t.moTa && t.moTa.toLowerCase().includes(q))
            );
        }

        // Filter by status
        if (this.filterStatus) {
            result = result.filter(t => t.trangThai === this.filterStatus);
        }

        // Filter by priority
        if (this.filterUuTien) {
            result = result.filter(t => t.uuTien === this.filterUuTien);
        }

        this.filtered = result;
    }

    updateKPIs(): void {
        this.kpiTotal = this.tasks.length;
        this.kpiInProgress = this.tasks.filter(t =>
            t.trangThai === 'DANG_LAM' || t.trangThai === 'CHO_REVIEW'
        ).length;
        this.kpiCompleted = this.tasks.filter(t => t.trangThai === 'HOAN_THANH').length;
    }

    clearFilters(): void {
        this.searchQ = '';
        this.filterStatus = '';
        this.filterUuTien = '';
        this.applyFilter();
    }

    // ===== Modal Update =====
    openUpdateModal(task: TaskListItemDto): void {
        this.updatingTask = task;
        this.updateForm = {
            trangThai: task.trangThai,
            phanTramHoanThanh: task.phanTramHoanThanh,
            ghiChu: task.ghiChu
        };
        this.showUpdateModal = true;
    }

    closeUpdateModal(): void {
        this.showUpdateModal = false;
        this.updatingTask = null;
        this.updateForm = { trangThai: null, phanTramHoanThanh: null, ghiChu: null };
    }

    saveUpdate(): void {
        if (!this.updatingTask) return;

        this.saving = true;

        this.api.updateTask(this.updatingTask.id, this.updateForm)
            .pipe(finalize(() => this.saving = false))
            .subscribe({
                next: () => {
                    this.toast.success('Cập nhật task thành công!');
                    this.closeUpdateModal();
                    this.loadTasks();
                },
                error: (err) => {
                    console.error('Error updating task:', err);
                    this.toast.danger('Không thể cập nhật task');
                }
            });
    }

    // ===== Helpers =====
    getTrangThaiLabel(status: TaskTrangThai): string {
        return TASK_TRANG_THAI_LABELS[status] || status;
    }

    getUuTienLabel(priority: TaskUuTien): string {
        return TASK_UU_TIEN_LABELS[priority] || priority;
    }

    getTrangThaiBadgeClass(status: TaskTrangThai): string {
        return TASK_TRANG_THAI_BADGE_CLASS[status] || 'badge-secondary';
    }

    getUuTienBadgeClass(priority: TaskUuTien): string {
        return TASK_UU_TIEN_BADGE_CLASS[priority] || 'badge-secondary';
    }

    getProgressBarClass(percent: number): string {
        if (percent >= 100) return 'bg-success';
        if (percent >= 75) return 'bg-info';
        if (percent >= 50) return 'bg-primary';
        if (percent >= 25) return 'bg-warning';
        return 'bg-danger';
    }

    isOverdue(task: TaskListItemDto): boolean {
        if (!task.ngayKetThuc || task.trangThai === 'HOAN_THANH' || task.trangThai === 'HUY') {
            return false;
        }
        return new Date(task.ngayKetThuc) < new Date();
    }
}
