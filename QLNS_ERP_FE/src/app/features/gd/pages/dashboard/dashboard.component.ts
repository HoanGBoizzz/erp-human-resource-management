import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { DashboardApiService } from 'src/app/core/services/api/dashboard-api.service';
import { DirectorDashboardDto, isDirectorDashboard } from 'src/app/core/models/dashboard.model';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit, OnDestroy {
  loading = false;
  errorMsg = '';

  now = new Date();
  month = this.now.getMonth() + 1;
  year  = this.now.getFullYear();

  data: DirectorDashboardDto = {
    tongNhanVien: 0,
    nghiViecTrongThang: 0,
    tongLuongThang: 0,
    tongOtThang: 0,
    bangLuongChoDuyet: 0,
    tongDuAn: 0,
    duAnChoDuyet: 0,
    duAnDaDuyet: 0,
    duAnTuChoi: 0,
    nhatKyGanNhat: 0,
  };

  @ViewChild('barDuAn',   { static: false }) barDuAn!:   ElementRef<HTMLCanvasElement>;
  @ViewChild('barLuong',  { static: false }) barLuong!:  ElementRef<HTMLCanvasElement>;
  @ViewChild('pieStatus', { static: false }) pieStatus!: ElementRef<HTMLCanvasElement>;

  private c1?: Chart;
  private c2?: Chart;
  private c3?: Chart;

  constructor(private api: DashboardApiService) {}

  ngOnInit(): void { this.load(); }

  ngOnDestroy(): void { this.destroyCharts(); }

  load(): void {
    this.loading = true;
    this.errorMsg = '';
    this.api.getDashboard().subscribe({
      next: (res) => {
        if (!isDirectorDashboard(res)) {
          this.errorMsg = 'Bạn không có quyền xem Dashboard Giám đốc.';
          this.loading = false;
          return;
        }
        this.data = res.data;
        this.buildInsights();
        this.startCountUp();
        this.loading = false;
        setTimeout(() => this.renderCharts());
      },
      error: () => {
        this.errorMsg = 'Không tải được Dashboard Giám đốc.';
        this.loading = false;
      },
    });
  }

  private destroyCharts(): void {
    this.c1?.destroy(); this.c1 = undefined;
    this.c2?.destroy(); this.c2 = undefined;
    this.c3?.destroy(); this.c3 = undefined;
  }

  /** % dự án đã được xử lý (duyệt + từ chối so với tổng) */
  projectApprovalPercent(): number {
    const total = this.data.tongDuAn || 0;
    if (!total) return 0;
    return Math.round(((this.data.duAnDaDuyet + this.data.duAnTuChoi) / total) * 100);
  }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('vi-VN').format(v || 0);
  }

  // ===== Display (count-up animated) =====
  display = {
    tongNhanVien:       0,
    bangLuongChoDuyet:  0,
    duAnChoDuyet:       0,
    tongLuongThang:     0,
    tongOtThang:        0,
    nhatKyGanNhat:      0,
  };

  insights: Array<{
    icon: string;
    title: string;
    desc: string;
    tone: 'primary' | 'success' | 'warning' | 'danger' | 'info';
  }> = [];

  private animateValue(key: keyof typeof this.display, to: number, durationMs = 900): void {
    const from  = Number(this.display[key] || 0);
    const start = performance.now();
    const diff  = to - from;
    const step  = (now: number) => {
      const t      = Math.min(1, (now - start) / durationMs);
      const eased  = 1 - Math.pow(1 - t, 3);
      this.display[key] = Math.round(from + diff * eased);
      if (t < 1) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  }

  private startCountUp(): void {
    this.animateValue('tongNhanVien',      this.data.tongNhanVien);
    this.animateValue('bangLuongChoDuyet', this.data.bangLuongChoDuyet);
    this.animateValue('duAnChoDuyet',      this.data.duAnChoDuyet);
    this.animateValue('nhatKyGanNhat',     this.data.nhatKyGanNhat);
    this.animateValue('tongOtThang',       this.data.tongOtThang);
    this.animateValue('tongLuongThang',    this.data.tongLuongThang, 1100);
  }

  private buildInsights(): void {
    const totalDuAn  = this.data.tongDuAn || 0;
    const donePct    = totalDuAn ? Math.round((this.data.duAnDaDuyet / totalDuAn) * 100) : 0;
    const payrollTone: 'success' | 'warning' = this.data.bangLuongChoDuyet > 0 ? 'warning' : 'success';
    const churnTone:  'info' | 'success'     = this.data.nghiViecTrongThang > 0 ? 'info' : 'success';

    this.insights = [
      {
        icon: 'bi bi-cash-stack',
        title: `Tổng lương tháng: ${this.formatMoney(this.data.tongLuongThang)} đ`,
        desc:  `OT tháng này: ${this.data.tongOtThang} giờ.`,
        tone:  'primary',
      },
      {
        icon: 'bi bi-clipboard-check',
        title: `Bảng lương chờ duyệt: ${this.data.bangLuongChoDuyet}`,
        desc:  this.data.bangLuongChoDuyet > 0 ? 'Ưu tiên xử lý để kịp chi trả.' : 'Tình trạng ổn định.',
        tone:  payrollTone,
      },
      {
        icon: 'bi bi-kanban',
        title: `Dự án đã duyệt: ${donePct}%`,
        desc:  `Chờ duyệt ${this.data.duAnChoDuyet}  Từ chối ${this.data.duAnTuChoi}.`,
        tone:  this.data.duAnChoDuyet > 0 ? 'warning' : 'success',
      },
      {
        icon: 'bi bi-people',
        title: `Nhân sự: ${this.data.tongNhanVien} người`,
        desc:  `Nghỉ việc trong tháng: ${this.data.nghiViecTrongThang}.`,
        tone:  churnTone,
      },
    ];
  }

  private renderCharts(): void {
    this.destroyCharts();

    const colors = {
      primary: '#2563eb',
      green:   '#16a34a',
      orange:  '#ea580c',
      danger:  '#ef4444',
      gray:    '#94a3b8',
      purple:  '#7c3aed',
    };

    // Bar 1: Dự án
    const totalDuAn = this.data.duAnChoDuyet + this.data.duAnDaDuyet + this.data.duAnTuChoi;
    const cfg1: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: ['Chờ duyệt', 'Đã duyệt', 'Từ chối'],
        datasets: [{
          label: 'Dự án',
          data: [this.data.duAnChoDuyet, this.data.duAnDaDuyet, this.data.duAnTuChoi],
          backgroundColor: [colors.orange, colors.green, colors.danger],
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
            backgroundColor: 'rgba(0,0,0,0.8)',
            padding: 12,
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            footerFont: { size: 12, weight: 'bold' },
            displayColors: true,
            callbacks: {
              label: (ctx) => {
                const v = ctx.parsed.y ?? 0;
                const pct = totalDuAn > 0 ? ((v / totalDuAn) * 100).toFixed(1) : '0';
                return `${ctx.label}: ${v} dự án (${pct}%)`;
              },
              footer: () => `Tổng: ${totalDuAn} dự án`,
            },
          },
        },
        scales: {
          y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
          x: { grid: { display: false } },
        },
      },
    };
    this.c1 = new Chart(this.barDuAn.nativeElement, cfg1);

    // Bar 2: Bảng lương
    const luongDaXuLy = Math.max(0, (this.data.tongNhanVien || 0) - (this.data.bangLuongChoDuyet || 0));
    const totalLuong  = this.data.bangLuongChoDuyet + luongDaXuLy;
    const cfg2: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: ['Chờ duyệt', 'Đã xử lý'],
        datasets: [{
          label: 'Bảng lương',
          data: [this.data.bangLuongChoDuyet, luongDaXuLy],
          backgroundColor: [colors.orange, colors.primary],
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
            backgroundColor: 'rgba(0,0,0,0.8)',
            padding: 12,
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            footerFont: { size: 12, weight: 'bold' },
            displayColors: true,
            callbacks: {
              label: (ctx) => {
                const v = ctx.parsed.y ?? 0;
                const pct = totalLuong > 0 ? ((v / totalLuong) * 100).toFixed(1) : '0';
                return `${ctx.label}: ${v} bảng (${pct}%)`;
              },
              footer: () => `Tổng: ${totalLuong} bảng`,
            },
          },
        },
        scales: {
          y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
          x: { grid: { display: false } },
        },
      },
    };
    this.c2 = new Chart(this.barLuong.nativeElement, cfg2);

    // Pie: Tổng quan phê duyệt
    const cfg3: ChartConfiguration<'pie'> = {
      type: 'pie',
      data: {
        labels: ['DA đã duyệt', 'DA từ chối', 'DA chờ duyệt', 'Lương chờ duyệt', 'Lương đã xử lý'],
        datasets: [{
          data: [
            this.data.duAnDaDuyet,
            this.data.duAnTuChoi,
            this.data.duAnChoDuyet,
            this.data.bangLuongChoDuyet,
            luongDaXuLy,
          ],
          backgroundColor: [colors.green, colors.danger, colors.orange, '#f59e0b', colors.primary],
          borderWidth: 3,
          borderColor: '#ffffff',
          hoverBorderWidth: 4,
          hoverOffset: 8,
        }],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: {
            display: true,
            position: 'bottom',
            labels: {
              padding: 15,
              font: { size: 13, weight: 600 },
              color: '#1e293b',
              usePointStyle: true,
              pointStyle: 'circle',
            },
          },
          tooltip: {
            backgroundColor: 'rgba(0,0,0,0.8)',
            padding: 12,
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            displayColors: true,
            callbacks: {
              label: (ctx) => {
                const v     = ctx.parsed;
                const total = (ctx.dataset.data as number[]).reduce((a, b) => a + b, 0);
                const pct   = total > 0 ? ((v / total) * 100).toFixed(1) : '0';
                return ` ${ctx.label}: ${v} (${pct}%)`;
              },
            },
          },
        },
      },
    };
    if (this.pieStatus?.nativeElement) {
      this.c3 = new Chart(this.pieStatus.nativeElement, cfg3);
    }
  }
}
