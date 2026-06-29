import { Component, Input, Output, EventEmitter } from '@angular/core';
import { AuditLog, ACTION_TYPES, TABLE_NAMES } from '../../../../core/models/audit-log.model';

@Component({
    selector: 'app-audit-log-detail',
    templateUrl: './audit-log-detail.component.html',
    styleUrls: ['./audit-log-detail.component.scss']
})
export class AuditLogDetailComponent {
    @Input() auditLog!: AuditLog;
    @Output() close = new EventEmitter<void>();

    tableNames = TABLE_NAMES;
    actionTypes = ACTION_TYPES;

    /**
     * Đóng modal
     */
    onClose(): void {
        this.close.emit();
    }

    /**
     * Ngăn sự kiện click lan ra backdrop
     */
    onModalClick(event: Event): void {
        event.stopPropagation();
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
}
