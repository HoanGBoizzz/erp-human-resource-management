import { Component, OnInit, OnDestroy } from '@angular/core';
import { finalize, Subscription } from 'rxjs';
import {
    AccountWarningListItemDto,
    AccountWarningDto,
    TrangThaiCanhBao,
    TRANG_THAI_CANH_BAO_LABELS,
    TRANG_THAI_CANH_BAO_BADGE_CLASS
} from 'src/app/core/models/account-warning.model';
import { AccountWarningApiService } from 'src/app/core/services/api/account-warning-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { NotificationService } from 'src/app/core/services/notification.service';

@Component({
    selector: 'app-canh-bao-tai-khoan',
    templateUrl: './canh-bao-tai-khoan.component.html',
    styleUrls: ['./canh-bao-tai-khoan.component.scss']
})
export class CanhBaoTaiKhoanComponent implements OnInit, OnDestroy {
    loading = false;
    errorMsg = '';

    accounts: AccountWarningListItemDto[] = [];
    filtered: AccountWarningListItemDto[] = [];

    // Filters
    searchQ = '';
    filterStatus: TrangThaiCanhBao | '' = '';

    // KPIs
    kpiWarning = 0;
    kpiBanned = 0;
    kpiLocked = 0;

    // Modal cảnh báo/cấm
    showWarningModal = false;
    selectedAccount: AccountWarningListItemDto | null = null;
    warningStatus: 'CANH_BAO' | 'CAM' | 'BINH_THUONG' = 'CANH_BAO';
    warningReason = '';
    submitting = false;

    // Modal xác nhận mở khóa
    showUnlockModal = false;
    unlockingAccount: AccountWarningListItemDto | null = null;

    readonly statusOptions: ('CANH_BAO' | 'CAM')[] = ['CANH_BAO', 'CAM'];

    // Auto-refresh subscription
    private notificationSub: Subscription | null = null;
    private lastWarningCount = -1;

    constructor(
        private api: AccountWarningApiService,
        private toast: ToastService,
        private notificationService: NotificationService
    ) { }

    ngOnInit(): void {
        this.loadAccounts();

        // Subscribe to notification changes to auto-reload when new warnings appear
        this.notificationSub = this.notificationService.counts$.subscribe(counts => {
            if (counts && counts.taiKhoanCanhBao !== this.lastWarningCount) {
                if (this.lastWarningCount !== -1) {
                    // Only reload when count actually changes (not initial load)
                    this.loadAccounts();
                }
                this.lastWarningCount = counts.taiKhoanCanhBao;
            }
        });
    }

    ngOnDestroy(): void {
        this.notificationSub?.unsubscribe();
    }

    loadAccounts(): void {
        this.loading = true;
        this.errorMsg = '';

        console.log('[CanhBaoTaiKhoan] Loading accounts...');

        this.api.getWarnedAccounts()
            .pipe(finalize(() => this.loading = false))
            .subscribe({
                next: (res) => {
                    console.log('[CanhBaoTaiKhoan] API Response:', res);
                    this.accounts = res.accounts || [];
                    console.log('[CanhBaoTaiKhoan] Accounts loaded:', this.accounts.length);
                    this.applyFilter();
                    this.updateKPIs();
                    // Refresh badge ngay sau khi tải để đồng bộ số thực tế
                    this.notificationService.refresh();
                    if (this.accounts.length > 0) {
                        this.toast.success(`Đã tải ${this.accounts.length} tài khoản!`);
                    } else {
                        this.toast.info('Không có tài khoản nào bị cảnh báo');
                    }
                },
                error: (err) => {
                    console.error('[CanhBaoTaiKhoan] Error:', err);
                    console.error('[CanhBaoTaiKhoan] Error status:', err?.status);
                    console.error('[CanhBaoTaiKhoan] Error body:', err?.error);
                    this.errorMsg = err?.error?.message || 'Không thể tải danh sách tài khoản';
                    this.toast.danger(this.errorMsg);
                }
            });
    }

    applyFilter(): void {
        let result = [...this.accounts];

        // Search
        if (this.searchQ.trim()) {
            const q = this.searchQ.trim().toLowerCase();
            result = result.filter(a =>
                a.tenDangNhap.toLowerCase().includes(q) ||
                (a.hoTen && a.hoTen.toLowerCase().includes(q)) ||
                (a.lyDoCanhBao && a.lyDoCanhBao.toLowerCase().includes(q))
            );
        }

        // Filter by status
        if (this.filterStatus) {
            result = result.filter(a => a.trangThaiCanhBao === this.filterStatus);
        }

        this.filtered = result;
    }

    updateKPIs(): void {
        this.kpiWarning = this.accounts.filter(a => a.trangThaiCanhBao === 'CANH_BAO').length;
        this.kpiBanned = this.accounts.filter(a => a.trangThaiCanhBao === 'CAM').length;
        this.kpiLocked = this.accounts.filter(a => a.thoiGianKhoa !== null).length;
    }

    clearFilters(): void {
        this.searchQ = '';
        this.filterStatus = '';
        this.applyFilter();
    }

    // ===== Warning Modal =====
    openWarningModal(account: AccountWarningListItemDto): void {
        this.selectedAccount = account;
        if (account.trangThaiCanhBao === 'CAM') {
            this.warningStatus = 'CAM';
        } else if (account.trangThaiCanhBao === 'CANH_BAO') {
            this.warningStatus = 'CANH_BAO';
        } else {
            this.warningStatus = 'BINH_THUONG';
        }
        this.warningReason = account.lyDoCanhBao || '';
        this.showWarningModal = true;
    }

    closeWarningModal(): void {
        this.showWarningModal = false;
        this.selectedAccount = null;
        this.warningStatus = 'CANH_BAO';
        this.warningReason = '';
    }

    submitWarning(): void {
        if (!this.selectedAccount) return;
        if (this.warningStatus !== 'BINH_THUONG' && !this.warningReason.trim()) {
            this.toast.warning('Vui lòng nhập lý do!');
            return;
        }

        this.submitting = true;

        // Nếu chọn Bình thường: dùng API clear-warning để reset hoàn toàn
        if (this.warningStatus === 'BINH_THUONG') {
            this.api.clearWarning(this.selectedAccount.id)
                .pipe(finalize(() => this.submitting = false))
                .subscribe({
                    next: () => {
                        this.toast.success('Gỡ cảnh báo thành công!');
                        this.closeWarningModal();
                        this.loadAccounts();
                    },
                    error: (err) => {
                        console.error('Error clearing warning:', err);
                        this.toast.danger('Không thể gỡ cảnh báo');
                    }
                });
            return;
        }

        const warningDto: AccountWarningDto = {
            trangThai: this.warningStatus,
            lyDo: this.warningReason.trim()
        };

        this.api.setWarning(this.selectedAccount.id, warningDto)
            .pipe(finalize(() => this.submitting = false))
            .subscribe({
                next: () => {
                    const action = this.warningStatus === 'CAM' ? 'Cấm' : 'Cảnh báo';
                    this.toast.success(`${action} tài khoản thành công!`);
                    this.closeWarningModal();
                    this.loadAccounts();
                    this.notificationService.refresh();
                },
                error: (err) => {
                    console.error('Error setting warning:', err);
                    this.toast.danger('Không thể cập nhật trạng thái tài khoản');
                }
            });
    }

    // ===== Unlock Modal =====
    openUnlockModal(account: AccountWarningListItemDto): void {
        this.unlockingAccount = account;
        this.selectedAccount = account;
        this.showUnlockModal = true;
    }

    closeUnlockModal(): void {
        this.showUnlockModal = false;
        this.unlockingAccount = null;
        this.selectedAccount = null;
    }

    submitUnlock(): void {
        if (!this.unlockingAccount) return;

        this.submitting = true;

        this.api.unlockAccount(this.unlockingAccount.id)
            .pipe(finalize(() => this.submitting = false))
            .subscribe({
                next: () => {
                    this.toast.success('Đã mở khóa tài khoản!');
                    this.closeUnlockModal();
                    this.loadAccounts();
                    this.notificationService.refresh();
                },
                error: (err) => {
                    console.error('Error unlocking account:', err);
                    this.toast.danger('Không thể mở khóa tài khoản');
                }
            });
    }

    // ===== Helpers =====
    getStatusLabel(status: TrangThaiCanhBao): string {
        return TRANG_THAI_CANH_BAO_LABELS[status] || status;
    }

    getStatusBadgeClass(status: TrangThaiCanhBao): string {
        return TRANG_THAI_CANH_BAO_BADGE_CLASS[status] || 'badge-secondary';
    }

    isLocked(account: AccountWarningListItemDto): boolean {
        if (!account.thoiGianKhoa) return false;
        // Check if lock time has expired (15 minutes)
        const lockTime = new Date(account.thoiGianKhoa);
        const now = new Date();
        const diffMinutes = (now.getTime() - lockTime.getTime()) / (1000 * 60);
        return diffMinutes < 15;
    }

    getRemainingLockTime(account: AccountWarningListItemDto): string {
        if (!account.thoiGianKhoa) return '';
        const lockTime = new Date(account.thoiGianKhoa);
        const unlockTime = new Date(lockTime.getTime() + 15 * 60 * 1000);
        const now = new Date();
        const diffMs = unlockTime.getTime() - now.getTime();
        if (diffMs <= 0) return 'Đã hết';
        const minutes = Math.floor(diffMs / (1000 * 60));
        const seconds = Math.floor((diffMs % (1000 * 60)) / 1000);
        return `${minutes}p ${seconds}s`;
    }
}
