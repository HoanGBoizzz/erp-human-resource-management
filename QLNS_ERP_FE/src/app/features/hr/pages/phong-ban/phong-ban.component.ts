import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import {
  PhongBanListDto,
  PhongBanDetailDto,
  PhongBanCreateDto,
  PhongBanUpdateDto,
  NhanVienTrongPhongBanDto
} from 'src/app/core/models/phong-ban.model';
import { PhongBanApiService } from 'src/app/core/services/api/phong-ban-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';

@Component({
  selector: 'app-phong-ban',
  templateUrl: './phong-ban.component.html',
  styleUrls: ['./phong-ban.component.scss']
})
export class PhongBanComponent implements OnInit, OnDestroy {
  // Loading states
  loadingList = false;
  loadingDetail = false;
  saving = false;
  deleting = false;
  transferring = false;

  // Data
  list: PhongBanListDto[] = [];
  detail: PhongBanDetailDto | null = null;

  // Modals
  showCreateModal = false;
  showEditModal = false;
  showDetailModal = false;
  showTransferModal = false;

  // Forms
  createForm: FormGroup;
  editForm: FormGroup;
  transferForm: FormGroup;

  // Selected items
  selectedId: number | null = null;
  selectedNhanVien: NhanVienTrongPhongBanDto | null = null;

  errorMsg = '';
  quyetDinhFile: File | null = null;
  deletingNvId: number | null = null;

  @ViewChild('nvChart', { static: false }) nvChartRef?: ElementRef<HTMLCanvasElement>;
  private nvChart?: Chart;

  @ViewChild('deptBarChart', { static: false }) deptBarChartRef?: ElementRef<HTMLCanvasElement>;
  private deptBarChart?: Chart;

  constructor(
    private api: PhongBanApiService,
    private toast: ToastService,
    private fb: FormBuilder
  ) {
    this.createForm = this.fb.group({
      maPhongBan: ['', [Validators.required, Validators.maxLength(20)]],
      tenPhongBan: ['', [Validators.required, Validators.maxLength(100)]],
      phongBanChaId: [null],
      ghiChu: ['']
    });

    this.editForm = this.fb.group({
      tenPhongBan: ['', [Validators.required, Validators.maxLength(100)]],
      phongBanChaId: [null],
      trangThai: [true],
      ghiChu: ['']
    });

    this.transferForm = this.fb.group({
      phongBanMoiId: [null, Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadList();
  }

  ngOnDestroy(): void {
    this.nvChart?.destroy();
    this.deptBarChart?.destroy();
  }

  loadList(): void {
    this.loadingList = true;
    this.errorMsg = '';
    this.api.getAll()
      .pipe(finalize(() => (this.loadingList = false)))
      .subscribe({
        next: (res) => {
          this.list = res;
          setTimeout(() => this.renderDeptBarChart());
        },
        error: (err) => {
          this.errorMsg = err.error?.message || 'Lỗi tải danh sách phòng ban';
          this.toast.danger(this.errorMsg);
        }
      });
  }

  // ===== CREATE =====
  openCreateModal(): void {
    this.createForm.reset({
      maPhongBan: '',
      tenPhongBan: '',
      phongBanChaId: null,
      ghiChu: ''
    });
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  saveCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const dto: PhongBanCreateDto = this.createForm.value;
    this.saving = true;
    this.api.create(dto)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Đã thêm phòng ban thành công');
          this.showCreateModal = false;
          this.loadList();
        },
        error: (err) => {
          this.toast.danger(err.error?.message || 'Lỗi thêm phòng ban');
        }
      });
  }

  // ===== EDIT =====
  openEditModal(pb: PhongBanListDto): void {
    this.selectedId = pb.id;
    this.editForm.reset({
      tenPhongBan: pb.tenPhongBan,
      phongBanChaId: pb.phongBanChaId,
      trangThai: pb.trangThai,
      ghiChu: pb.ghiChu || ''
    });
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.selectedId = null;
  }

  saveEdit(): void {
    if (this.editForm.invalid || !this.selectedId) {
      this.editForm.markAllAsTouched();
      return;
    }

    const dto: PhongBanUpdateDto = this.editForm.value;
    this.saving = true;
    this.api.update(this.selectedId, dto)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Đã cập nhật phòng ban');
          this.showEditModal = false;
          this.loadList();
        },
        error: (err) => {
          this.toast.danger(err.error?.message || 'Lỗi cập nhật');
        }
      });
  }

  // ===== DELETE =====
  confirmDelete(pb: PhongBanListDto): void {
    if (!confirm(`Bạn có chắc muốn xóa phòng ban "${pb.tenPhongBan}"?`)) {
      return;
    }

    this.deleting = true;
    this.api.delete(pb.id)
      .pipe(finalize(() => (this.deleting = false)))
      .subscribe({
        next: () => {
          this.toast.success('Đã xóa phòng ban');
          this.loadList();
        },
        error: (err) => {
          this.toast.danger(err.error?.message || 'Lỗi xóa phòng ban');
        }
      });
  }

  // ===== DETAIL =====
  openDetailModal(pb: PhongBanListDto): void {
    this.selectedId = pb.id;
    this.detail = null;
    this.loadingDetail = true;
    this.showDetailModal = true;

    this.api.getById(pb.id)
      .pipe(finalize(() => (this.loadingDetail = false)))
      .subscribe({
        next: (res) => {
          this.detail = res;
          setTimeout(() => this.renderNvChart());
        },
        error: (err) => {
          this.toast.danger(err.error?.message || 'Lỗi tải chi tiết');
          this.showDetailModal = false;
        }
      });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedId = null;
    this.detail = null;
    this.nvChart?.destroy();
    this.nvChart = undefined;
  }

  // ===== TRANSFER =====
  openTransferModal(nv: NhanVienTrongPhongBanDto): void {
    this.selectedNhanVien = nv;
    this.transferForm.reset({ phongBanMoiId: null });
    this.showTransferModal = true;
  }

  closeTransferModal(): void {
    this.showTransferModal = false;
    this.selectedNhanVien = null;
    this.quyetDinhFile = null;
  }

  onQuyetDinhFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.quyetDinhFile = input.files?.[0] ?? null;
  }

  saveTransfer(): void {
    if (this.transferForm.invalid || !this.selectedNhanVien) {
      this.transferForm.markAllAsTouched();
      return;
    }

    const formData = new FormData();
    formData.append('nvCongViecId', String(this.selectedNhanVien.nvCongViecId));
    formData.append('phongBanMoiId', String(this.transferForm.value.phongBanMoiId));
    if (this.quyetDinhFile) {
      formData.append('quyetDinh', this.quyetDinhFile, this.quyetDinhFile.name);
    }

    this.transferring = true;
    this.api.chuyenNhanVien(formData)
      .pipe(finalize(() => (this.transferring = false)))
      .subscribe({
        next: () => {
          this.toast.success('Đã điều chuyển nhân viên');
          this.showTransferModal = false;
          // Reload detail
          if (this.selectedId) {
            this.api.getById(this.selectedId).subscribe({
              next: (res) => {
                this.detail = res;
              }
            });
          }
          this.loadList();
        },
        error: (err) => {
          this.toast.danger(err.error?.message || 'Lỗi điều chuyển');
        }
      });
  }

  // ===== XÓA NHÂN VIÊN NGHỈ VIỆC KHỏi PHÒNG BAN =====
  xoaNhanVienKhoiPhongBan(nv: NhanVienTrongPhongBanDto): void {
    if (!confirm(`Xóa "${nv.hoTen}" khỏi danh sách phòng ban này?`)) return;
    this.deletingNvId = nv.nvCongViecId;
    this.api.xoaNhanVienKhoiPhongBan(nv.nvCongViecId)
      .pipe(finalize(() => (this.deletingNvId = null)))
      .subscribe({
        next: () => {
          this.toast.success('Dả xóa nhân viên khỏi phòng ban');
          if (this.selectedId) {
            this.api.getById(this.selectedId).subscribe({ next: res => this.detail = res });
          }
          this.loadList();
        },
        error: (err) => this.toast.danger(err.error?.message || 'Lỗi xóa')
      });
  }

  // ===== DEPT BAR CHART =====
  private renderDeptBarChart(): void {
    const canvas = this.deptBarChartRef?.nativeElement;
    if (!canvas || this.list.length === 0) return;
    if (this.deptBarChart) this.deptBarChart.destroy();

    const labels = this.list.map(pb => pb.tenPhongBan);
    const data = this.list.map(pb => pb.tongNhanVien);
    const total = data.reduce((s, v) => s + v, 0);
    const colors = [
      '#2563eb', '#16a34a', '#f59e0b', '#ef4444', '#8b5cf6',
      '#06b6d4', '#ec4899', '#84cc16', '#f97316', '#a855f7'
    ];

    const config: ChartConfiguration<'bar', number[], string> = {
      type: 'bar',
      data: {
        labels,
        datasets: [{
          label: 'Nhân viên',
          data,
          backgroundColor: labels.map((_, i) => colors[i % colors.length] + '99'),
          borderColor: labels.map((_, i) => colors[i % colors.length]),
          borderWidth: 2,
          borderRadius: 6,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(15, 23, 42, 0.9)',
            padding: 12,
            titleFont: { size: 13, weight: 'bold' },
            bodyFont: { size: 12 },
            footerFont: { size: 11, weight: 'bold' },
            displayColors: true,
            borderColor: '#ffffff',
            borderWidth: 1,
            callbacks: {
              label: (ctx) => {
                const val = ctx.parsed.y ?? 0;
                const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0';
                return ` ${val} nhân viên (${pct}%)`;
              },
              footer: () => `Tổng: ${total} nhân viên`
            }
          }
        },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { stepSize: 1, precision: 0 },
            grid: { color: 'rgba(0,0,0,.06)' }
          },
          x: { grid: { display: false } }
        }
      }
    };
    this.deptBarChart = new Chart(canvas, config);
  }

  // ===== CHART =====
  private renderNvChart(): void {
    const canvas = this.nvChartRef?.nativeElement;
    if (!canvas || !this.detail) return;
    if (this.nvChart) this.nvChart.destroy();

    const dangLam = this.getSoNvDangLam();
    const nghiViec = this.getSoNvNghiViec();
    const total = dangLam + nghiViec;

    const config: ChartConfiguration<'pie'> = {
      type: 'pie',
      data: {
        labels: ['Đang làm', 'Nghỉ việc'],
        datasets: [{
          data: [dangLam, nghiViec],
          backgroundColor: ['#22c55e', '#94a3b8'],
          borderColor: ['#fff', '#fff'],
          borderWidth: 2,
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: 'rgba(15, 23, 42, 0.9)',
            padding: 10,
            titleFont: { size: 13, weight: 'bold' },
            bodyFont: { size: 12 },
            footerFont: { size: 11, weight: 'bold' },
            displayColors: true,
            borderColor: '#ffffff',
            borderWidth: 1,
            callbacks: {
              label: (ctx) => {
                const val = (ctx.parsed as number) ?? 0;
                const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0';
                return ` ${ctx.label}: ${val} NV (${pct}%)`;
              },
              footer: () => `Tổng: ${total} nhân viên`
            }
          }
        }
      }
    };
    this.nvChart = new Chart(canvas, config);
  }

  // ===== HELPERS =====
  getStatusBadge(trangThai: boolean): string {
    return trangThai
      ? 'badge rounded-pill bg-success-subtle text-success'
      : 'badge rounded-pill bg-secondary-subtle text-secondary';
  }

  getStatusLabel(trangThai: boolean): string {
    return trangThai ? 'Hoạt động' : 'Ngừng hoạt động';
  }

  getNvStatusClass(trangThai: number): string {
    return trangThai === 1 ? '' : 'nv-resigned';
  }

  getNvStatusBadge(trangThai: number): string {
    return trangThai === 1
      ? 'badge rounded-pill bg-success-subtle text-success'
      : 'badge rounded-pill bg-secondary-subtle text-secondary';
  }

  getNvStatusLabel(trangThai: number): string {
    return trangThai === 1 ? 'Đang làm' : 'Nghỉ việc';
  }

  // Lấy danh sách phòng ban khác (để chọn phòng ban cha hoặc chuyển NV)
  getOtherPhongBan(excludeId?: number): PhongBanListDto[] {
    return this.list.filter(pb => pb.id !== excludeId && pb.trangThai);
  }

  // Helper methods for employee counts
  getSoNvDangLam(): number {
    return this.detail?.danhSachNhanVien?.filter(nv => nv.trangThaiLamViec === 1).length || 0;
  }

  getSoNvNghiViec(): number {
    return this.detail?.danhSachNhanVien?.filter(nv => nv.trangThaiLamViec !== 1).length || 0;
  }
}
