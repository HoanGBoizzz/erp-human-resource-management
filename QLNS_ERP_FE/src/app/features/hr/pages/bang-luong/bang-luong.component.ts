import { Component, OnInit, ViewChild, ElementRef, AfterViewInit, OnDestroy } from '@angular/core';
import * as ExcelJS from 'exceljs';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Chart, ChartConfiguration, registerables } from 'chart.js';
import { finalize, forkJoin } from 'rxjs';
import { LuongApiService } from 'src/app/core/services/api/luong-api.service';
import {
  BangLuongThangDetailDto,
  BangLuongThangListItemDto,
  GuiDuyetLuongRequestDto,
  DuyetLuongRequestDto,
  TrangThaiLuong,
  LuongThongKeTrangThaiDto,
  LuongTongLuongThangDto
} from 'src/app/core/models/luong.model';
import { ToastService } from 'src/app/shared/services/toast.service';

Chart.register(...registerables);

@Component({
  selector: 'app-bang-luong',
  templateUrl: './bang-luong.component.html',
  styleUrls: ['./bang-luong.component.scss']
})
export class BangLuongComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('pieChart') pieChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('barChart') barChartRef?: ElementRef<HTMLCanvasElement>;
  @ViewChild('lineChart') lineChartRef?: ElementRef<HTMLCanvasElement>;

  loadingList = false;
  loadingDetail = false;
  loadingCharts = false;
  processing = false;
  errorMsg = '';

  list: BangLuongThangListItemDto[] = [];
  filtered: BangLuongThangListItemDto[] = [];
  detail?: BangLuongThangDetailDto;

  showDetailModal = false;
  showGuiDuyetModal = false;

  keyword = '';
  statusFilter: TrangThaiLuong | 'ALL' = 'ALL';
  thangFilter: number | 'ALL' = 'ALL';
  namFilter: number = new Date().getFullYear();

  pageIndex = 1;
  pageSize = 10;
  totalCount = 0;

  guiDuyetForm: FormGroup;

  pieChart?: Chart;
  barChart?: Chart;
  lineChart?: Chart;

  // Thống kê
  stats = {
    tamTinh: 0,
    choDuyet: 0,
    daDuyet: 0,
    tuChoi: 0,
    daKhoa: 0,
    khac: 0,
    total: 0,
    tongLuong: 0
  };

  // Thống kê tổng lương theo trạng thái (từ API tong-luong-thang)
  tongLuongThang: LuongTongLuongThangDto | null = null;

  // Chọn tháng/năm cho card tổng hợp lương
  selectedThangTongHop: number = new Date().getMonth() + 1;
  selectedNamTongHop: number = new Date().getFullYear();
  loadingTongHop = false;

  // Modal gửi duyệt hàng loạt
  showGuiDuyetTatCaModal = false;
  processingBulk = false;
  bulkProgress = { current: 0, total: 0, success: 0, failed: 0 };

  // Modal thu hồi hàng loạt
  showThuHoiTatCaModal = false;

  // ─── Excel Export Modal ───────────────────────────────────────────────────
  showExportModal = false;
  exportAllEmployees = true;
  exportForm = {
    thang: new Date().getMonth() + 1,
    nam: new Date().getFullYear(),
    nvHoSoId: null as number | null
  };

  exportEmployeeList: { id: number; hoTen: string; maNhanVien: string }[] = [];

  thuHoiForm: FormGroup;
  selectedBangLuongId: number | null = null;

  // Thống kê theo tháng (12 tháng)
  monthlyStats: { month: string; tongLuong: number }[] = [];

  constructor(
    private api: LuongApiService,
    private toast: ToastService,
    private fb: FormBuilder
  ) {
    this.guiDuyetForm = this.fb.group({
      ghiChu: ['']
    });

    this.thuHoiForm = this.fb.group({
      lyDo: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadList();
  }

  ngAfterViewInit(): void {
    // Charts sẽ render sau khi có dữ liệu
  }

  ngOnDestroy(): void {
    this.destroyCharts();
  }

  loadList(triggeredByUser = false): void {
    this.loadingList = true;
    this.errorMsg = '';

    this.api.getList()
      .pipe(finalize(() => (this.loadingList = false)))
      .subscribe({
        next: (data: BangLuongThangListItemDto[]) => {
          this.list = data || [];
          this.totalCount = this.list.length;
          this.applyFilters();

          // Load chart data từ API
          this.loadChartData();

          if (triggeredByUser) {
            this.toast.success('Tải dữ liệu thành công');
          }
        },
        error: (err: any) => {
          this.errorMsg = err?.error?.message || 'Không thể tải danh sách bảng lương';
          this.toast.danger('Lỗi: ' + this.errorMsg);
        }
      });
  }

  /**
   * Load dữ liệu biểu đồ từ API
   */
  loadChartData(): void {
    this.loadingCharts = true;
    const now = new Date();
    const currentMonth = now.getMonth() + 1;
    const currentYear = now.getFullYear();

    // Tạo danh sách 12 tháng gần nhất
    const monthsToLoad: { thang: number; nam: number }[] = [];
    for (let i = 11; i >= 0; i--) {
      const d = new Date(currentYear, currentMonth - 1 - i, 1);
      monthsToLoad.push({ thang: d.getMonth() + 1, nam: d.getFullYear() });
    }

    // Gọi API thống kê trạng thái cho tháng/năm đã chọn (pie chart)
    // và API tổng lương cho 12 tháng (bar chart)
    const statsRequest = this.api.getThongKeTrangThai(this.selectedThangTongHop, this.selectedNamTongHop);
    const monthlyRequests = monthsToLoad.map(m => this.api.getTongLuongThang(m.thang, m.nam));

    forkJoin([statsRequest, ...monthlyRequests])
      .pipe(finalize(() => (this.loadingCharts = false)))
      .subscribe({
        next: ([statsData, ...monthlyData]) => {
          // Cập nhật stats cho pie chart
          const statsDto = statsData as LuongThongKeTrangThaiDto;
          this.stats.tamTinh = statsDto?.tamTinh || 0;
          this.stats.choDuyet = statsDto?.choDuyet || 0;
          this.stats.daDuyet = statsDto?.daDuyet || 0;
          this.stats.tuChoi = statsDto?.tuChoi || 0;
          this.stats.daKhoa = statsDto?.daKhoa || 0;
          this.stats.khac = statsDto?.khac || 0;
          this.stats.total = statsDto?.tong || 0;

          // Cập nhật monthly stats cho bar chart
          this.monthlyStats = (monthlyData as LuongTongLuongThangDto[]).map(item => ({
            month: `${item.thang}/${item.nam}`,
            tongLuong: item.tongLuongDaDuyet // Chỉ lấy lương đã duyệt
          }));

          // Lưu thống kê tổng lương tháng hiện tại (item cuối cùng)
          const currentMonthData = monthlyData[monthlyData.length - 1] as LuongTongLuongThangDto;
          this.tongLuongThang = currentMonthData;

          // Tính tổng lương
          this.stats.tongLuong = this.monthlyStats.reduce((sum, x) => sum + x.tongLuong, 0);

          // Render charts
          this.renderCharts();
        },
        error: (err: any) => {
          console.error('Lỗi tải dữ liệu biểu đồ:', err);
          // Fallback: tính từ list nếu API lỗi
          this.calculateStatsFromList();
          this.calculateMonthlyStatsFromList();
          this.renderCharts();
        }
      });
  }

  /**
   * Load riêng dữ liệu pie chart theo tháng/năm đã chọn
   */
  loadPieChartData(): void {
    this.api.getThongKeTrangThai(this.selectedThangTongHop, this.selectedNamTongHop)
      .subscribe({
        next: (statsDto: LuongThongKeTrangThaiDto) => {
          this.stats.tamTinh = statsDto?.tamTinh || 0;
          this.stats.choDuyet = statsDto?.choDuyet || 0;
          this.stats.daDuyet = statsDto?.daDuyet || 0;
          this.stats.tuChoi = statsDto?.tuChoi || 0;
          this.stats.daKhoa = statsDto?.daKhoa || 0;
          this.stats.khac = statsDto?.khac || 0;
          this.stats.total = statsDto?.tong || 0;

          // Render lại pie chart
          this.renderPieChart();
        },
        error: (err: any) => {
          console.error('Lỗi tải thống kê trạng thái:', err);
          // Fallback: tính từ list
          this.calculateStatsFromList();
          this.renderPieChart();
        }
      });
  }

  /**
   * Fallback: Tính stats từ list khi API lỗi
   */
  calculateStatsFromList(): void {
    this.stats.total = this.list.length;
    this.stats.tamTinh = this.list.filter(x => x.trangThai === 'TAM_TINH').length;
    this.stats.choDuyet = this.list.filter(x => x.trangThai === 'CHO_DUYET_GIAM_DOC').length;
    this.stats.daDuyet = this.list.filter(x => x.trangThai === 'DA_DUYET').length;
    this.stats.tuChoi = this.list.filter(x => x.trangThai === 'TU_CHOI').length;
    this.stats.daKhoa = this.list.filter(x => x.trangThai === 'DA_KHOA').length;
    this.stats.tongLuong = this.list
      .filter(x => x.trangThai === 'DA_DUYET')
      .reduce((sum, x) => sum + x.tongLuong, 0);
  }

  /**
   * Fallback: Tính monthly stats từ list khi API lỗi
   */
  calculateMonthlyStatsFromList(): void {
    const now = new Date();
    const monthMap = new Map<string, number>();

    for (let i = 11; i >= 0; i--) {
      const d = new Date(now.getFullYear(), now.getMonth() - i, 1);
      const key = `${d.getMonth() + 1}/${d.getFullYear()}`;
      monthMap.set(key, 0);
    }

    this.list
      .filter(x => x.trangThai === 'DA_DUYET')
      .forEach(item => {
        const key = `${item.thang}/${item.nam}`;
        if (monthMap.has(key)) {
          monthMap.set(key, (monthMap.get(key) || 0) + item.tongLuong);
        }
      });

    this.monthlyStats = Array.from(monthMap.entries()).map(([month, tongLuong]) => ({
      month,
      tongLuong
    }));
  }

  applyFilters(): void {
    let result = [...this.list];

    // Filter by keyword
    if (this.keyword.trim()) {
      const kw = this.keyword.toLowerCase();
      result = result.filter(x => x.hoTen.toLowerCase().includes(kw));
    }

    // Filter by status
    if (this.statusFilter !== 'ALL') {
      result = result.filter(x => x.trangThai === this.statusFilter);
    }

    // Filter by month
    if (this.thangFilter !== 'ALL') {
      result = result.filter(x => x.thang === this.thangFilter);
    }

    // Filter by year
    result = result.filter(x => x.nam === this.namFilter);

    this.filtered = result;
    this.totalCount = result.length;
  }

  applyStatusFilter(): void {
    this.pageIndex = 1;
    this.applyFilters();
  }

  renderCharts(): void {
    setTimeout(() => {
      this.renderPieChart();
      this.renderBarChart();
      this.renderLineChart();
    }, 100);
  }

  renderPieChart(): void {
    if (!this.pieChartRef?.nativeElement) return;

    if (this.pieChart) {
      this.pieChart.destroy();
    }

    // Không render nếu không có dữ liệu
    if (this.stats.total === 0) {
      return;
    }

    const ctx = this.pieChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration<'doughnut'> = {
      type: 'doughnut',
      data: {
        labels: ['Tạm tính', 'Chờ duyệt', 'Đã duyệt', 'Từ chối', 'Đã khóa'],
        datasets: [{
          data: [
            this.stats.tamTinh,
            this.stats.choDuyet,
            this.stats.daDuyet,
            this.stats.tuChoi,
            this.stats.daKhoa
          ],
          backgroundColor: ['#94a3b8', '#f59e0b', '#10b981', '#ef4444', '#1e293b'],
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

    // Không render nếu không có dữ liệu
    if (this.monthlyStats.length === 0 || this.monthlyStats.every(m => m.tongLuong === 0)) {
      return;
    }

    const ctx = this.barChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    const config: ChartConfiguration<'bar'> = {
      type: 'bar',
      data: {
        labels: this.monthlyStats.map(x => x.month),
        datasets: [{
          label: 'Tổng lương (VNĐ)',
          data: this.monthlyStats.map(x => x.tongLuong),
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
              label: (context) => `Lương: ${this.formatCurrency(context.parsed.y || 0)}`
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: {
              callback: (value) => this.formatCurrency(Number(value))
            }
          }
        }
      }
    };

    this.barChart = new Chart(ctx, config);
  }

  renderLineChart(): void {
    if (!this.lineChartRef?.nativeElement) return;

    if (this.lineChart) {
      this.lineChart.destroy();
    }

    // Không render nếu không có dữ liệu
    if (this.monthlyStats.length === 0 || this.monthlyStats.every(m => m.tongLuong === 0)) {
      return;
    }

    const ctx = this.lineChartRef.nativeElement.getContext('2d');
    if (!ctx) return;

    // Calculate total for percentage in tooltip
    const totalSalary = this.monthlyStats.reduce((sum, item) => sum + item.tongLuong, 0);

    const config: ChartConfiguration<'line'> = {
      type: 'line',
      data: {
        labels: this.monthlyStats.map(x => x.month),
        datasets: [{
          label: 'Tổng lương (VNĐ)',
          data: this.monthlyStats.map(x => x.tongLuong),
          borderColor: '#2563eb',
          backgroundColor: 'rgba(37, 99, 235, 0.1)',
          borderWidth: 2,
          fill: true,
          tension: 0.4, // Smooth curve (natural)
          pointRadius: 4,
          pointBorderWidth: 2,
          pointBackgroundColor: '#2563eb',
          pointBorderColor: '#ffffff',
          pointHoverRadius: 6,
          pointHoverBorderWidth: 2,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(0, 0, 0, 0.8)',
            padding: { top: 12, right: 16, bottom: 12, left: 16 },
            titleFont: { size: 14, weight: 'bold' },
            bodyFont: { size: 13 },
            footerFont: { size: 12, weight: 'bold' },
            displayColors: true,
            borderColor: '#ffffff',
            borderWidth: 1,
            titleSpacing: 8,
            bodySpacing: 6,
            footerSpacing: 8,
            callbacks: {
              label: (context) => {
                const value = context.parsed.y || 0;
                const percent = totalSalary > 0
                  ? ((value / totalSalary) * 100).toFixed(1)
                  : '0';
                return `Lương: ${this.formatCurrency(value)} (${percent}%)`;
              },
              footer: () => {
                return `Tổng 12 tháng: ${this.formatCurrency(totalSalary)}`;
              }
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            grid: {
              color: '#f1f5f9',
            },
            ticks: {
              font: { size: 12 },
              callback: (value) => this.formatCurrency(Number(value))
            },
            border: { display: false }
          },
          x: {
            grid: { display: false },
            ticks: {
              font: { size: 12, weight: 'bold' }
            }
          }
        },
        interaction: {
          intersect: false,
          mode: 'index'
        }
      }
    };

    this.lineChart = new Chart(ctx, config);
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
    if (this.lineChart) {
      this.lineChart.destroy();
      this.lineChart = undefined;
    }
  }

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
          this.toast.danger('Không thể tải chi tiết bảng lương');
          this.showDetailModal = false;
        }
      });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.detail = undefined;
  }

  openGuiDuyetModal(): void {
    if (!this.detail) return;
    this.showGuiDuyetModal = true;
    this.guiDuyetForm.reset({ ghiChu: '' });
  }

  closeGuiDuyetModal(): void {
    this.showGuiDuyetModal = false;
    this.guiDuyetForm.reset();
  }

  saveGuiDuyet(): void {
    if (!this.detail) return;

    const payload: GuiDuyetLuongRequestDto = this.guiDuyetForm.value;
    this.processing = true;

    this.api.guiDuyet(this.detail.id, payload)
      .pipe(finalize(() => (this.processing = false)))
      .subscribe({
        next: () => {
          this.toast.success('Gửi duyệt giám đốc thành công');
          this.closeGuiDuyetModal();
          this.closeDetail();
          this.loadList();
          this.loadTongHopThang();
        },
        error: (err: any) => {
          this.toast.danger(err?.error?.message || 'Không thể gửi duyệt');
        }
      });
  }

  // ========== GỬI DUYỆT HÀNG LOẠT ==========

  /**
   * Load thống kê tổng hợp theo tháng đã chọn
   */
  loadTongHopThang(): void {
    this.loadingTongHop = true;

    this.api.getTongLuongThang(this.selectedThangTongHop, this.selectedNamTongHop)
      .pipe(finalize(() => (this.loadingTongHop = false)))
      .subscribe({
        next: (data: LuongTongLuongThangDto) => {
          this.tongLuongThang = data;
        },
        error: (err: any) => {
          console.error('Lỗi tải thống kê tổng hợp:', err);
          this.toast.danger('Không thể tải thống kê tháng ' + this.selectedThangTongHop + '/' + this.selectedNamTongHop);
        }
      });
  }

  /**
   * Khi thay đổi tháng/năm trong card tổng hợp
   */
  onThangTongHopChange(): void {
    this.loadTongHopThang();
    // Load lại pie chart theo tháng/năm mới
    this.loadPieChartData();
  }

  /**
   * Lấy số bảng lương tạm tính theo tháng đã chọn
   */
  getSoBangLuongTamTinhTheoThang(): number {
    return this.list.filter(x =>
      x.trangThai === 'TAM_TINH' &&
      x.thang === this.selectedThangTongHop &&
      x.nam === this.selectedNamTongHop
    ).length;
  }

  /**
   * Lấy số bảng lương chờ duyệt theo tháng đã chọn
   */
  getSoBangLuongChoDuyetTheoThang(): number {
    return this.list.filter(x =>
      x.trangThai === 'CHO_DUYET_GIAM_DOC' &&
      x.thang === this.selectedThangTongHop &&
      x.nam === this.selectedNamTongHop
    ).length;
  }

  openGuiDuyetTatCaModal(): void {
    const dsTamTinh = this.list.filter(x =>
      x.trangThai === 'TAM_TINH' &&
      x.thang === this.selectedThangTongHop &&
      x.nam === this.selectedNamTongHop
    );
    if (dsTamTinh.length === 0) {
      this.toast.warning(`Không có bảng lương nào ở trạng thái Tạm tính trong tháng ${this.selectedThangTongHop}/${this.selectedNamTongHop}`);
      return;
    }
    this.bulkProgress = { current: 0, total: dsTamTinh.length, success: 0, failed: 0 };
    this.showGuiDuyetTatCaModal = true;
  }

  closeGuiDuyetTatCaModal(): void {
    this.showGuiDuyetTatCaModal = false;
    this.processingBulk = false;
    this.bulkProgress = { current: 0, total: 0, success: 0, failed: 0 };
  }

  // ========== THU HỒI BẢNG LƯƠNG ==========

  /**
   * Mở modal thu hồi hàng loạt
   */
  openThuHoiTatCaModal(): void {
    const dsChoDuyet = this.list.filter(x =>
      x.trangThai === 'CHO_DUYET_GIAM_DOC' &&
      x.thang === this.selectedThangTongHop &&
      x.nam === this.selectedNamTongHop
    );
    if (dsChoDuyet.length === 0) {
      this.toast.warning(`Không có bảng lương nào đang chờ duyệt trong tháng ${this.selectedThangTongHop}/${this.selectedNamTongHop}`);
      return;
    }
    this.selectedBangLuongId = null;
    this.thuHoiForm.reset();
    this.bulkProgress = { current: 0, total: dsChoDuyet.length, success: 0, failed: 0 };
    this.showThuHoiTatCaModal = true;
  }

  /**
   * Mở modal thu hồi 1 bảng lương
   */
  openThuHoiModal(bangLuongId: number): void {
    this.selectedBangLuongId = bangLuongId;
    this.thuHoiForm.reset();
    this.showThuHoiTatCaModal = true;
  }

  /**
   * Đóng modal thu hồi
   */
  closeThuHoiTatCaModal(): void {
    this.showThuHoiTatCaModal = false;
    this.processingBulk = false;
    this.selectedBangLuongId = null;
    this.thuHoiForm.reset();
    this.bulkProgress = { current: 0, total: 0, success: 0, failed: 0 };
  }

  /**
   * Thu hồi 1 bảng lương
   */
  thuHoi(bangLuongId: number): void {
    if (this.thuHoiForm.invalid) {
      this.thuHoiForm.markAllAsTouched();
      return;
    }

    const lyDo = this.thuHoiForm.value.lyDo;

    if (!confirm(`Bạn có chắc muốn thu hồi bảng lương này về trạng thái tạm tính?\nLý do: ${lyDo}`)) {
      return;
    }

    this.processingBulk = true;

    this.api.thuHoiLuong(bangLuongId)
      .pipe(finalize(() => (this.processingBulk = false)))
      .subscribe({
        next: () => {
          this.toast.success(`Thu hồi bảng lương thành công. Lý do: ${lyDo}`);
          this.closeThuHoiTatCaModal();
          this.loadList();
          this.loadTongHopThang();
        },
        error: (err: any) => {
          const msg = err?.error?.message || 'Thu hồi thất bại';
          this.toast.danger(msg);
        }
      });
  }

  /**
   * Thu hồi tất cả bảng lương chờ duyệt trong tháng
   */
  thuHoiTatCa(): void {
    if (this.thuHoiForm.invalid) {
      this.thuHoiForm.markAllAsTouched();
      return;
    }

    const lyDo = this.thuHoiForm.value.lyDo;
    const dsChoDuyet = this.list.filter(x =>
      x.trangThai === 'CHO_DUYET_GIAM_DOC' &&
      x.thang === this.selectedThangTongHop &&
      x.nam === this.selectedNamTongHop
    );

    if (dsChoDuyet.length === 0) {
      this.toast.warning('Không có bảng lương nào để thu hồi');
      return;
    }

    if (!confirm(`Bạn có chắc muốn thu hồi ${dsChoDuyet.length} bảng lương về trạng thái tạm tính?\nLý do: ${lyDo}`)) {
      return;
    }

    this.processingBulk = true;
    this.bulkProgress = { current: 0, total: dsChoDuyet.length, success: 0, failed: 0 };

    const requests = dsChoDuyet.map(item =>
      this.api.thuHoiLuong(item.id).pipe(
        finalize(() => {
          this.bulkProgress.current++;
        })
      )
    );

    forkJoin(requests).subscribe({
      next: (results) => {
        this.bulkProgress.success = results.length;
        this.toast.success(`Đã thu hồi ${results.length}/${dsChoDuyet.length} bảng lương. Lý do: ${lyDo}`);
        setTimeout(() => {
          this.closeThuHoiTatCaModal();
          this.loadList();
          this.loadTongHopThang();
        }, 1500);
      },
      error: (err: any) => {
        this.bulkProgress.failed = dsChoDuyet.length - this.bulkProgress.success;
        this.toast.danger(`Thu hồi thất bại. Thành công: ${this.bulkProgress.success}, Thất bại: ${this.bulkProgress.failed}`);
        setTimeout(() => {
          this.closeThuHoiTatCaModal();
          this.loadList();
          this.loadTongHopThang();
        }, 2000);
      }
    });
  }

  async guiDuyetTatCa(): Promise<void> {
    const dsTamTinh = this.list.filter(x =>
      x.trangThai === 'TAM_TINH' &&
      x.thang === this.selectedThangTongHop &&
      x.nam === this.selectedNamTongHop
    );
    if (dsTamTinh.length === 0) return;

    this.processingBulk = true;
    this.bulkProgress = { current: 0, total: dsTamTinh.length, success: 0, failed: 0 };

    for (const item of dsTamTinh) {
      try {
        await this.api.guiDuyet(item.id, { ghiChu: 'Gửi duyệt hàng loạt' }).toPromise();
        this.bulkProgress.success++;
      } catch (err) {
        this.bulkProgress.failed++;
        console.error(`Lỗi gửi duyệt ID ${item.id}:`, err);
      }
      this.bulkProgress.current++;
    }

    this.processingBulk = false;

    if (this.bulkProgress.failed === 0) {
      this.toast.success(`Đã gửi duyệt thành công ${this.bulkProgress.success} bảng lương`);
    } else {
      this.toast.warning(`Gửi duyệt: ${this.bulkProgress.success} thành công, ${this.bulkProgress.failed} thất bại`);
    }

    this.closeGuiDuyetTatCaModal();
    this.loadList();
    this.loadTongHopThang();
  }

  getStatusBadge(status: TrangThaiLuong): string {
    const map: Record<string, string> = {
      'KHAC': 'badge bg-secondary',
      'TAM_TINH': 'badge bg-secondary',
      'CHO_DUYET_GIAM_DOC': 'badge bg-warning text-dark',
      'DA_DUYET': 'badge bg-success',
      'TU_CHOI': 'badge bg-danger',
      'DA_TINH': 'badge bg-info',
      'DA_KHOA': 'badge bg-dark'
    };
    return map[status] || 'badge bg-secondary';
  }

  getStatusLabel(status: TrangThaiLuong): string {
    const map: Record<string, string> = {
      'KHAC': 'Khác',
      'TAM_TINH': 'Tạm tính',
      'CHO_DUYET_GIAM_DOC': 'Chờ duyệt',
      'DA_DUYET': 'Đã duyệt',
      'TU_CHOI': 'Từ chối',
      'DA_TINH': 'Đã tính',
      'DA_KHOA': 'Đã khóa'
    };
    return map[status] || status;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(value);
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

  get paginatedList(): BangLuongThangListItemDto[] {
    const start = (this.pageIndex - 1) * this.pageSize;
    const end = start + this.pageSize;
    return this.filtered.slice(start, end);
  }

  get hasNoBarChartData(): boolean {
    return this.monthlyStats.length === 0 || this.monthlyStats.every(m => m.tongLuong === 0);
  }

  get availableYears(): number[] {
    const currentYear = new Date().getFullYear();
    return Array.from({ length: 5 }, (_, i) => currentYear - i);
  }

  get availableMonths(): (number | 'ALL')[] {
    return ['ALL', 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
  }

  // ─── Excel Export ──────────────────────────────────────────────────────────
  openExportModal(): void {
    const now = new Date();
    this.exportForm.thang = this.thangFilter !== 'ALL' ? Number(this.thangFilter) : now.getMonth() + 1;
    this.exportForm.nam = this.namFilter || now.getFullYear();
    this.exportForm.nvHoSoId = null;
    this.exportAllEmployees = true;
    // Build employee list once — avoid getter causing infinite change detection
    const seen = new Set<number>();
    this.exportEmployeeList = this.list
      .filter(x => { if (seen.has(x.nvHoSoId)) return false; seen.add(x.nvHoSoId); return true; })
      .map(x => ({ id: x.nvHoSoId, hoTen: x.hoTen, maNhanVien: x.maNhanVien || String(x.nvHoSoId) }))
      .sort((a, b) => a.hoTen.localeCompare(b.hoTen, 'vi'));
    this.showExportModal = true;
  }

  closeExportModal(): void {
    this.showExportModal = false;
  }

  async doExport(): Promise<void> {
    let source = this.list.filter(
      x => x.thang === this.exportForm.thang && x.nam === this.exportForm.nam
    );
    if (!this.exportAllEmployees && this.exportForm.nvHoSoId) {
      source = source.filter(x => x.nvHoSoId === this.exportForm.nvHoSoId);
    }
    if (source.length === 0) {
      alert('Không có dữ liệu để xuất với bộ lọc đã chọn.');
      return;
    }

    const wb = new ExcelJS.Workbook();
    wb.creator = 'QLNS ERP';
    wb.created = new Date();

    const ws = wb.addWorksheet('Bảng lương', {
      pageSetup: { paperSize: 9, orientation: 'landscape', fitToPage: true, fitToWidth: 1 }
    });

    ws.columns = [
      { width: 5 }, { width: 12 }, { width: 26 }, { width: 20 },
      { width: 7 }, { width: 7 }, { width: 13 }, { width: 11 },
      { width: 18 }, { width: 15 }, { width: 14 }, { width: 14 },
      { width: 18 }, { width: 18 }
    ];

    // ── Shared style constants ────────────────────────────────────────────────
    const NAVY = 'FF1E3A5F';
    const H_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: NAVY } } as any;
    const EVEN_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF1F5F9' } } as any;
    const ODD_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFFFFF' } } as any;
    const TOTAL_FILL = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFFFF3CD' } } as any;
    const THIN_COLOR = { argb: 'FFD1D5DB' };
    const thinBorder = {
      top: { style: 'thin', color: THIN_COLOR }, left: { style: 'thin', color: THIN_COLOR },
      bottom: { style: 'thin', color: THIN_COLOR }, right: { style: 'thin', color: THIN_COLOR }
    } as any;
    const CURRENCY_FMT = '#,##0';
    const center = (wrapText = false): Partial<ExcelJS.Alignment> =>
      ({ horizontal: 'center', vertical: 'middle', wrapText });
    const right: Partial<ExcelJS.Alignment> = { horizontal: 'right', vertical: 'middle' };
    const left: Partial<ExcelJS.Alignment> = { vertical: 'middle' };

    // ── Row 1: Company name ───────────────────────────────────────────────────
    const r1 = ws.addRow(['CÔNG TY CỔ PHẦN CÔNG NGHỆ V9']);
    ws.mergeCells(1, 1, 1, 14);
    r1.height = 26;
    r1.getCell(1).font = { bold: true, size: 13, color: { argb: NAVY } };
    r1.getCell(1).alignment = center();

    // ── Row 2: Report title ───────────────────────────────────────────────────
    const r2 = ws.addRow([`BẢNG LƯƠNG THÁNG ${this.exportForm.thang}/${this.exportForm.nam}`]);
    ws.mergeCells(2, 1, 2, 14);
    r2.height = 36;
    r2.getCell(1).font = { bold: true, size: 16, color: { argb: NAVY } };
    r2.getCell(1).alignment = center();

    // ── Row 3: Metadata ───────────────────────────────────────────────────────
    const printDate = new Date().toLocaleDateString('vi-VN',
      { day: '2-digit', month: '2-digit', year: 'numeric' });
    const r3 = ws.addRow([`Ngày xuất: ${printDate}   •   Tổng số nhân viên: ${source.length} người`]);
    ws.mergeCells(3, 1, 3, 14);
    r3.height = 18;
    r3.getCell(1).font = { italic: true, size: 10, color: { argb: 'FF64748B' } };
    r3.getCell(1).alignment = center();

    // ── Row 4: Spacer ─────────────────────────────────────────────────────────
    ws.addRow([]).height = 6;

    // ── Row 5: Column headers ─────────────────────────────────────────────────
    const hRow = ws.addRow([
      'STT', 'Mã NV', 'Họ và tên', 'Phòng ban',
      'Tháng', 'Năm', 'Tổng công\n(ngày)', 'Tổng OT\n(giờ)',
      'Lương cơ bản', 'Phụ cấp', 'Thưởng', 'Khấu trừ', 'Tổng lương', 'Trạng thái'
    ]);
    hRow.height = 40;
    for (let c = 1; c <= 14; c++) {
      const cell = hRow.getCell(c);
      cell.fill = H_FILL;
      cell.font = { bold: true, color: { argb: 'FFFFFFFF' }, size: 11 };
      cell.alignment = center(true);
      cell.border = {
        top: { style: 'medium', color: { argb: NAVY } },
        left: { style: 'thin', color: THIN_COLOR },
        bottom: { style: 'medium', color: { argb: NAVY } },
        right: { style: 'thin', color: THIN_COLOR }
      } as any;
    }

    // ── Data rows ─────────────────────────────────────────────────────────────
    const statusLabel: Record<string, string> = {
      'TAM_TINH': 'Tạm tính', 'CHO_DUYET_GIAM_DOC': 'Chờ duyệt GĐ',
      'DA_DUYET': 'Đã duyệt', 'TU_CHOI': 'Từ chối', 'DA_KHOA': 'Đã khóa'
    };
    // [bgARGB, fgARGB]
    const statusStyle: Record<string, [string, string]> = {
      'TAM_TINH': ['FFE2E8F0', 'FF334155'],
      'CHO_DUYET_GIAM_DOC': ['FFFEF3C7', 'FF92400E'],
      'DA_DUYET': ['FFD1FAE5', 'FF065F46'],
      'TU_CHOI': ['FFFEE2E2', 'FF991B1B'],
      'DA_KHOA': ['FF1E293B', 'FFFFFFFF']
    };

    source.forEach((x, idx) => {
      const row = ws.addRow([
        idx + 1,
        x.maNhanVien || String(x.nvHoSoId),
        x.hoTen,
        x.tenPhongBan || '',
        x.thang, x.nam,
        x.tongCong, x.tongOt,
        x.luongCoBanTinh, x.phuCapTinh, x.thuong, x.khauTru, x.tongLuong,
        statusLabel[x.trangThai] || x.trangThai
      ]);
      row.height = 20;
      const rowFill = idx % 2 === 0 ? ODD_FILL : EVEN_FILL;

      for (let c = 1; c <= 14; c++) {
        const cell = row.getCell(c);
        cell.fill = rowFill;
        cell.border = thinBorder;
        if ([1, 5, 6, 7, 8].includes(c)) {
          cell.alignment = center();
        } else if (c >= 9 && c <= 13) {
          cell.numFmt = CURRENCY_FMT;
          cell.alignment = right;
        } else {
          cell.alignment = left;
        }
      }

      // Highlight total salary
      row.getCell(13).font = { bold: true, color: { argb: NAVY }, size: 11 };

      // Status badge styling
      const [bg, fg] = statusStyle[x.trangThai] || ['FFFFFFFF', 'FF000000'];
      const stCell = row.getCell(14);
      stCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: bg } } as any;
      stCell.font = { bold: true, size: 10, color: { argb: fg } };
      stCell.alignment = center();
    });

    // ── Totals row ────────────────────────────────────────────────────────────
    const sum = (fn: (r: typeof source[0]) => number) => source.reduce((s, r) => s + fn(r), 0);
    const totRow = ws.addRow([
      'TỔNG CỘNG', '', '', '', '', '',
      sum(r => r.tongCong), sum(r => r.tongOt),
      sum(r => r.luongCoBanTinh), sum(r => r.phuCapTinh),
      sum(r => r.thuong), sum(r => r.khauTru), sum(r => r.tongLuong),
      `${source.length} NV`
    ]);
    ws.mergeCells(totRow.number, 1, totRow.number, 6);
    totRow.height = 26;
    for (let c = 1; c <= 14; c++) {
      const cell = totRow.getCell(c);
      cell.fill = TOTAL_FILL;
      cell.font = { bold: true, size: 12, color: { argb: 'FF1E293B' } };
      cell.border = {
        top: { style: 'medium', color: { argb: NAVY } },
        left: { style: 'thin', color: THIN_COLOR },
        bottom: { style: 'double', color: { argb: NAVY } },
        right: { style: 'thin', color: THIN_COLOR }
      } as any;
      if (c >= 9 && c <= 13) {
        cell.numFmt = CURRENCY_FMT;
        cell.alignment = right;
      } else {
        cell.alignment = center();
      }
    }

    // ── Footer note ───────────────────────────────────────────────────────────
    ws.addRow([]).height = 6;
    const footRow = ws.addRow([
      '(*) Đơn vị: VNĐ.  Lương ngày = Lương CB ÷ 26.  Lương OT = Lương ngày ÷ 8 × Giờ OT × 1.5'
    ]);
    ws.mergeCells(footRow.number, 1, footRow.number, 14);
    footRow.getCell(1).font = { italic: true, size: 9, color: { argb: 'FF94A3B8' } };
    footRow.getCell(1).alignment = { horizontal: 'left', vertical: 'middle' };

    // ── Freeze top 5 rows (header area) ──────────────────────────────────────
    ws.views = [{ state: 'frozen', xSplit: 0, ySplit: 5, activeCell: 'A6' }];

    // ── Download ──────────────────────────────────────────────────────────────
    const empSuffix = (!this.exportAllEmployees && this.exportForm.nvHoSoId)
      ? `_nv${this.exportForm.nvHoSoId}` : '_tatca';
    const fileName = `bang_luong_${this.exportForm.nam}_thang${this.exportForm.thang}${empSuffix}.xlsx`;
    const buffer = await wb.xlsx.writeBuffer();
    const blob = new Blob([buffer], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
    this.closeExportModal();
  }

  exportExcel(): void { this.openExportModal(); }

  countExport(): number {
    let s = this.list.filter(x => x.thang === this.exportForm.thang && x.nam === this.exportForm.nam);
    if (!this.exportAllEmployees && this.exportForm.nvHoSoId) {
      s = s.filter(x => x.nvHoSoId === this.exportForm.nvHoSoId);
    }
    return s.length;
  }

  // ─── Print Salary Slip ─────────────────────────────────────────────────────
  printSalarySlip(item: BangLuongThangListItemDto): void {
    const luongCoBan = item.luongCoBanTinh;
    const luongNgay = luongCoBan / 26;
    const luongCong = luongNgay * item.tongCong;
    const luongOt = (luongNgay / 8) * item.tongOt * 1.5;

    // Áp dụng cờ công thức (mặc định bật nếu không có)
    const phuCapPrint = item.coTinhPhuCap !== false ? item.phuCapTinh : 0;
    const luongOtPrint = item.coTinhOT !== false ? luongOt : 0;
    const thuongPrint = item.coTinhThuong !== false ? item.thuong : 0;
    const khauTruPrint = item.coTinhKhauTru !== false ? item.khauTru : 0;

    const offTag = '<span style="font-size:10px;color:#888;font-style:italic">(không tính kỳ này)</span>';

    // Breakdown khấu trừ
    const ktDiMuon = item.khauTruDiMuon ?? 0;
    const ktThuongPhat = item.khauTruThuongPhat ?? 0;
    const ktBreakdownText = (ktDiMuon > 0 || ktThuongPhat > 0)
      ? `Phạt đi muộn: ${this.fmtNum(ktDiMuon)}; T&amp;P: ${this.fmtNum(ktThuongPhat)}`
      : 'gồm phạt đi muộn và thưởng &amp; phạt';

    const slip = `
<!DOCTYPE html>
<html lang="vi">
<head>
  <meta charset="UTF-8">
  <title>Phiếu lương - ${item.hoTen}</title>
  <style>
    body { font-family: Arial, sans-serif; padding: 30px; max-width: 720px; margin: 0 auto; font-size: 13px; }
    .header { text-align: center; margin-bottom: 24px; }
    .header h2 { font-size: 18px; margin: 4px 0; }
    .header p { margin: 2px 0; color: #555; }
    .divider { border-top: 2px solid #333; margin: 16px 0; }
    .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 6px 20px; margin-bottom: 20px; }
    .info-grid .label { color: #666; }
    .info-grid .value { font-weight: 600; }
    table { width: 100%; border-collapse: collapse; margin-top: 8px; }
    th { background: #f0f0f0; padding: 8px 12px; text-align: left; border: 1px solid #ddd; }
    td { padding: 7px 12px; border: 1px solid #ddd; }
    .total-row td { font-weight: 700; font-size: 14px; background: #fff9e6; }
    .positive { color: #059669; }
    .negative { color: #dc2626; }
    .disabled-row td { opacity: 0.45; text-decoration: line-through; }
    .formula-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; margin-top: 16px; font-size: 12px; }
    .formula-box h4 { font-size: 13px; margin: 0 0 8px; color: #475569; }
    .formula-box p { margin: 2px 0; font-family: monospace; }
    .formula-off { opacity: 0.45; text-decoration: line-through; }
    .footer { margin-top: 32px; display: grid; grid-template-columns: 1fr 1fr; text-align: center; }
    .footer div { padding-top: 8px; }
    .sig-line { margin-top: 60px; border-top: 1px solid #999; display: inline-block; width: 140px; }
    @media print { body { padding: 10px; } }
  </style>
</head>
<body>
  <div class="header">
    <h2>CÔNG TY CP CÔNG NGHỆ V9</h2>
    <p>PHIẾU LƯƠNG THÁNG ${item.thang}/${item.nam}</p>
  </div>
  <div class="divider"></div>
  <div class="info-grid">
    <span class="label">Họ và tên:</span><span class="value">${item.hoTen}</span>
    <span class="label">Mã nhân viên:</span><span class="value">${item.maNhanVien || item.nvHoSoId}</span>
    <span class="label">Phòng ban:</span><span class="value">${item.tenPhongBan || '—'}</span>
    <span class="label">Kỳ lương:</span><span class="value">Tháng ${item.thang}/${item.nam}</span>
    <span class="label">Số ngày công:</span><span class="value">${item.tongCong} ngày</span>
    <span class="label">Giờ làm thêm:</span><span class="value">${item.tongOt} giờ</span>
  </div>
  <table>
    <thead><tr><th>Khoản mục</th><th style="text-align:right">Số tiền (VND)</th></tr></thead>
    <tbody>
      <tr><td>Lương theo công (${this.fmtNum(luongNgay)}/ngày × ${item.tongCong} ngày)</td><td style="text-align:right" class="positive">${this.fmtNum(luongCong)}</td></tr>
      <tr${item.coTinhPhuCap === false ? ' class="disabled-row"' : ''}><td>Phụ cấp${item.coTinhPhuCap === false ? ' ' + offTag : ''}</td><td style="text-align:right" class="positive">${this.fmtNum(phuCapPrint)}</td></tr>
      <tr${item.coTinhOT === false ? ' class="disabled-row"' : ''}><td>Lương OT (${item.tongOt}h × ${this.fmtNum(luongNgay / 8)}/h × 1.5)${item.coTinhOT === false ? ' ' + offTag : ''}</td><td style="text-align:right" class="positive">${this.fmtNum(luongOtPrint)}</td></tr>
      <tr${item.coTinhThuong === false ? ' class="disabled-row"' : ''}><td>Thưởng${item.coTinhThuong === false ? ' ' + offTag : ''}</td><td style="text-align:right" class="positive">${this.fmtNum(thuongPrint)}</td></tr>
      <tr${item.coTinhKhauTru === false ? ' class="disabled-row"' : ''}><td>Khấu trừ${item.coTinhKhauTru === false ? ' ' + offTag : ''} <small style="color:#888">(${ktBreakdownText})</small></td><td style="text-align:right" class="negative">−${this.fmtNum(khauTruPrint)}</td></tr>
      <tr class="total-row"><td><strong>TỔNG LƯƠNG NHẬN</strong></td><td style="text-align:right"><strong>${this.fmtNum(item.tongLuong)}</strong></td></tr>
    </tbody>
  </table>
  <div class="formula-box">
    <h4>Công thức tính lương</h4>
    <p>Lương ngày = Lương CB / 26 = ${this.fmtNum(luongCoBan)} / 26 = ${this.fmtNum(luongNgay)}</p>
    <p>Lương theo công = ${this.fmtNum(luongNgay)} × ${item.tongCong} = ${this.fmtNum(luongCong)}</p>
    <p${item.coTinhOT === false ? ' class="formula-off"' : ''}>Lương OT = ${this.fmtNum(luongNgay)} / 8 × ${item.tongOt}h × 1.5 = ${this.fmtNum(luongOt)}${item.coTinhOT === false ? ' (không tính)' : ''}</p>
    <p${item.coTinhPhuCap === false ? ' class="formula-off"' : ''}>Phụ cấp = ${this.fmtNum(item.phuCapTinh)}${item.coTinhPhuCap === false ? ' (không tính)' : ''}</p>
    <p${item.coTinhThuong === false ? ' class="formula-off"' : ''}>Thưởng = ${this.fmtNum(item.thuong)}${item.coTinhThuong === false ? ' (không tính)' : ''}</p>
    <p${item.coTinhKhauTru === false ? ' class="formula-off"' : ''}>Khấu trừ = ${this.fmtNum(item.khauTru)} &nbsp;<em>(${ktBreakdownText})</em>${item.coTinhKhauTru === false ? ' (không tính)' : ''}</p>
    <p><strong>Tổng = ${this.fmtNum(luongCong)} + ${this.fmtNum(phuCapPrint)} + ${this.fmtNum(luongOtPrint)} + ${this.fmtNum(thuongPrint)} − ${this.fmtNum(khauTruPrint)} = ${this.fmtNum(item.tongLuong)}</strong></p>
  </div>
  <div class="footer">
    <div>
      <p>Người lập phiếu</p>
      <div class="sig-line"></div>
    </div>
    <div>
      <p>Nhân viên xác nhận</p>
      <div class="sig-line"></div>
    </div>
  </div>
</body>
</html>`;

    const win = window.open('', '_blank', 'width=760,height=900');
    if (win) {
      win.document.write(slip);
      win.document.close();
      win.onload = () => win.print();
    }
  }

  private fmtNum(val: number): string {
    return new Intl.NumberFormat('vi-VN').format(val || 0);
  }

}
