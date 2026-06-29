import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { finalize, Subscription } from 'rxjs';
import { DonPhepApiService } from 'src/app/core/services/api/don-phep-api.service';
import { DonPhepListItemDto, DonPhepDetailDto, DuyetDonPhepRequestDto, DonPhepTrangThai } from 'src/app/core/models/don-phep.model';
import { NotificationService } from 'src/app/core/services/notification.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';

Chart.register(...registerables);

@Component({
  selector: 'app-don-phep',
  templateUrl: './don-phep.component.html',
  styleUrls: ['./don-phep.component.scss']
})
export class DonPhepComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('pieChart') pieChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('barChart') barChartRef?: ElementRef<HTMLCanvasElement>;

  loadingList = false;
  loadingDetail = false;
  processing = false;
  errorMsg = '';

  list: DonPhepListItemDto[] = [];
  filtered: DonPhepListItemDto[] = [];
  detail?: DonPhepDetailDto;
  showDetailModal = false;
  showDuyetModal = false;

  keyword = '';
  statusFilter: DonPhepTrangThai | 'ALL' = 'ALL';
  pageIndex = 1;
  pageSize = 10;
  totalCount = 0;

  duyetForm: FormGroup;
  pieChart?: Chart;
  barChart?: Chart;

  // Thống kê
  stats = {
    choDuyet: 0,
    daDuyet: 0,
    tuChoi: 0,
    total: 0
  };

  // Thống kê theo tháng (12 tháng gần nhất)
  monthlyStats: { month: string; count: number }[] = [];

  // SignalR subscription
  private entityUpdateSub?: Subscription;

  constructor(
    private api: DonPhepApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private notificationService: NotificationService,
    private signalrService: SignalrService
  ) {
    this.duyetForm = this.fb.group({
      chapNhan: [true, Validators.required],
      lyDoTuChoi: ['']
    });
  }

  ngOnInit(): void {
    this.loadList();

    // Subscribe to realtime DON_PHEP updates (employee submits / status changes)
    this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
      if (update.entityType === 'DON_PHEP') {
        console.log('[HR-DonPhep] Realtime update received:', update);
        this.loadList();
      }
    });
  }

  ngAfterViewInit(): void {
    // Charts sẽ được render sau khi có dữ liệu
  }

  ngOnDestroy(): void {
    this.entityUpdateSub?.unsubscribe();
    this.destroyCharts();
  }

  loadList(triggeredByUser = false): void {
    this.loadingList = true;
    this.errorMsg = '';

    this.api.getList()
      .pipe(finalize(() => (this.loadingList = false)))
      .subscribe({
        next: (data) => {
          this.list = data || [];
          this.totalCount = this.list.length;
          this.applyFilters();
          this.calculateStats();
          this.calculateMonthlyStats();
          this.renderCharts();

          if (triggeredByUser) {
            this.toast.success('Tải dữ liệu thành công');
          }
        },
        error: (err) => {
          this.errorMsg = err?.error?.message || 'Không thể tải danh sách đơn phép';
          this.toast.danger('Lỗi: ' + this.errorMsg);
        }
      });
  }

  applyFilters(): void {
    let result = [...this.list];

    // Filter by keyword
    if (this.keyword.trim()) {
      const kw = this.keyword.toLowerCase();
      result = result.filter(x =>
        x.hoTen.toLowerCase().includes(kw) ||
        x.tenLoaiPhep.toLowerCase().includes(kw)
      );
    }

    // Filter by status
    if (this.statusFilter !== 'ALL') {
      result = result.filter(x => x.trangThai === this.statusFilter);
    }

    this.filtered = result;
    this.totalCount = result.length;
  }

  applyStatusFilter(): void {
    this.pageIndex = 1;
    this.applyFilters();
  }

  calculateStats(): void {
    this.stats.total = this.list.length;
    this.stats.choDuyet = this.list.filter(x => x.trangThai === 'CHO_DUYET').length;
    this.stats.daDuyet = this.list.filter(x => x.trangThai === 'DA_DUYET').length;
    this.stats.tuChoi = this.list.filter(x => x.trangThai === 'TU_CHOI').length;
  }

  calculateMonthlyStats(): void {
    const now = new Date();
    const monthMap = new Map<string, number>();

    // Tạo 12 tháng gần nhất
    for (let i = 11; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const key = `${d.getMonth() + 1}/${d.getFullYear()}`;
      monthMap.set(key, 0);
    }

    // Đếm đơn theo tháng
    this.list.forEach(item => {
      const date = new Date(item.tuNgay);
      const key = `${date.getMonth() + 1}/${date.getFullYear()}`;
      if (monthMap.has(key)) {
        monthMap.set(key, (monthMap.get(key) || 0) + 1);
      }
    });

    this.monthlyStats = Array.from(monthMap.entries()).map(([month, count]) => ({
      month,
      count
    }));
  }

  renderCharts(): void {
    setTimeout(() => {
      this.renderPieChart();
      this.renderBarChart();
    }, 100);
  }

  renderPieChart(): void {
    if (!this.pieChartRef?.nativeElement) return;

    if (this.pieChart) {
      this.pieChart.destroy();
    }

    const ctx = this.pieChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration<'doughnut'> = {
      type: 'doughnut',
      data: {
        labels: ['Chờ duyệt', 'Đã duyệt', 'Từ chối'],
        datasets: [{
          data: [this.stats.choDuyet, this.stats.daDuyet, this.stats.tuChoi],
          backgroundColor: ['#f59e0b', '#10b981', '#ef4444'],
          borderWidth: 0
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            position: 'bottom',
            labels: {
              padding: 16,
              font: { size: 12 }
            }
          },
          tooltip: {
            callbacks: {
              label: (context) => {
                const label = context.label || '';
                const value = context.parsed || 0;
                const total = this.stats.total;
                const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
                return `${label}: ${value} (${percent}%)`;
              }
            }
          }
        }
      }
    };

    this.pieChart = new Chart(ctx, config);
  }

  renderBarChart(): void {
    if (!this.barChartRef?.nativeElement) return;

    if (this.barChart) {
      this.barChart.destroy();
    }

    const ctx = this.barChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: this.monthlyStats.map(x => x.month),
        datasets: [{
          label: 'Số đơn',
          data: this.monthlyStats.map(x => x.count),
          backgroundColor: '#2563eb',
          borderRadius: 6
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (context) => `Số đơn: ${context.parsed.y}`
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { stepSize: 1 }
          }
        }
      }
    };

    this.barChart = new Chart(ctx, config);
  }

  destroyCharts(): void {
    if (this.pieChart) {
      this.pieChart.destroy();
      this.pieChart = undefined;
    }
    if (this.barChart) {
      this.barChart.destroy();
      this.barChart = undefined;
    }
  }

  openDetail(id: number): void {
    this.loadingDetail = true;
    this.showDetailModal = true;

    this.api.getDetail(id)
      .pipe(finalize(() => (this.loadingDetail = false)))
      .subscribe({
        next: (data) => {
          this.detail = data;
        },
        error: (err) => {
          this.toast.danger('Không thể tải chi tiết đơn phép');
          this.showDetailModal = false;
        }
      });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.detail = undefined;
  }

  openDuyetModal(): void {
    if (!this.detail) return;
    this.showDuyetModal = true;
    this.duyetForm.reset({ chapNhan: true, lyDoTuChoi: '' });
  }

  closeDuyetModal(): void {
    this.showDuyetModal = false;
    this.duyetForm.reset();
  }

  onChapNhanChange(): void {
    const chapNhan = this.duyetForm.get('chapNhan')?.value;
    if (chapNhan) {
      this.duyetForm.get('lyDoTuChoi')?.clearValidators();
      this.duyetForm.get('lyDoTuChoi')?.setValue('');
    } else {
      this.duyetForm.get('lyDoTuChoi')?.setValidators([Validators.required]);
    }
    this.duyetForm.get('lyDoTuChoi')?.updateValueAndValidity();
  }

  saveDuyet(): void {
    if (!this.detail || this.duyetForm.invalid) {
      this.toast.warning('Vui lòng nhập đầy đủ thông tin');
      return;
    }

    const formValue = this.duyetForm.value;
    const payload: DuyetDonPhepRequestDto = {
      donPhepId: this.detail.id,
      chapNhan: formValue.chapNhan,
      lyDoTuChoi: formValue.chapNhan ? null : formValue.lyDoTuChoi
    };

    this.processing = true;
    this.api.duyet(payload)
      .pipe(finalize(() => (this.processing = false)))
      .subscribe({
        next: () => {
          this.toast.success(
            formValue.chapNhan ? 'Duyệt đơn thành công' : 'Từ chối đơn thành công'
          );
          this.closeDuyetModal();
          this.closeDetail();
          this.loadList();
          this.notificationService.refresh();
        },
        error: (err) => {
          this.toast.danger(err?.error?.message || 'Không thể xử lý đơn phép');
        }
      });
  }

  getStatusBadge(status: DonPhepTrangThai): string {
    const map: Record<string, string> = {
      'CHO_DUYET': 'badge bg-warning text-dark',
      'DA_DUYET': 'badge bg-success',
      'TU_CHOI': 'badge bg-danger'
    };
    return map[status] || 'badge bg-secondary';
  }

  getStatusLabel(status: DonPhepTrangThai): string {
    const map: Record<string, string> = {
      'CHO_DUYET': 'Chờ duyệt',
      'DA_DUYET': 'Đã duyệt',
      'TU_CHOI': 'Từ chối'
    };
    return map[status] || status;
  }

  formatDate(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    return date.toLocaleDateString('vi-VN');
  }

  getTotalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  getPageNumbers(): number[] {
    const total = this.getTotalPages();
    if (total <= 5) return Array.from({ length: total }, (_, i) => i + 1);

    const current = this.pageIndex;
    const pages: number[] = [];

    if (current <= 3) {
      return [1, 2, 3, 4, 5];
    } else if (current >= total - 2) {
      return [total - 4, total - 3, total - 2, total - 1, total];
    } else {
      return [current - 2, current - 1, current, current + 1, current + 2];
    }
  }

  changePage(page: number): void {
    if (page < 1 || page > this.getTotalPages() || page === this.pageIndex) return;
    this.pageIndex = page;
  }

  get paginatedList(): DonPhepListItemDto[] {
    const start = (this.pageIndex - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.filtered.slice(start, end);
  }
}
