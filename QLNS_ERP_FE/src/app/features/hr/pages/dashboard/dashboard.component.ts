import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { DashboardApiService } from 'src/app/core/services/api/dashboard-api.service';
import { HrDashboardDto, isHrDashboard } from 'src/app/core/models/dashboard.model';

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

  data: HrDashboardDto = {
    tongNhanVien: 0, dangLam: 0, daNghi: 0,
    tongBangCong: 0, dangNhapLieu: 0, daChotCong: 0,
    choDuyet: 0, daDuyet: 0, tuChoi: 0,
    canTinh: 0, choDuyetLuong: 0, daKhoa: 0,
    deXuatChoDuyet: 0, deXuatDuyet: 0, deXuatTuChoi: 0
  };

  @ViewChild('barNhanSu', { static: false }) barNhanSu!: ElementRef<HTMLCanvasElement>;
  @ViewChild('barPhep', { static: false }) barPhep!: ElementRef<HTMLCanvasElement>;
  @ViewChild('pieModules', { static: false }) pieModules!: ElementRef<HTMLCanvasElement>;


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
        if (!isHrDashboard(res)) {
          this.errorMsg = 'Bạn không có quyền xem Dashboard HR.';
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
        this.errorMsg = 'Không tải được Dashboard HR.';
        this.loading = false;
      }
    });
  }

  private destroyCharts(): void {
    this.c1?.destroy(); this.c1 = undefined;
    this.c2?.destroy(); this.c2 = undefined;
    this.c3?.destroy(); this.c3 = undefined;
  }

  private renderCharts(): void {
    this.destroyCharts();

    const colors = {
      primary: '#2563eb',
      green: '#16a34a',
      orange: '#ea580c',
      danger: '#ef4444',
      gray: '#94a3b8',
      purple: '#7c3aed',
    };

    // Bar 1: Nhân sự (với tooltip nâng cao)
    const totalNhanSu = this.data.dangLam + this.data.daNghi;
    const cfg1: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: ['Đang làm', 'Đã nghỉ'],
        datasets: [{
          label: 'Nhân viên',
          data: [this.data.dangLam, this.data.daNghi],
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
                const percent = totalNhanSu > 0 
                  ? ((value / totalNhanSu) * 100).toFixed(1) 
                  : '0';
                return `${context.label}: ${value} người (${percent}%)`;
              },
              footer: () => {
                return `Tổng: ${totalNhanSu} người`;
              }
            }
          }
        },
        scales: {
          y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
          x: { grid: { display: false } }
        }
      }
    };
    this.c1 = new Chart(this.barNhanSu.nativeElement, cfg1);

    // Bar 2: Đơn phép (với tooltip nâng cao)
    const totalDon = this.data.choDuyet + this.data.daDuyet + this.data.tuChoi;
    const cfg2: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: ['Chờ duyệt', 'Đã duyệt', 'Từ chối'],
        datasets: [{
          label: 'Số đơn',
          data: [this.data.choDuyet, this.data.daDuyet, this.data.tuChoi],
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
      }
    };
    this.c2 = new Chart(this.barPhep.nativeElement, cfg2);

    // Pie Chart: Tổng hợp module
    const totalTasks = this.data.dangNhapLieu + this.data.choDuyetLuong + this.data.deXuatChoDuyet +
                       this.data.daChotCong + this.data.daKhoa + this.data.deXuatDuyet +
                       this.data.canTinh + this.data.deXuatTuChoi;
    
    const cfg3: ChartConfiguration<'pie'> = {
      type: 'pie',
      data: {
        labels: ['Bảng công', 'Bảng lương', 'Đề xuất lương'],
        datasets: [{
          data: [
            this.data.dangNhapLieu + this.data.daChotCong,
            this.data.choDuyetLuong + this.data.daKhoa + this.data.canTinh,
            this.data.deXuatChoDuyet + this.data.deXuatDuyet + this.data.deXuatTuChoi
          ],
          backgroundColor: [colors.green, colors.primary, colors.orange],
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
                const total = totalTasks;
                const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
                return `${context.label}: ${value} (${percent}%)`;
              }
            }
          }
        },
      }
    };
    this.c3 = new Chart(this.pieModules.nativeElement, cfg3);
  }
  display = {
    tongNhanVien: 0,
    choDuyetLuong: 0,
    choDuyet: 0,
    dangNhapLieu: 0,
  };

  insights: Array<{ icon: string; title: string; desc: string; tone: 'primary' | 'success' | 'warning' | 'danger' | 'info' }> = [];
  private animateValue(key: keyof typeof this.display, to: number, durationMs = 900): void {
    const from = Number(this.display[key] || 0);
    const start = performance.now();
    const diff = to - from;

    const step = (now: number) => {
      const t = Math.min(1, (now - start) / durationMs);
      const eased = 1 - Math.pow(1 - t, 3);
      this.display[key] = Math.round(from + diff * eased);
      if (t < 1) requestAnimationFrame(step);
    };
    requestAnimationFrame(step);
  }

  private startCountUp(): void {
    this.animateValue('tongNhanVien', this.data.tongNhanVien);
    this.animateValue('choDuyetLuong', this.data.choDuyetLuong);
    this.animateValue('choDuyet', this.data.choDuyet);
    this.animateValue('dangNhapLieu', this.data.dangNhapLieu);
  }
  private buildInsights(): void {
    const leaveTone = this.data.choDuyet > 0 ? 'warning' : 'success';
    const payrollTone =
      this.data.choDuyetLuong > 0 ? 'warning' : this.data.canTinh > 0 ? 'danger' : 'success';

    this.insights = [
      {
        icon: 'bi bi-people',
        title: `Nhân sự đang làm: ${this.data.dangLam}/${this.data.tongNhanVien}`,
        desc: `Đã nghỉ: ${this.data.daNghi}.`,
        tone: 'primary',
      },
      {
        icon: 'bi bi-umbrella',
        title: `Đơn phép chờ duyệt: ${this.data.choDuyet}`,
        desc: `Đã duyệt ${this.data.daDuyet} · Từ chối ${this.data.tuChoi}.`,
        tone: leaveTone,
      },
      {
        icon: 'bi bi-calendar2-week',
        title: `Bảng công đang nhập liệu: ${this.data.dangNhapLieu}`,
        desc: `Đã chốt công: ${this.data.daChotCong}.`,
        tone: this.data.dangNhapLieu > 0 ? 'info' : 'success',
      },
      {
        icon: 'bi bi-receipt',
        title: `Lương chờ duyệt: ${this.data.choDuyetLuong}`,
        desc: `Cần tính lại: ${this.data.canTinh} · Đã khoá: ${this.data.daKhoa}.`,
        tone: payrollTone,
      },
    ];
  }

}
