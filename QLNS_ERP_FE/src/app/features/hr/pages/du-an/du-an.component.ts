// src/app/features/du-an/pages/du-an-hr-acc/du-an-hr-acc.component.ts

import { Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Chart, ChartConfiguration } from 'chart.js/auto';
import { finalize, forkJoin, of, Subscription } from 'rxjs';
import {
  DU_AN_DETAIL_DEFAULT,
  DuAnCreateDto,
  DuAnDetailDto,
  DuAnListItemDto,
  DuAnTrangThai,
  DuAnUpdateDto,
} from 'src/app/core/models/du-an.model';
import { DuAnApiService } from 'src/app/core/services/api/du-an-api.service';
import { NhanVienApiService } from 'src/app/core/services/api/nhan-vien-api.service';
import { ThongBaoApiService } from 'src/app/core/services/api/thong-bao-api.service';
import { NhanVienListItemDto, PagingRequestDto, PagedResultDto } from 'src/app/core/models/nhan-vien.model';
import { AuthService } from 'src/app/core/services/auth.service';
import { RoleCode } from 'src/app/shared/enums/role.enum';
import { environment } from 'src/environments/environment';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';

type Mode = 'LIST' | 'CREATE' | 'EDIT';

@Component({
  selector: 'app-du-an',
  templateUrl: './du-an.component.html',
  styleUrls: ['./du-an.component.scss'],
})
export class DuAnComponent implements OnInit, OnDestroy {
  loading = false;
  detailLoading = false;
  saving = false;
  errorMsg = '';
  uploading = false;
  uploadError = '';
  loadingNhanVien = false;

  list: DuAnListItemDto[] = [];
  filtered: DuAnListItemDto[] = [];
  nhanVienList: NhanVienListItemDto[] = [];
  filteredNhanVien: NhanVienListItemDto[] = [];
  nhanVienSearch = '';

  // Temporary members for form (before save)
  tempMembers: Array<{ nvId: number; hoTen: string; vaiTro: string }> = [];

  q = '';
  statusFilter: DuAnTrangThai | 'ALL' = 'ALL';

  // KPI (count-up)
  kpiTotal = 0;
  kpiDraft = 0;
  kpiPending = 0;
  kpiApproved = 0;
  kpiRejected = 0;

  selected: DuAnDetailDto = { ...DU_AN_DETAIL_DEFAULT };
  selectedId = 0;
  showDetailModal = false;
  showFormModal = false;

  mode: Mode = 'LIST';
  form: FormGroup;

  // Members form (HR nhập ID NV vì BE chưa có endpoint search NV)
  memberForm: FormGroup;
  roleForm: FormGroup;

  @ViewChild('statusChart', { static: false }) statusChartRef?: ElementRef<HTMLCanvasElement>;
  private statusChart?: Chart;
  private entityUpdateSub?: Subscription;

  constructor(
    private api: DuAnApiService,
    private nhanVienApi: NhanVienApiService,
    private authService: AuthService,
    private toast: ToastService,
    private thongBaoApi: ThongBaoApiService,
    private signalr: SignalrService,
    fb: FormBuilder
  ) {
    this.form = fb.group({
      maDuAn: ['', [Validators.required, Validators.maxLength(50)]],
      tenDuAn: ['', [Validators.required, Validators.maxLength(255)]],
      moTa: [''],
      nganSach: [null],
      ngayBatDau: [null],
      ngayKetThuc: [null],
      nvPhuTrachId: [null],
    });

    this.memberForm = fb.group({
      nvHoSoId: [null, [Validators.required]],
      vaiTroTrongDuAn: ['', [Validators.required, Validators.maxLength(100)]],
    });

    this.roleForm = fb.group({
      vaiTroTrongDuAn: ['', [Validators.required, Validators.maxLength(100)]],
    });
  }

  ngOnInit(): void {
    this.loadList();
    this.loadNhanVienList();

    // Realtime: auto-refresh when DU_AN entity is updated by anyone
    this.entityUpdateSub = this.signalr.entityUpdate$.subscribe(update => {
      if (update.entityType === 'DU_AN') {
        this.loadList();
        // If the updated entity is currently open in detail modal, refresh it
        if (this.showDetailModal && this.selectedId === update.entityId) {
          this.openDetail(update.entityId);
        }
      }
    });
  }

  ngOnDestroy(): void {
    this.destroyCharts();
    this.entityUpdateSub?.unsubscribe();
  }

  // ================= LOAD NHAN VIEN =================
  loadNhanVienList(): void {
    this.loadingNhanVien = true;
    const request: PagingRequestDto = {
      pageIndex: 1,
      pageSize: 500,
      keyword: this.nhanVienSearch.trim(),
    };

    this.nhanVienApi
      .getPaged(request)
      .pipe(finalize(() => (this.loadingNhanVien = false)))
      .subscribe({
        next: (res: PagedResultDto<NhanVienListItemDto>) => {
          const listCandidate = res?.items;

          this.nhanVienList = Array.isArray(listCandidate) ? listCandidate : [];
          this.filteredNhanVien = [...this.nhanVienList];
        },
        error: (err: unknown) => {
          console.error('Lỗi khi tải danh sách nhân viên:', err);
        },
      });
  }

  filterNhanVien(): void {
    const q = this.nhanVienSearch.toLowerCase().trim();
    if (!q) {
      this.filteredNhanVien = this.nhanVienList;
    } else {
      this.filteredNhanVien = this.nhanVienList.filter(
        (nv) =>
          nv.maNhanVien.toLowerCase().includes(q) ||
          nv.hoTen.toLowerCase().includes(q) ||
          (nv.tenPhongBan && nv.tenPhongBan.toLowerCase().includes(q)) ||
          (nv.tenChucVu && nv.tenChucVu.toLowerCase().includes(q))
      );
    }

    // Sort: Added members first, then by name
    this.filteredNhanVien.sort((a, b) => {
      const aAdded = this.isTempMemberAdded(a.id);
      const bAdded = this.isTempMemberAdded(b.id);
      if (aAdded && !bAdded) return -1;
      if (!aAdded && bAdded) return 1;
      return a.hoTen.localeCompare(b.hoTen);
    });
  }

  // Filter for leaders only (Truong Phong, HR_KETOAN, Giam Doc)
  get filteredLeaders(): NhanVienListItemDto[] {
    const leaderTitles = ['Trưởng phòng', 'Giám đốc', 'HR', 'Kế toán'];
    return this.filteredNhanVien.filter(nv =>
      nv.tenChucVu && leaderTitles.some(title => nv.tenChucVu?.includes(title))
    );
  }

  // Filter for members (all employees, exclude selected leader)
  get filteredMembers(): NhanVienListItemDto[] {
    const selectedLeaderId = this.form.get('nvPhuTrachId')?.value;
    if (selectedLeaderId) {
      // Exclude the selected leader from member list
      return this.filteredNhanVien.filter(nv => nv.id !== selectedLeaderId);
    }
    return this.filteredNhanVien;
  }

  getNhanVienDisplay(id: number | null): string {
    if (!id) return '';
    const nv = this.nhanVienList.find((x) => x.id === id);
    return nv ? `${nv.maNhanVien} - ${nv.hoTen}` : `ID: ${id}`;
  }

  getNhanVienWithDepartment(id: number | null): { hoTen: string; chucVu: string; phongBan: string } | null {
    if (!id) return null;
    const nv = this.nhanVienList.find((x) => x.id === id);
    if (!nv) return null;
    return {
      hoTen: `${nv.maNhanVien} - ${nv.hoTen}`,
      chucVu: nv.tenChucVu || 'Chưa có chức vụ',
      phongBan: nv.tenPhongBan || 'Chưa có phòng ban'
    };
  }

  selectNhanVienForMember(nvId: number): void {
    if (this.isMemberAlreadyAdded(nvId)) return;
    this.memberForm.patchValue({ nvHoSoId: nvId });
  }

  selectNhanVienPhuTrach(nvId: number): void {
    this.form.patchValue({ nvPhuTrachId: nvId });
  }

  isNhanVienMemberSelected(nvId: number): boolean {
    return this.memberForm.value?.nvHoSoId === nvId;
  }

  isNhanVienPhuTrachSelected(nvId: number): boolean {
    return this.form.value?.nvPhuTrachId === nvId;
  }

  // ================= LIST =================
  loadList(): void {
    this.loading = true;
    this.errorMsg = '';

    this.api
      .getList()
      .pipe(finalize(() => (this.loading = false)))
      .subscribe({
        next: (res) => {
          this.list = res || [];
          this.applyFilter();
          this.recalcKpiAndCharts();
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
        },
      });
  }

  applyFilter(): void {
    const qLower = this.q.trim().toLowerCase();

    this.filtered = this.list.filter((x) => {
      const okQ =
        !qLower ||
        x.maDuAn.toLowerCase().includes(qLower) ||
        x.tenDuAn.toLowerCase().includes(qLower) ||
        (x.tenNhanVienPhuTrach || '').toLowerCase().includes(qLower);

      const okStatus = this.statusFilter === 'ALL' || x.trangThaiDuAn === this.statusFilter;
      return okQ && okStatus;
    });
  }

  // ================= DETAIL =================
  openDetail(id: number): void {
    this.selectedId = id;
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
    this.detailLoading = true;
    this.errorMsg = '';
    this.showDetailModal = true;

    // Mark related notifications as read
    this.thongBaoApi.markAsReadByEntity('DU_AN', id).subscribe();

    this.api
      .getDetail(id)
      .pipe(finalize(() => (this.detailLoading = false)))
      .subscribe({
        next: (res) => {
          this.selected = res;
          // default role form when select a member later
        },
        error: (err) => (this.errorMsg = this.toMsg(err)),
      });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedId = 0;
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
  }

  // ================= MODE + FORM =================
  startCreate(): void {
    this.mode = 'CREATE';
    this.form.reset({
      maDuAn: '',
      tenDuAn: '',
      moTa: '',
      nganSach: null,
      ngayBatDau: null,
      ngayKetThuc: null,
      nvPhuTrachId: null,
    });
    this.selected = { ...DU_AN_DETAIL_DEFAULT };
    this.tempMembers = []; // Reset temp members
    this.showFormModal = true;
  }

  startEditFromSelected(): void {
    if (this.selectedId <= 0) return;

    this.mode = 'EDIT';
    this.form.reset({
      maDuAn: this.selected.maDuAn,
      tenDuAn: this.selected.tenDuAn,
      moTa: this.selected.moTa,
      nganSach: this.selected.nganSach,
      ngayBatDau: this.selected.ngayBatDau,
      ngayKetThuc: this.selected.ngayKetThuc,
      nvPhuTrachId: this.selected.nvPhuTrachId,
    });

    // Load existing members into tempMembers for editing
    this.tempMembers = this.selected.thanhViens.map(m => ({
      nvId: m.nvHoSoId,
      hoTen: m.hoTen,
      vaiTro: m.vaiTroTrongDuAn
    }));

    // HR edit: maDuAn không có trong UpdateDto, chỉ cho xem (khóa input)
    this.form.get('maDuAn')?.disable({ emitEvent: false });

    this.showDetailModal = false;
    this.showFormModal = true;
  }

  cancelForm(): void {
    this.showFormModal = false;
    this.mode = 'LIST';
    this.form.enable({ emitEvent: false });
    this.form.reset();
  }

  closeFormModal(): void {
    this.cancelForm();
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMsg = '';

    if (this.mode === 'CREATE') {
      const dto: DuAnCreateDto = {
        maDuAn: this.form.getRawValue().maDuAn,
        tenDuAn: this.form.getRawValue().tenDuAn,
        moTa: this.form.getRawValue().moTa || null,
        nganSach: this.toNumberOrNull(this.form.getRawValue().nganSach),
        ngayBatDau: this.form.getRawValue().ngayBatDau || null,
        ngayKetThuc: this.form.getRawValue().ngayKetThuc || null,
        nvPhuTrachId: this.toNumberOrNull(this.form.getRawValue().nvPhuTrachId),
      };

      this.api
        .create(dto)
        .subscribe({
          next: (res) => {
            if (res && res.id) {
              // Thêm thành viên sau khi tạo dự án
              this.saveMembersAfterCreateOrUpdate(res.id, true);
            } else {
              this.saving = false;
              this.toast.success('Tạo dự án thành công!');
              this.cancelForm();
              this.loadList();
            }
          },
          error: (err) => {
            this.saving = false;
            this.errorMsg = this.toMsg(err);
            this.toast.danger('Tạo dự án thất bại: ' + this.toMsg(err));
          },
        });

      return;
    }

    // EDIT
    const dtoUpdate: DuAnUpdateDto = {
      tenDuAn: this.form.getRawValue().tenDuAn,
      moTa: this.form.getRawValue().moTa || null,
      nganSach: this.toNumberOrNull(this.form.getRawValue().nganSach),
      ngayBatDau: this.form.getRawValue().ngayBatDau || null,
      ngayKetThuc: this.form.getRawValue().ngayKetThuc || null,
      nvPhuTrachId: this.toNumberOrNull(this.form.getRawValue().nvPhuTrachId),
    };

    this.api
      .update(this.selectedId, dtoUpdate)
      .subscribe({
        next: () => {
          // Sync thành viên sau khi update dự án
          this.saveMembersAfterCreateOrUpdate(this.selectedId, false);
        },
        error: (err) => {
          this.saving = false;
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Cập nhật dự án thất bại: ' + this.toMsg(err));
        },
      });
  }

  // ================= SAVE MEMBERS AFTER CREATE/UPDATE =================
  private saveMembersAfterCreateOrUpdate(duAnId: number, isCreate: boolean): void {
    if (this.tempMembers.length === 0) {
      // Không có thành viên, hoàn tất
      this.saving = false;
      this.toast.success(isCreate ? 'Tạo dự án thành công!' : 'Cập nhật dự án thành công!');
      this.cancelForm();
      this.loadList();
      this.openDetail(duAnId);
      return;
    }

    // Lấy danh sách thành viên hiện tại (chỉ khi EDIT)
    const currentMembers = isCreate ? [] : this.selected.thanhViens.map(m => m.nvHoSoId);
    const tempMemberIds = this.tempMembers.map(m => m.nvId);

    // Xác định thành viên cần thêm, xóa
    const toAdd = this.tempMembers.filter(m => !currentMembers.includes(m.nvId));
    const toRemove = currentMembers.filter(id => !tempMemberIds.includes(id));

    const addCalls = toAdd.map(m =>
      this.api.addMember(duAnId, {
        nvHoSoId: m.nvId,
        vaiTroTrongDuAn: m.vaiTro || 'Thành viên'
      })
    );

    const removeCalls = toRemove.map(id => this.api.removeMember(duAnId, id));

    const allCalls = [...addCalls, ...removeCalls];

    if (allCalls.length === 0) {
      // Không có thay đổi thành viên
      this.saving = false;
      this.toast.success(isCreate ? 'Tạo dự án thành công!' : 'Cập nhật dự án thành công!');
      this.cancelForm();
      this.loadList();
      this.openDetail(duAnId);
      return;
    }

    // Thực hiện tất cả API calls đồng thời
    forkJoin(allCalls)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success(isCreate ? 'Tạo dự án và thêm thành viên thành công!' : 'Cập nhật dự án và thành viên thành công!');
          this.cancelForm();
          this.loadList();
          this.openDetail(duAnId);
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.warning(isCreate
            ? 'Tạo dự án thành công nhưng có lỗi khi thêm thành viên'
            : 'Cập nhật dự án thành công nhưng có lỗi khi sync thành viên');
          this.loadList();
          this.openDetail(duAnId);
        },
      });
  }

  // ================= ACTIONS =================
  canGuiDuyet(): boolean {
    return this.selectedId > 0 && (
      this.selected.trangThaiDuAn === 'DANG_NHAP' ||
      this.selected.trangThaiDuAn === 'TU_CHOI'
    );
  }

  guiDuyet(): void {
    if (!this.canGuiDuyet()) return;

    this.saving = true;
    this.errorMsg = '';

    this.api
      .guiDuyet(this.selectedId, { ghiChu: 'HR gửi duyệt giám đốc' })
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Gửi duyệt dự án thành công!');
          this.openDetail(this.selectedId);
          this.loadList();
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Gửi duyệt thất bại: ' + this.toMsg(err));
        },
      });
  }

  // ================= MEMBERS =================
  addMember(): void {
    if (this.memberForm.invalid || this.selectedId <= 0) {
      this.memberForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMsg = '';

    const raw = this.memberForm.getRawValue();
    this.api
      .addMember(this.selectedId, {
        nvHoSoId: Number(raw.nvHoSoId),
        vaiTroTrongDuAn: raw.vaiTroTrongDuAn,
      })
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Thêm thành viên thành công!');
          this.memberForm.reset({ nvHoSoId: null, vaiTroTrongDuAn: '' });
          this.openDetail(this.selectedId);
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Thêm thành viên thất bại: ' + this.toMsg(err));
        },
      });
  }

  patchRole(role: string): void {
    this.roleForm.reset({ vaiTroTrongDuAn: role });
  }

  updateMemberRole(nvHoSoId: number): void {
    if (this.roleForm.invalid || this.selectedId <= 0) {
      this.roleForm.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMsg = '';

    const raw = this.roleForm.getRawValue();
    this.api
      .updateMemberRole(this.selectedId, nvHoSoId, { vaiTroTrongDuAn: raw.vaiTroTrongDuAn })
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Cập nhật vai trò thành công!');
          this.openDetail(this.selectedId);
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Cập nhật vai trò thất bại: ' + this.toMsg(err));
        },
      });
  }

  removeMember(nvHoSoId: number): void {
    if (this.selectedId <= 0) return;
    if (!confirm('Xóa thành viên này khỏi dự án?')) return;

    this.saving = true;
    this.errorMsg = '';

    this.api
      .removeMember(this.selectedId, nvHoSoId)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Xóa thành viên thành công!');
          this.openDetail(this.selectedId);
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Xóa thành viên thất bại: ' + this.toMsg(err));
        },
      });
  }

  // ================= KPI + CHARTS =================
  private recalcKpiAndCharts(): void {
    const total = this.list.length;
    const draft = this.list.filter((x) => x.trangThaiDuAn === 'DANG_NHAP').length;
    const pending = this.list.filter((x) => x.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC').length;
    const approved = this.list.filter((x) => x.trangThaiDuAn === 'DA_DUYET').length;
    const rejected = this.list.filter((x) => x.trangThaiDuAn === 'TU_CHOI').length;

    this.animateKpi('kpiTotal', total);
    this.animateKpi('kpiDraft', draft);
    this.animateKpi('kpiPending', pending);
    this.animateKpi('kpiApproved', approved);
    this.animateKpi('kpiRejected', rejected);

    setTimeout(() => this.renderStatusChart(draft, pending, approved, rejected));
  }

  private renderStatusChart(draft: number, pending: number, approved: number, rejected: number): void {
    const canvas = this.statusChartRef?.nativeElement;
    if (!canvas) return;

    if (this.statusChart) this.statusChart.destroy();

    // Calculate total for percentage
    const total = draft + pending + approved + rejected;

    // Bar chart configuration following tooltip_chart.md
    const config: ChartConfiguration<'bar', number[], string> = {
      type: 'bar',
      data: {
        labels: ['Đang nháp', 'Chờ duyệt', 'Đã duyệt', 'Từ chối'],
        datasets: [{
          label: 'Số lượng dự án',
          data: [draft, pending, approved, rejected],
          backgroundColor: ['#94a3b8', '#f97316', '#22c55e', '#ef4444'],
          borderRadius: 6,
          borderSkipped: false,
        }]
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

  private destroyCharts(): void {
    if (this.statusChart) this.statusChart.destroy();
  }

  private animateKpi(field: 'kpiTotal' | 'kpiDraft' | 'kpiPending' | 'kpiApproved' | 'kpiRejected', to: number): void {
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

  // ================= UTILS =================
  badgeClass(st: DuAnTrangThai): string {
    if (st === 'DA_DUYET') return 'text-bg-success';
    if (st === 'CHO_DUYET_GIAM_DOC') return 'text-bg-warning';
    if (st === 'TU_CHOI') return 'text-bg-danger';
    return 'text-bg-secondary';
  }

  // NEW: Format status label (tiếng Việt)
  getStatusLabel(st: DuAnTrangThai): string {
    if (st === 'DANG_NHAP') return 'Đang nháp';
    if (st === 'CHO_DUYET_GIAM_DOC') return 'Chờ duyệt';
    if (st === 'DA_DUYET') return 'Đã duyệt';
    if (st === 'TU_CHOI') return 'Từ chối';
    return st;
  }

  // NEW: Badge class theo Design System
  getStatusBadgeClass(st: DuAnTrangThai): string {
    if (st === 'DANG_NHAP') return 'bg-secondary-subtle text-secondary';
    if (st === 'CHO_DUYET_GIAM_DOC') return 'bg-warning-subtle text-warning';
    if (st === 'DA_DUYET') return 'bg-success-subtle text-success';
    if (st === 'TU_CHOI') return 'bg-danger-subtle text-danger';
    return 'bg-secondary-subtle text-secondary';
  }

  toMsg(err: unknown): string {
    // giữ đơn giản, tránh phụ thuộc interceptor
    const anyErr = err as any;
    return anyErr?.error?.message || anyErr?.message || 'Có lỗi xảy ra';
  }

  private toNumberOrNull(v: unknown): number | null {
    if (v === null || v === undefined || v === '') return null;
    const n = Number(v);
    return Number.isFinite(n) ? n : null;
  }

  // ================= FILE ATTACHMENT =================
  isHrRole(): boolean {
    return this.authService.role === RoleCode.HR_ACC;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.uploadError = '';

    // Validate file type
    const allowedTypes = ['.pdf', '.doc', '.docx'];
    const ext = file.name.substring(file.name.lastIndexOf('.')).toLowerCase();
    if (!allowedTypes.includes(ext)) {
      this.uploadError = 'Chỉ chấp nhận file .pdf, .doc, .docx';
      input.value = '';
      return;
    }

    // Validate file size (10MB)
    if (file.size > 10 * 1024 * 1024) {
      this.uploadError = 'Dung lượng file tối đa 10MB';
      input.value = '';
      return;
    }

    this.uploadAttachment(file);
    input.value = ''; // Reset input
  }

  uploadAttachment(file: File): void {
    this.uploading = true;
    this.uploadError = '';

    this.api
      .uploadAttachment(this.selectedId, file)
      .pipe(finalize(() => (this.uploading = false)))
      .subscribe({
        next: (res) => {
          this.toast.success('Upload file thành công!');
          this.openDetail(this.selectedId);
        },
        error: (err) => {
          this.uploadError = this.toMsg(err);
          this.toast.danger('Upload file thất bại: ' + this.toMsg(err));
        },
      });
  }

  // ================= MULTI-FILE UPLOAD =================
  onMultiFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const files = Array.from(input.files);
    this.uploading = true;
    this.uploadError = '';

    this.api
      .uploadFiles(this.selectedId, files)
      .pipe(finalize(() => {
        this.uploading = false;
        input.value = ''; // Reset input
      }))
      .subscribe({
        next: (res) => {
          this.toast.success(`Upload ${files.length} file thành công!`);
          this.openDetail(this.selectedId);
        },
        error: (err) => {
          this.uploadError = this.toMsg(err);
          this.toast.danger('Upload files thất bại: ' + this.toMsg(err));
        },
      });
  }

  deleteFile(fileId: number): void {
    if (!confirm('Xóa file này?')) return;

    this.saving = true;
    this.api
      .deleteFile(this.selectedId, fileId)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Xóa file thành công!');
          this.openDetail(this.selectedId);
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Xóa file thất bại: ' + this.toMsg(err));
        },
      });
  }

  getFileUrl(url: string | null): string {
    if (!url) return '';
    return `${environment.apiBaseUrl}${url}`;
  }

  getFileIcon(fileName: string): string {
    const ext = fileName.substring(fileName.lastIndexOf('.')).toLowerCase();
    if (ext === '.pdf') return 'bi-file-pdf-fill text-danger';
    if (ext === '.doc' || ext === '.docx') return 'bi-file-word-fill text-primary';
    if (ext === '.xls' || ext === '.xlsx') return 'bi-file-excel-fill text-success';
    if (ext === '.jpg' || ext === '.jpeg' || ext === '.png') return 'bi-file-image-fill text-info';
    return 'bi-file-earmark-fill text-secondary';
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  // ================= MEMBER HELPERS =================
  isMemberAlreadyAdded(nvId: number): boolean {
    return this.selected.thanhViens.some(m => m.nvHoSoId === nvId);
  }

  // ================= TEMP MEMBER HELPERS (for form) =================
  isTempMemberAdded(nvId: number): boolean {
    return this.tempMembers.some(m => m.nvId === nvId);
  }

  selectTempMember(nv: NhanVienListItemDto): void {
    if (!this.isTempMemberAdded(nv.id)) {
      this.tempMembers.push({
        nvId: nv.id,
        hoTen: nv.hoTen,
        vaiTro: nv.tenChucVu || ''
      });
    }
  }

  removeTempMember(nvId: number): void {
    this.tempMembers = this.tempMembers.filter(m => m.nvId !== nvId);
  }

  // ================= APPROVAL WORKFLOW HELPERS =================
  canEdit(project: DuAnListItemDto | DuAnDetailDto): boolean {
    // Chỉ cho sửa khi: DANG_NHAP hoặc TU_CHOI
    return project.trangThaiDuAn === 'DANG_NHAP' || project.trangThaiDuAn === 'TU_CHOI';
  }

  canDelete(project: DuAnListItemDto): boolean {
    // Chỉ cho xóa khi DANG_NHAP
    return project.trangThaiDuAn === 'DANG_NHAP';
  }

  canRecall(project: DuAnDetailDto): boolean {
    // Chỉ cho thu hồi khi CHO_DUYET_GIAM_DOC
    return project.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC';
  }

  canSendApproval(project: DuAnDetailDto): boolean {
    // Chỉ cho gửi duyệt khi DANG_NHAP hoặc TU_CHOI
    return project.trangThaiDuAn === 'DANG_NHAP' || project.trangThaiDuAn === 'TU_CHOI';
  }

  recallApproval(): void {
    if (!confirm('Thu hồi yêu cầu duyệt dự án này?')) return;

    this.saving = true;
    this.api
      .recall(this.selectedId)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Thu hồi dự án thành công!');
          this.openDetail(this.selectedId);
          this.loadList();
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger('Thu hồi thất bại: ' + this.toMsg(err));
        },
      });
  }

  // ================= BUDGET FORMATTING =================
  get nganSachDisplay(): string {
    const value = this.form.get('nganSach')?.value;
    if (!value) return '';
    return this.formatCurrency(value);
  }

  onNganSachInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const rawValue = input.value.replace(/\./g, ''); // Remove dots
    const numValue = parseInt(rawValue, 10);

    if (isNaN(numValue)) {
      this.form.patchValue({ nganSach: null }, { emitEvent: false });
      input.value = '';
    } else {
      this.form.patchValue({ nganSach: numValue }, { emitEvent: false });
      input.value = this.formatCurrency(numValue);
    }
  }

  formatCurrency(value: number | null): string {
    if (!value) return '';
    return value.toString().replace(/\B(?=(\d{3})+(?!\d))/g, '.');
  }
}
