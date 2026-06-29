import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { NoiLamViecApiService, ThongKeNoiLamViec } from 'src/app/core/services/api/noi-lam-viec-api.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import { ToastService } from 'src/app/shared/services/toast.service';

Chart.register(...registerables);

@Component({
  selector: 'app-noi-lam-viec-thong-ke',
  templateUrl: './noi-lam-viec-thong-ke.component.html',
  styleUrls: ['./noi-lam-viec-thong-ke.component.scss']
})
export class NoiLamViecThongKeComponent implements OnInit, OnDestroy {
  @ViewChild('barChart', { static: false }) barChartRef!: ElementRef<HTMLCanvasElement>;

  stats: ThongKeNoiLamViec = {
    tongDeXuat: 0, deXuatChoDuyet: 0, deXuatDaDuyet: 0, deXuatTuChoi: 0,
    tongTamUng: 0, tamUngChoDuyet: 0, tamUngDaDuyet: 0, tamUngTuChoi: 0,
    tongDiMuon: 0, diMuonChoDuyet: 0, diMuonDaDuyet: 0, diMuonTuChoi: 0
  };
  loading = true;
  private chart?: Chart;
  private entityUpdateSub?: Subscription;
  private notificationSub?: Subscription;

  constructor(
    private api: NoiLamViecApiService,
    private signalr: SignalrService,
    private toast: ToastService,
  ) { }

  ngOnInit(): void {
    this.loadStats();

    // Realtime: tự reload khi HR xử lý đơn
    this.entityUpdateSub = this.signalr.entityUpdate$.subscribe(update => {
      const nlvTypes = ['PhieuDeXuat', 'PhieuTamUng', 'DonDiMuon'];
      if (nlvTypes.includes(update.entityType)) {
        this.loadStats();
      }
    });

    // Realtime: thông báo khi HR duyệt/từ chối
    this.notificationSub = this.signalr.notification$.subscribe(n => {
      const nlvEntities = ['PhieuDeXuat', 'PhieuTamUng', 'DonDiMuon'];
      if (n.relatedEntity && nlvEntities.includes(n.relatedEntity)) {
        this.loadStats();
        if (n.type === 'SUCCESS') {
          this.toast.success(n.title);
        } else if (n.type === 'WARNING') {
          this.toast.warning(n.title);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
    this.entityUpdateSub?.unsubscribe();
    this.notificationSub?.unsubscribe();
  }

  loadStats(): void {
    this.api.getThongKe().subscribe({
      next: data => {
        this.stats = data;
        this.loading = false;
        setTimeout(() => this.renderChart(), 100);
      },
      error: () => { this.loading = false; }
    });
  }

  get totalAll(): number {
    return this.stats.tongDeXuat + this.stats.tongTamUng + this.stats.tongDiMuon;
  }

  get totalChoDuyet(): number {
    return this.stats.deXuatChoDuyet + this.stats.tamUngChoDuyet + this.stats.diMuonChoDuyet;
  }

  get totalDaDuyet(): number {
    return this.stats.deXuatDaDuyet + this.stats.tamUngDaDuyet + this.stats.diMuonDaDuyet;
  }

  get totalTuChoi(): number {
    return this.stats.deXuatTuChoi + this.stats.tamUngTuChoi + this.stats.diMuonTuChoi;
  }

  // Nested wrappers used by the HTML template (stats.deXuat.total etc.)
  get deXuat() {
    return {
      total: this.stats.tongDeXuat,
      choDuyet: this.stats.deXuatChoDuyet,
      daDuyet: this.stats.deXuatDaDuyet,
      tuChoi: this.stats.deXuatTuChoi
    };
  }

  get tamUng() {
    return {
      total: this.stats.tongTamUng,
      choDuyet: this.stats.tamUngChoDuyet,
      daDuyet: this.stats.tamUngDaDuyet,
      tuChoi: this.stats.tamUngTuChoi
    };
  }

  get diMuon() {
    return {
      total: this.stats.tongDiMuon,
      choDuyet: this.stats.diMuonChoDuyet,
      daDuyet: this.stats.diMuonDaDuyet,
      tuChoi: this.stats.diMuonTuChoi
    };
  }

  private renderChart(): void {
    if (!this.barChartRef?.nativeElement) return;
    this.chart?.destroy();

    const ctx = this.barChartRef.nativeElement.getContext('2d')!;
    const s = this.stats;

    const labels = ['Dụng cụ đề xuất', 'Tạm ứng', 'Đi muộn / Về sớm'];

    // Dataset colors
    const colorChoDuyet = { bg: 'rgba(245, 158, 11, 0.8)', border: '#f59e0b', hover: 'rgba(245, 158, 11, 1)' };
    const colorDaDuyet = { bg: 'rgba(22, 163, 74, 0.8)', border: '#16a34a', hover: 'rgba(22, 163, 74, 1)' };
    const colorTuChoi = { bg: 'rgba(239, 68, 68, 0.8)', border: '#ef4444', hover: 'rgba(239, 68, 68, 1)' };
    const colorTotal = { bg: 'rgba(37, 99, 235, 0.15)', border: '#2563eb', hover: 'rgba(37, 99, 235, 0.3)' };

    const config: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels,
        datasets: [
          {
            label: 'Tổng phiếu / đơn',
            data: [s.tongDeXuat, s.tongTamUng, s.tongDiMuon],
            backgroundColor: colorTotal.bg,
            borderColor: colorTotal.border,
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: colorTotal.hover,
          },
          {
            label: 'Chờ duyệt',
            data: [s.deXuatChoDuyet, s.tamUngChoDuyet, s.diMuonChoDuyet],
            backgroundColor: colorChoDuyet.bg,
            borderColor: colorChoDuyet.border,
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: colorChoDuyet.hover,
          },
          {
            label: 'Đã duyệt',
            data: [s.deXuatDaDuyet, s.tamUngDaDuyet, s.diMuonDaDuyet],
            backgroundColor: colorDaDuyet.bg,
            borderColor: colorDaDuyet.border,
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: colorDaDuyet.hover,
          },
          {
            label: 'Từ chối',
            data: [s.deXuatTuChoi, s.tamUngTuChoi, s.diMuonTuChoi],
            backgroundColor: colorTuChoi.bg,
            borderColor: colorTuChoi.border,
            borderWidth: 2,
            borderRadius: 6,
            hoverBackgroundColor: colorTuChoi.hover,
          }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: {
            position: 'top',
            labels: {
              padding: 16,
              font: { size: 12, weight: 'bold' },
              usePointStyle: true,
              pointStyleWidth: 10,
            }
          },
          tooltip: {
            backgroundColor: 'rgba(15, 23, 42, 0.92)',
            titleColor: '#f1f5f9',
            bodyColor: '#cbd5e1',
            borderColor: 'rgba(148, 163, 184, 0.2)',
            borderWidth: 1,
            padding: { top: 10, right: 14, bottom: 10, left: 14 },
            titleFont: { size: 13, weight: 'bold' },
            bodyFont: { size: 12 },
            cornerRadius: 10,
            callbacks: {
              title: (items) => `📋 ${items[0].label}`,
              label: (ctx) => {
                const val = ctx.parsed.y;
                const dataset = ctx.dataset.label || '';
                const icon = dataset.includes('Tổng') ? '📁'
                  : dataset.includes('Chờ') ? '⏳'
                    : dataset.includes('duyệt') ? '✅' : '❌';
                return `  ${icon} ${dataset}: ${val} phiếu/đơn`;
              },
              footer: (items) => {
                const total = items.reduce((sum, i) => {
                  if (!i.dataset.label?.includes('Tổng')) return sum + (i.parsed.y ?? 0);
                  return sum;
                }, 0);
                return [`─────────────────`, `  📊 Tổng trạng thái: ${total}`];
              }
            }
          }
        },
        scales: {
          x: {
            grid: { display: false },
            ticks: { font: { size: 12, weight: 'bold' }, color: '#334155' }
          },
          y: {
            beginAtZero: true,
            grid: { color: 'rgba(148, 163, 184, 0.15)' },
            ticks: {
              stepSize: 1,
              font: { size: 11 },
              color: '#64748b',
              callback: (v) => Number.isInteger(v) ? v : null
            }
          }
        }
      }
    };

    this.chart = new Chart(ctx, config);
  }
}