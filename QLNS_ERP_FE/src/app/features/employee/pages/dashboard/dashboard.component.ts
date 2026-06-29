import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { DashboardApiService } from 'src/app/core/services/api/dashboard-api.service';
import {
  EmployeeDashboardDto,
  isEmployeeDashboard
} from 'src/app/core/models/dashboard.model';

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
  year = this.now.getFullYear();

  data: EmployeeDashboardDto = {
    employeeId: 0,
    hoTen: '',
    soNgayChamCong: 0,
    tongOt: 0,
    soNgayVang: 0,
    tongLuong: 0,
    trangThaiLuong: 'CHUA_CO',
    donChoDuyet: 0,
    donDaDuyet: 0,
    donTuChoi: 0,
    soDuAnThamGia: 0,
  };

  @ViewChild('barChamCong', { static: false }) barChamCong!: ElementRef<HTMLCanvasElement>;
  @ViewChild('barDonPhep', { static: false }) barDonPhep!: ElementRef<HTMLCanvasElement>;
  @ViewChild('pieOverview', { static: false }) pieOverview!: ElementRef<HTMLCanvasElement>;

  private c1?: Chart;
  private c2?: Chart;
  private c3?: Chart;

  constructor(private api: DashboardApiService) { }

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  load(): void {
    this.loading = true;
    this.errorMsg = '';

    this.api.getDashboard().subscribe({
      next: (res) => {
        if (!isEmployeeDashboard(res)) {
          this.errorMsg = 'Bạn không có quyền xem dashboard nhân viên.';
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
        this.errorMsg = 'Không tải được dashboard nhân viên.';
        this.loading = false;
      }
    });
  }

  private destroyCharts(): void {
    this.c1?.destroy(); this.c1 = undefined;
    this.c2?.destroy(); this.c2 = undefined;
    this.c3?.destroy(); this.c3 = undefined;
  }

  attendancePercent(): number {
    const total = this.data.soNgayChamCong + this.data.soNgayVang;
    if (!total) return 0;
    return Math.round((this.data.soNgayChamCong / total) * 100);
  }

  formatMoney(v: number): string {
    return new Intl.NumberFormat('vi-VN').format(v || 0);
  }

  salaryStatusText(st: string): string {
    switch (st) {
      case 'DA_KHOA': return 'Đã khoá';
      case 'CHO_DUYET': return 'Chờ duyệt';
      case 'CAN_TINH_LAI': return 'Cần tính lại';
      case 'CHUA_CO': return 'Chưa có';
      default: return st || '-';
    }
  }

  salaryBadgeClass(st: string): string {
    switch (st) {
      case 'DA_KHOA': return 'text-bg-success';
      case 'CHO_DUYET': return 'text-bg-warning';
      case 'CAN_TINH_LAI': return 'text-bg-danger';
      case 'CHUA_CO': return 'text-bg-secondary';
      default: return 'text-bg-secondary';
    }
  }

  private renderCharts(): void {
    this.destroyCharts();

    // Define colors
    const colors = {
      primary: '#2563eb',
      purple: '#7c3aed',
      orange: '#ea580c',
      green: '#16a34a',
      danger: '#ef4444',
      gray: '#94a3b8',
    };

    // Bar 1: Chấm công (với tooltip nâng cao)
    const totalAttendance = this.data.soNgayChamCong + this.data.soNgayVang;
    const cfg1: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: ['Đi làm', 'Vắng'],
        datasets: [{
          label: 'Ngày',
          data: [this.data.soNgayChamCong, this.data.soNgayVang],
          backgroundColor: [colors.primary, colors.gray],
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
            callbacks: {
              label: (context) => {
                const value = context.parsed.y ?? 0;
                const percent = totalAttendance > 0
                  ? ((value / totalAttendance) * 100).toFixed(1)
                  : '0';
                return `${context.label}: ${value} ngày (${percent}%)`;
              },
              footer: () => {
                return `Tổng: ${totalAttendance} ngày`;
              }
            }
          }
        },
        scales: {
          y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
          x: { grid: { display: false } }
        }
      },
    };
    this.c1 = new Chart(this.barChamCong.nativeElement, cfg1);

    // Bar 2: Đơn phép (với tooltip nâng cao)
    const totalDon = this.data.donChoDuyet + this.data.donDaDuyet + this.data.donTuChoi;
    const cfg2: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: ['Chờ duyệt', 'Đã duyệt', 'Từ chối'],
        datasets: [{
          label: 'Số đơn',
          data: [this.data.donChoDuyet, this.data.donDaDuyet, this.data.donTuChoi],
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
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            padding: 12,
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            footerFont: { size: 12, weight: 'bold' },
            displayColors: true,
            callbacks: {
              label: (context) => {
                const value = context.parsed.y ?? 0;
                const percent = totalDon > 0
                  ? ((value / totalDon) * 100).toFixed(1)
                  : '0';
                return `${context.label}: ${value} đơn (${percent}%)`;
              },
              footer: () => {
                return `Tổng: ${totalDon} đơn`;
              }
            }
          }
        },
        scales: {
          y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
          x: { grid: { display: false } }
        }
      },
    };
    this.c2 = new Chart(this.barDonPhep.nativeElement, cfg2);

    // Pie Chart: Tổng quan tháng (OT, Dự án, Đơn phép)
    const cfg3: ChartConfiguration<'pie'> = {
      type: 'pie',
      data: {
        labels: ['OT (giờ)', 'Dự án', 'Đơn phép'],
        datasets: [
          {
            data: [this.data.tongOt, this.data.soDuAnThamGia, totalDon],
            backgroundColor: [colors.purple, colors.primary, colors.orange],
            borderWidth: 3,
            borderColor: '#ffffff',
            hoverBorderWidth: 4,
            hoverOffset: 8,
          },
        ],
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
            }
          },
          tooltip: {
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            padding: 12,
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            displayColors: true,
            callbacks: {
              label: (context) => {
                const value = context.parsed;
                const total = this.data.tongOt + this.data.soDuAnThamGia + totalDon;
                const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
                return `${context.label}: ${value} (${percent}%)`;
              }
            }
          }
        },
      },
    };
    this.c3 = new Chart(this.pieOverview.nativeElement, cfg3);
  }
  // ===== ADD: UI display for count-up (numbers animate) =====
  display = {
    soNgayChamCong: 0,
    tongOt: 0,
    soNgayVang: 0,
    tongLuong: 0,
    donChoDuyet: 0,
    soDuAnThamGia: 0,
  };

  insights: Array<{ icon: string; title: string; desc: string; tone: 'primary' | 'success' | 'warning' | 'danger' | 'info' }> = [];
  // ===== ADD: count-up helper =====
  private animateValue(
    key: keyof typeof this.display,
    to: number,
    durationMs = 900
  ): void {
    const from = Number(this.display[key] || 0);
    const start = performance.now();
    const diff = to - from;

    const step = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs);
      // easing
      const eased = 1 - Math.pow(1 - t, 3);
      this.display[key] = Math.round(from + diff * eased);

      if (t < 1) requestAnimationFrame(step);
    };

    requestAnimationFrame(step);
  }

  private startCountUp(): void {
    this.animateValue('soNgayChamCong', this.data.soNgayChamCong);
    this.animateValue('soNgayVang', this.data.soNgayVang);
    this.animateValue('tongOt', this.data.tongOt);
    this.animateValue('donChoDuyet', this.data.donChoDuyet);
    this.animateValue('soDuAnThamGia', this.data.soDuAnThamGia);
    this.animateValue('tongLuong', this.data.tongLuong, 1100);
  }
  // ===== ADD: insights (soft) =====
  private buildInsights(): void {
    const totalAttend = this.data.soNgayChamCong + this.data.soNgayVang;
    const attendPct = totalAttend ? Math.round((this.data.soNgayChamCong / totalAttend) * 100) : 0;

    const salaryText = this.salaryStatusText(this.data.trangThaiLuong);
    const salaryTone =
      this.data.trangThaiLuong === 'DA_KHOA' ? 'success'
        : this.data.trangThaiLuong === 'CHO_DUYET' ? 'warning'
          : this.data.trangThaiLuong === 'CAN_TINH_LAI' ? 'danger'
            : 'info';

    const otTone =
      this.data.tongOt >= 30 ? 'warning'
        : this.data.tongOt >= 10 ? 'info'
          : 'success';

    this.insights = [
      {
        icon: 'bi bi-percent',
        title: `Bạn đã đi làm ${attendPct}% tháng này`,
        desc: `Đi làm ${this.data.soNgayChamCong} ngày · Vắng ${this.data.soNgayVang} ngày.`,
        tone: attendPct >= 90 ? 'success' : attendPct >= 75 ? 'info' : 'warning',
      },
      {
        icon: 'bi bi-lightning-charge',
        title: `OT hiện tại: ${this.data.tongOt} giờ`,
        desc: this.data.tongOt > 0 ? 'Duy trì sức khoẻ và cân bằng nhé.' : 'Chưa có OT trong tháng.',
        tone: otTone,
      },
      {
        icon: 'bi bi-receipt',
        title: `Trạng thái lương: ${salaryText}`,
        desc: `Tổng lương tạm tính: ${this.formatMoney(this.data.tongLuong)}.`,
        tone: salaryTone,
      },
      {
        icon: 'bi bi-umbrella',
        title: `Đơn nghỉ phép chờ duyệt: ${this.data.donChoDuyet}`,
        desc: `Đã duyệt ${this.data.donDaDuyet} · Từ chối ${this.data.donTuChoi}.`,
        tone: this.data.donChoDuyet > 0 ? 'warning' : 'success',
      },
    ];
  }
}
