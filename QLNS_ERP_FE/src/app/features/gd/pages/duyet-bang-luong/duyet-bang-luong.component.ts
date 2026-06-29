import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { finalize } from 'rxjs';
import { LuongApiService } from 'src/app/core/services/api/luong-api.service';
import {
  BangLuongThangDetailDto,
  BangLuongThangListItemDto,
  DuyetLuongRequestDto,
  TrangThaiLuong
} from 'src/app/core/models/luong.model';
import { ToastService } from 'src/app/shared/services/toast.service';
import { NotificationService } from 'src/app/core/services/notification.service';

@Component({
  selector: 'app-duyet-bang-luong',
  templateUrl: './duyet-bang-luong.component.html',
  styleUrls: ['./duyet-bang-luong.component.scss']
})
export class DuyetBangLuongComponent implements OnInit {
  loadingList = false;
  loadingDetail = false;
  processing = false;
  errorMsg = '';

  // Filter controls
  keyword = '';
  statusFilter: TrangThaiLuong | 'ALL' = 'CHO_DUYET_GIAM_DOC';
  thangFilter: number | 'ALL' = 'ALL';
  namFilter: number = new Date().getFullYear();

  // Data
  list: BangLuongThangListItemDto[] = [];
  filtered: BangLuongThangListItemDto[] = [];
  detail?: BangLuongThangDetailDto;

  // Pagination
  pageIndex = 1;
  pageSize = 20;
  totalCount = 0;
  paginatedList: BangLuongThangListItemDto[] = [];

  // Modal
  showDetailModal = false;
  showDuyetModal = false;
  showBulkApproveModal = false;
  duyetForm: FormGroup;
  duyetAction: 'approve' | 'reject' = 'approve';

  // Bulk approval
  bulkProgress = { current: 0, total: 0, success: 0, failed: 0 };
  processingBulk = false;
  excludedFromBulk: number[] = []; // IDs của nhân viên bị loại khỏi duyệt hàng loạt
  bulkDetailId: number | null = null; // ID nhân viên đang xem trong modal bulk
  loadingBulkDetail = false;

  // Helpers
  availableYears: number[] = [];
  availableMonths: number[] = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];

  constructor(
    private api: LuongApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private notificationService: NotificationService
  ) {
    this.duyetForm = this.fb.group({
      lyDoTuChoi: ['']
    });

    const currentYear = new Date().getFullYear();
    for (let i = currentYear - 2; i <= currentYear + 1; i++) {
      this.availableYears.push(i);
    }
  }

  ngOnInit(): void {
    this.loadList();
  }

  loadList(): void {
    this.loadingList = true;
    this.errorMsg = '';

    this.api.getList()
      .pipe(finalize(() => (this.loadingList = false)))
      .subscribe({
        next: (data: BangLuongThangListItemDto[]) => {
          this.list = data || [];

          // Chỉ hiển thị bảng lương CHO_DUYET_GIAM_DOC (chờ duyệt)
          this.list = this.list.filter(x =>
            x.trangThai === 'CHO_DUYET_GIAM_DOC' ||
            x.trangThai === 'DA_DUYET' ||
            x.trangThai === 'TU_CHOI'
          );

          this.totalCount = this.list.length;
          this.applyFilters();
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Không thể tải danh sách bảng lương';
          this.toast.danger('Lỗi: ' + this.errorMsg);
        }
      });
  }

  applyFilters(): void {
    const q = this.keyword.trim().toLowerCase();

    this.filtered = this.list.filter(item => {
      // Filter by keyword
      if (q) {
        const text = `${item.hoTen} ${item.thang} ${item.nam}`.toLowerCase();
        if (!text.includes(q)) return false;
      }

      // Filter by status
      if (this.statusFilter !== 'ALL' && item.trangThai !== this.statusFilter) {
        return false;
      }

      // Filter by năm
      if (item.nam !== this.namFilter) {
        return false;
      }

      // Filter by tháng
      if (this.thangFilter !== 'ALL' && item.thang !== this.thangFilter) {
        return false;
      }

      return true;
    });

    this.totalCount = this.filtered.length;
    this.updatePagination();
  }

  updatePagination(): void {
    const startIdx = (this.pageIndex - 1) * this.pageSize;
    const endIdx = startIdx + this.pageSize;
    this.paginatedList = this.filtered.slice(startIdx, endIdx);
  }

  changePage(page: number): void {
    const totalPages = Math.ceil(this.totalCount / this.pageSize) || 1;
    if (page < 1 || page > totalPages) return;
    this.pageIndex = page;
    this.updatePagination();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize) || 1;
  }

  getPageNumbers(): number[] {
    const total = this.getTotalPages();
    const current = this.pageIndex;
    const delta = 2;
    const pages: number[] = [];

    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      pages.push(i);
    }

    return pages;
  }

  // ========== DETAIL MODAL ==========
  openDetail(id: number): void {
    this.loadingDetail = true;
    this.showDetailModal = true;

    this.api.getDetail(id)
      .pipe(finalize(() => (this.loadingDetail = false)))
      .subscribe({
        next: (data: BangLuongThangDetailDto) => {
          this.detail = data;
        },
        error: (err: any) => {
          this.toast.danger(err?.error?.message || 'Không thể tải chi tiết');
          this.closeDetail();
        }
      });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.detail = undefined;
  }

  // ========== DUYỆT LƯƠNG ==========
  openDuyetModal(action: 'approve' | 'reject'): void {
    if (!this.detail) return;
    this.duyetAction = action;
    this.showDuyetModal = true;
    this.duyetForm.reset({ lyDoTuChoi: '' });
  }

  closeDuyetModal(): void {
    this.showDuyetModal = false;
    this.duyetForm.reset();
  }

  saveDuyet(): void {
    if (!this.detail) return;

    const payload: DuyetLuongRequestDto = {
      dongY: this.duyetAction === 'approve',
      lyDoTuChoi: this.duyetAction === 'reject' ? this.duyetForm.value.lyDoTuChoi : null
    };

    this.processing = true;

    this.api.duyetLuong(this.detail.id, payload)
      .pipe(finalize(() => (this.processing = false)))
      .subscribe({
        next: () => {
          const msg = payload.dongY ? 'Đã duyệt bảng lương thành công' : 'Đã từ chối bảng lương';
          this.toast.success(msg);
          this.closeDuyetModal();
          this.closeDetail();
          this.loadList();
          this.notificationService.refresh();
        },
        error: (err: any) => {
          this.toast.danger(err?.error?.message || 'Không thể xử lý duyệt');
        }
      });
  }

  // ========== HELPERS ==========
  getStatusBadge(status: TrangThaiLuong): string {
    const map: Record<string, string> = {
      'CHO_DUYET_GIAM_DOC': 'badge bg-warning text-dark',
      'DA_DUYET': 'badge bg-success',
      'TU_CHOI': 'badge bg-danger',
      'TAM_TINH': 'badge bg-secondary',
      'DA_TINH': 'badge bg-info',
      'DA_KHOA': 'badge bg-dark',
      'KHAC': 'badge bg-secondary'
    };
    return map[status] || 'badge bg-secondary';
  }

  getStatusLabel(status: TrangThaiLuong): string {
    const map: Record<string, string> = {
      'CHO_DUYET_GIAM_DOC': 'Chờ duyệt',
      'DA_DUYET': 'Đã duyệt',
      'TU_CHOI': 'Từ chối',
      'TAM_TINH': 'Tạm tính',
      'DA_TINH': 'Đã tính',
      'DA_KHOA': 'Đã khóa',
      'KHAC': 'Khác'
    };
    return map[status] || status;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('vi-VN').format(value || 0);
  }

  // ========== BULK APPROVAL ==========
  getBangLuongChoDuyet(): BangLuongThangListItemDto[] {
    return this.filtered.filter((x: BangLuongThangListItemDto) =>
      x.trangThai === 'CHO_DUYET_GIAM_DOC' && !this.excludedFromBulk.includes(x.id)
    );
  }

  getTongLuongChoDuyet(): number {
    return this.getBangLuongChoDuyet().reduce((sum, x) => sum + x.tongLuong, 0);
  }

  // Xem chi tiết nhân viên trong modal bulk
  viewDetailInBulk(id: number): void {
    this.bulkDetailId = id;
    this.loadingBulkDetail = true;

    this.api.getDetail(id)
      .pipe(finalize(() => (this.loadingBulkDetail = false)))
      .subscribe({
        next: (data: BangLuongThangDetailDto) => {
          this.detail = data;
        },
        error: (err: any) => {
          this.toast.danger(err?.error?.message || 'Không thể tải chi tiết');
          this.bulkDetailId = null;
        }
      });
  }

  closeBulkDetail(): void {
    this.bulkDetailId = null;
    this.detail = undefined;
  }

  // Từ chối nhân viên này và loại khỏi danh sách duyệt hàng loạt
  rejectAndExcludeFromBulk(): void {
    if (!this.detail || !this.bulkDetailId) return;

    const lyDoTuChoi = prompt('Nhập lý do từ chối:');
    if (!lyDoTuChoi || lyDoTuChoi.trim() === '') {
      this.toast.warning('Vui lòng nhập lý do từ chối');
      return;
    }

    this.processing = true;

    this.api.duyetLuong(this.detail.id, { dongY: false, lyDoTuChoi })
      .pipe(finalize(() => (this.processing = false)))
      .subscribe({
        next: () => {
          this.toast.success(`Đã từ chối bảng lương của ${this.detail?.hoTen}`);
          this.excludedFromBulk.push(this.detail!.id);
          this.closeBulkDetail();
          this.notificationService.refresh();
        },
        error: (err: any) => {
          this.toast.danger(err?.error?.message || 'Không thể từ chối');
        }
      });
  }

  openBulkApproveModal(): void {
    const list = this.getBangLuongChoDuyet();
    if (list.length === 0) {
      this.toast.warning('Không có bảng lương nào cần duyệt');
      return;
    }
    this.showBulkApproveModal = true;
  }

  closeBulkApproveModal(): void {
    this.showBulkApproveModal = false;
    this.bulkProgress = { current: 0, total: 0, success: 0, failed: 0 };
    this.excludedFromBulk = [];
    this.bulkDetailId = null;
  }

  async processBulkApproval(): Promise<void> {
    const list = this.getBangLuongChoDuyet();
    if (list.length === 0) return;

    this.processingBulk = true;
    this.bulkProgress = { current: 0, total: list.length, success: 0, failed: 0 };

    for (const item of list) {
      try {
        await this.api.duyetLuong(item.id, { dongY: true, lyDoTuChoi: null }).toPromise();
        this.bulkProgress.success++;
      } catch (err) {
        this.bulkProgress.failed++;
      } finally {
        this.bulkProgress.current++;
      }
    }

    this.processingBulk = false;

    const rejectedCount = this.excludedFromBulk.length;
    const totalProcessed = this.bulkProgress.total;

    let msg = `Đã duyệt ${this.bulkProgress.success}/${totalProcessed} bảng lương`;

    if (rejectedCount > 0) {
      msg += ` (${rejectedCount} đã từ chối trước đó)`;
    }

    if (this.bulkProgress.failed > 0) {
      this.toast.warning(`${msg}. Có ${this.bulkProgress.failed} bảng lương thất bại`);
    } else {
      this.toast.success(msg);
    }

    this.closeBulkApproveModal();
    this.loadList();
    this.notificationService.refresh();
  }

  isExcludedFromBulk(id: number): boolean {
    return this.excludedFromBulk.includes(id);
  }
}
