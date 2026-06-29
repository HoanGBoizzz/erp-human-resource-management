import { Component, OnInit } from '@angular/core';
import { AuditLogService } from '../../../../core/services/api/audit-log.service';
import { AuditLog, AuditLogFilter, TABLE_NAMES, ACTION_TYPES, ActionType } from '../../../../core/models/audit-log.model';

@Component({
    selector: 'app-audit-log-list',
    templateUrl: './audit-log-list.component.html',
    styleUrls: ['./audit-log-list.component.scss']
})
export class AuditLogListComponent implements OnInit {
    // Data
    logs: AuditLog[] = [];
    totalCount = 0;
    selectedLog?: AuditLog;

    // Filter
    filter: AuditLogFilter = {
        pageIndex: 1,
        pageSize: 20,
        keyword: '',
        taiKhoanId: undefined,
        bang: '',
        hanhDong: '',
        tuNgay: '',
        denNgay: ''
    };

    // Loading states
    loadingList = false;
    loadingDetail = false;

    // Dropdown data
    tableNames = TABLE_NAMES;
    actionTypes = ACTION_TYPES;

    // Modal
    showDetailModal = false;

    constructor(private auditLogService: AuditLogService) { }

    ngOnInit(): void {
        this.loadData();
    }

    /**
     * Load danh sách audit logs
     */
    loadData(): void {
        this.loadingList = true;
        this.auditLogService.getPaged(this.filter).subscribe({
            next: (result) => {
                this.logs = result.items;
                this.totalCount = result.totalCount;
                this.loadingList = false;
            },
            error: (err) => {
                console.error('Lỗi khi tải danh sách audit logs:', err);
                this.loadingList = false;
            }
        });
    }

    /**
     * Áp dụng bộ lọc
     */
    applyFilter(): void {
        this.filter.pageIndex = 1;
        this.loadData();
    }

    /**
     * Reset bộ lọc
     */
    resetFilter(): void {
        this.filter = {
            pageIndex: 1,
            pageSize: 20,
            keyword: '',
            taiKhoanId: undefined,
            bang: '',
            hanhDong: '',
            tuNgay: '',
            denNgay: ''
        };
        this.loadData();
    }

    /**
     * Chuyển trang
     */
    onPageChange(page: number): void {
        this.filter.pageIndex = page;
        this.loadData();
    }

    /**
     * Xem chi tiết
     */
    viewDetail(log: AuditLog): void {
        this.loadingDetail = true;
        this.auditLogService.getById(log.id).subscribe({
            next: (detail) => {
                this.selectedLog = detail;
                this.showDetailModal = true;
                this.loadingDetail = false;
            },
            error: (err) => {
                console.error('Lỗi khi tải chi tiết audit log:', err);
                this.loadingDetail = false;
            }
        });
    }

    /**
     * Đóng modal chi tiết
     */
    closeDetailModal(): void {
        this.showDetailModal = false;
        this.selectedLog = undefined;
    }

    /**
     * Get màu sắc cho badge hành động
     */
    getActionColor(action: string): string {
        const actionType = this.actionTypes.find(a => a.value === action);
        return actionType?.color || '#64748b';
    }

    /**
     * Get gradient cho badge hành động
     */
    getActionGradient(action: string): string {
        const actionType = this.actionTypes.find(a => a.value === action);
        return actionType?.gradient || 'linear-gradient(135deg, #64748b 0%, #475569 100%)';
    }

    /**
     * Get icon cho hành động
     */
    getActionIcon(action: string): string {
        const actionType = this.actionTypes.find(a => a.value === action);
        return actionType?.icon || 'bi-question-circle';
    }

    /**
     * Get label cho bảng
     */
    getTableLabel(tableName: string): string {
        const table = this.tableNames.find(t => t.value === tableName);
        return table?.label || tableName;
    }

    /**
     * Get label cho hành động
     */
    getActionLabel(action: string): string {
        const actionType = this.actionTypes.find(a => a.value === action);
        return actionType?.label || action;
    }

    /**
     * Get tổng số trang
     */
    get totalPages(): number {
        return Math.ceil(this.totalCount / this.filter.pageSize);
    }

    /**
     * Get range hiển thị (VD: 1-20 of 100)
     */
    get displayRange(): string {
        const start = (this.filter.pageIndex - 1) * this.filter.pageSize + 1;
        const end = Math.min(this.filter.pageIndex * this.filter.pageSize, this.totalCount);
        return `${start}-${end} / ${this.totalCount}`;
    }
}
