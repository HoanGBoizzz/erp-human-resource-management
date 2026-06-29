// src/app/features/du-an/pages/du-an-employee/du-an-employee.component.ts

import { Component, OnDestroy, OnInit } from '@angular/core';
import { finalize, Subscription } from 'rxjs';
import {
  DU_AN_DETAIL_DEFAULT,
  DuAnDetailDto,
  DuAnMyListItemDto,
  DuAnTrangThai,
} from 'src/app/core/models/du-an.model';
import { DuAnApiService } from 'src/app/core/services/api/du-an-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import { ThongBaoApiService } from 'src/app/core/services/api/thong-bao-api.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-du-an',
  templateUrl: './du-an.component.html',
  styleUrls: ['./du-an.component.scss'],
})
export class DuAnComponent implements OnInit, OnDestroy {
  loading = false;
  detailLoading = false;
  errorMsg = '';

  list: DuAnMyListItemDto[] = [];
  filtered: DuAnMyListItemDto[] = [];

  q = '';

  // Modal state (replace drawer)
  showDetailModal = false;
  selected: DuAnDetailDto = { ...DU_AN_DETAIL_DEFAULT };
  selectedId = 0;

  // KPI
  kpiTotal = 0;
  kpiActive = 0; // đang tham gia (ngayRoiDi null ở BE query)
  kpiApproved = 0;
  kpiPending = 0;


  // SignalR subscription
  private entityUpdateSub?: Subscription;

  constructor(
    private api: DuAnApiService,
    private toast: ToastService,
    private signalrService: SignalrService,
    private thongBaoApi: ThongBaoApiService
  ) { }

  ngOnInit(): void {
    this.loadMy();

    // Subscribe to realtime entity updates
    this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
      if (update.entityType === 'DU_AN') {
        this.loadMy();
      }
    });
  }

  ngOnDestroy(): void {
    this.entityUpdateSub?.unsubscribe();
  }

  loadMy(): void {
    this.loading = true;
    this.errorMsg = '';

    this.api
      .getMy()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          this.list = res || [];
          this.applyFilter();

          const total = this.list.length;
          const approved = this.list.filter((x) => x.trangThaiDuAn === 'DA_DUYET').length;

          this.animateKpi('kpiTotal', total);
          this.animateKpi('kpiActive', total);
          this.animateKpi('kpiApproved', approved);

          const pending = this.list.filter((x) => x.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC').length;
          this.animateKpi('kpiPending', pending);

          this.toast.success('Đã làm mới dữ liệu');
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Không thể làm mới dữ liệu');
        },
      });
  }

  applyFilter(): void {
    const qLower = this.q.trim().toLowerCase();
    this.filtered = this.list.filter(
      (x) =>
        !qLower ||
        x.maDuAn.toLowerCase().includes(qLower) ||
        x.tenDuAn.toLowerCase().includes(qLower) ||
        x.vaiTroTrongDuAn.toLowerCase().includes(qLower)
    );
  }

  openDetailModal(id: number): void {
    this.selectedId = id;
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
    this.showDetailModal = true;
    this.detailLoading = true;
    this.errorMsg = '';

    this.api
      .getDetail(id)
      .pipe(finalize(() => (this.detailLoading = false)))
      .subscribe({
        next: (res) => (this.selected = res),
        error: (err) => (this.errorMsg = this.toMsg(err)),
      });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedId = 0;
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
    this.detailLoading = false;
  }

  badgeClass(st: DuAnTrangThai): string {
    if (st === 'DA_DUYET') return 'text-bg-success';
    if (st === 'CHO_DUYET_GIAM_DOC') return 'text-bg-warning';
    if (st === 'TU_CHOI') return 'text-bg-danger';
    return 'text-bg-secondary';
  }


  private animateKpi(field: 'kpiTotal' | 'kpiActive' | 'kpiApproved' | 'kpiPending', to: number): void {
    const from = Number(this[field] || 0);
    const duration = 450;
    const start = performance.now();

    const tick = (now: number) => {
      const p = Math.min(1, (now - start) / duration);
      this[field] = Math.round(from + (to - from) * p);
      if (p < 1) requestAnimationFrame(tick);
    };

    requestAnimationFrame(tick);
  }

  private toMsg(err: unknown): string {
    const anyErr = err as any;
    return anyErr?.error?.message || anyErr?.message || 'Có lỗi xảy ra';
  }

  // Helper methods for UI
  getStatusClass(status: DuAnTrangThai): string {
    switch (status) {
      case 'DA_DUYET': return 'status-approved';
      case 'CHO_DUYET_GIAM_DOC': return 'status-pending';
      case 'TU_CHOI': return 'status-rejected';
      default: return 'status-draft';
    }
  }

  getStatusLabel(status: DuAnTrangThai): string {
    switch (status) {
      case 'DA_DUYET': return 'Đã duyệt';
      case 'CHO_DUYET_GIAM_DOC': return 'Chờ Giám đốc duyệt';
      case 'TU_CHOI': return 'Từ chối';
      case 'DANG_NHAP': return 'Bản nháp';
      default: return status;
    }
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }

  getFileUrl(url: string | null): string {
    if (!url) return '';
    return `${environment.apiBaseUrl}${url}`;
  }

  getFileIcon(fileName: string): string {
    const ext = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
    if (ext === '.pdf') return 'bi-file-pdf-fill text-danger';
    if (ext === '.doc' || ext === '.docx') return 'bi-file-word-fill text-primary';
    return 'bi-file-earmark-fill text-secondary';
  }
}
