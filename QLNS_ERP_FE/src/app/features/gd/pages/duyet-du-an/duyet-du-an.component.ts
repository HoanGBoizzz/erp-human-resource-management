// src/app/features/du-an/pages/du-an-giam-doc/du-an-giam-doc.component.ts

import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { finalize, Subscription } from 'rxjs';
import {
  DU_AN_DETAIL_DEFAULT,
  DuAnDetailDto,
  DuAnListItemDto,
  DuAnMyApprovedListDto,
  DuAnTrangThai,
} from 'src/app/core/models/du-an.model';
import { DuAnApiService } from 'src/app/core/services/api/du-an-api.service';
import { NotificationService } from 'src/app/core/services/notification.service';
import { ThongBaoApiService } from 'src/app/core/services/api/thong-bao-api.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import { environment } from 'src/environments/environment';

type Tab = 'PENDING' | 'ALL' | 'MY_APPROVED';

@Component({
  selector: 'app-duyet-du-an',
  templateUrl: './duyet-du-an.component.html',
  styleUrls: ['./duyet-du-an.component.scss'],
})
export class DuyetDuAnComponent implements OnInit, OnDestroy {
  tab: Tab = 'PENDING';

  loading = false;
  detailLoading = false;
  saving = false;
  errorMsg = '';

  listAll: DuAnListItemDto[] = [];
  listPending: DuAnListItemDto[] = [];
  listMyApproved: DuAnMyApprovedListDto[] = [];

  selected: DuAnDetailDto = { ...DU_AN_DETAIL_DEFAULT };
  selectedId = 0;
  showDetailModal = false;

  q = '';

  // KPI
  kpiPending = 0;
  kpiApproved = 0;
  kpiRejected = 0;

  approveForm: FormGroup;

  @ViewChild('statusChart', { static: false }) statusChartRef?: ElementRef<HTMLCanvasElement>;
  private statusChart?: Chart;
  private entityUpdateSub?: Subscription;

  constructor(
    private api: DuAnApiService,
    fb: FormBuilder,
    private notificationService: NotificationService,
    private thongBaoApi: ThongBaoApiService,
    private signalr: SignalrService
  ) {
    this.approveForm = fb.group({
      dongY: [true, [Validators.required]],
      lyDoTuChoi: [''],
    });
  }

  ngOnInit(): void {
    this.loadAll();
    this.loadMyApproved();

    // Realtime: auto-refresh when DU_AN entity is updated by anyone
    this.entityUpdateSub = this.signalr.entityUpdate$.subscribe(update => {
      if (update.entityType === 'DU_AN') {
        this.loadAll();
        this.loadMyApproved();
        // If the updated entity is currently open in detail modal, refresh it
        if (this.showDetailModal && this.selectedId === update.entityId) {
          this.openDetail(update.entityId);
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.statusChart) this.statusChart.destroy();
    this.entityUpdateSub?.unsubscribe();
  }

  setTab(t: Tab): void {
    this.tab = t;
  }

  loadAll(): void {
    this.loading = true;
    this.errorMsg = '';

    this.api
      .getList()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          this.listAll = res || [];
          this.listPending = this.listAll.filter((x) => x.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC');

          const approved = this.listAll.filter((x) => x.trangThaiDuAn === 'DA_DUYET').length;
          const rejected = this.listAll.filter((x) => x.trangThaiDuAn === 'TU_CHOI').length;

          this.animateKpi('kpiPending', this.listPending.length);
          this.animateKpi('kpiApproved', approved);
          this.animateKpi('kpiRejected', rejected);

          setTimeout(() => this.renderChart(this.listPending.length, approved, rejected));
        },
        error: (err) => (this.errorMsg = this.toMsg(err)),
      });
  }

  loadMyApproved(): void {
    this.api.myApproved().subscribe({
      next: (res) => (this.listMyApproved = res || []),
      error: () => {
        // không chặn UI, chỉ fail nhẹ
      },
    });
  }

  openDetail(id: number): void {
    this.selectedId = id;
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
    this.showDetailModal = true;
    this.detailLoading = true;
    this.errorMsg = '';

    // Mark related notifications as read
    this.thongBaoApi.markAsReadByEntity('DU_AN', id).subscribe();

    this.api
      .getDetail(id)
      .pipe(finalize(() => (this.detailLoading = false)))
      .subscribe({
        next: (res) => {
          this.selected = res;
          // auto set form
          this.approveForm.reset({ dongY: true, lyDoTuChoi: '' });
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.closeDetailModal();
        },
      });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedId = 0;
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
    this.approveForm.reset();
  }

  canProcess(): boolean {
    return this.selectedId > 0 && this.selected.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC';
  }

  submitApprove(): void {
    if (!this.canProcess()) return;

    const dongY = Boolean(this.approveForm.getRawValue().dongY);
    const lyDo = (this.approveForm.getRawValue().lyDoTuChoi || '').trim();

    if (!dongY && !lyDo) {
      this.errorMsg = 'Vui lòng nhập lý do từ chối.';
      return;
    }

    this.saving = true;
    this.errorMsg = '';

    this.api
      .approve(this.selectedId, { dongY, lyDoTuChoi: dongY ? null : lyDo })
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.closeDetailModal();
          alert('Đã duyệt thành công!');

          // Reload after user confirms alert (using setTimeout to ensure UI updates)
          setTimeout(() => {
            this.loadAll();
            this.loadMyApproved();
            this.notificationService.refresh();
          }, 0);
        },
        error: (err) => (this.errorMsg = this.toMsg(err)),
      });
  }

  // ===== UI helpers =====
  badgeClass(st: DuAnTrangThai): string {
    if (st === 'DA_DUYET') return 'text-bg-success';
    if (st === 'CHO_DUYET_GIAM_DOC') return 'text-bg-warning';
    if (st === 'TU_CHOI') return 'text-bg-danger';
    return 'text-bg-secondary';
  }

  getStatusLabel(st: DuAnTrangThai): string {
    if (st === 'DANG_NHAP') return 'Đang nháp';
    if (st === 'CHO_DUYET_GIAM_DOC') return 'Chờ duyệt';
    if (st === 'DA_DUYET') return 'Đã duyệt';
    if (st === 'TU_CHOI') return 'Từ chối';
    return st;
  }

  filteredList(): DuAnListItemDto[] {
    const qLower = this.q.trim().toLowerCase();
    const src = this.tab === 'PENDING' ? this.listPending : this.listAll;
    if (!qLower) return src;

    return src.filter(
      (x) =>
        x.maDuAn.toLowerCase().includes(qLower) ||
        x.tenDuAn.toLowerCase().includes(qLower) ||
        (x.tenNhanVienPhuTrach || '').toLowerCase().includes(qLower)
    );
  }

  private renderChart(pending: number, approved: number, rejected: number): void {
    const canvas = this.statusChartRef?.nativeElement;
    if (!canvas) return;

    if (this.statusChart) this.statusChart.destroy();

    const total = pending + approved + rejected;

    // Bar chart with advanced tooltip following tooltip_chart.md
    const config: ChartConfiguration<'bar', number[], string> = {
      type: 'bar',
      data: {
        labels: ['Chờ duyệt', 'Đã duyệt', 'Từ chối'],
        datasets: [{
          label: 'Số lượng dự án',
          data: [pending, approved, rejected],
          backgroundColor: ['#f97316', '#22c55e', '#ef4444'],
          borderRadius: 6,
          borderSkipped: false,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            padding: 12,
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            footerFont: { size: 12, weight: 'bold' },
            displayColors: true,
            borderColor: '#ffffff',
            borderWidth: 1,
            callbacks: {
              label: (context) => {
                const value = context.parsed.y ?? 0;
                const percent = total > 0
                  ? ((value / total) * 100).toFixed(1)
                  : '0';
                return `${context.label}: ${value} dự án (${percent}%)`;
              },
              footer: () => `Tổng: ${total} dự án`,
            },
          },
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: { color: '#f1f5f9' },
            ticks: { font: { size: 12 }, stepSize: 1 },
          },
          x: {
            grid: { display: false },
            ticks: { font: { size: 12, weight: 'bold' } },
          },
        },
      },
    };

    this.statusChart = new Chart(canvas, config);
  }

  private animateKpi(field: 'kpiPending' | 'kpiApproved' | 'kpiRejected', to: number): void {
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

  getFileUrl(url: string | null): string {
    if (!url) return '';
    return `${environment.apiBaseUrl}${url}`;
  }

  private toMsg(err: unknown): string {
    const anyErr = err as any;
    return anyErr?.error?.message || anyErr?.message || 'Có lỗi xảy ra';
  }
}
