import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  AccountListItem,
  AccountDetail,
  AccountCreateDto,
  AccountUpdateDto,
  ResetPasswordRequest,
  VaiTro,
  EmployeeDropdownItem
} from 'src/app/core/models/tai-khoan.model';
import { NhanVienListItemDto, PagedResultDto } from 'src/app/core/models/nhan-vien.model';
import { HoSoCaNhanDto } from 'src/app/core/models/ho-so-ca-nhan.model';
import { TaiKhoanApiService } from 'src/app/core/services/api/tai-khoan-api.service';
import { NhanVienApiService } from 'src/app/core/services/api/nhan-vien-api.service';
import { MeApiService } from 'src/app/core/services/api/me-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { finalize, interval, Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-tai-khoan',
  templateUrl: './tai-khoan.component.html',
  styleUrls: ['./tai-khoan.component.scss']
})
export class TaiKhoanComponent implements OnInit, OnDestroy {
  loadingList = false;
  loadingDetail = false;
  saving = false;
  errorMsg = '';

  pageIndex = 1;
  pageSize = 10;
  totalCount = 0;
  keyword = '';
  statusFilter: 'ALL' | 'ACTIVE' | 'INACTIVE' = 'ALL';

  list: AccountListItem[] = [];
  filtered: AccountListItem[] = [];

  selectedId: number | null = null;
  detail?: AccountDetail;
  hoSoNhanVien?: HoSoCaNhanDto; // Thông tin đầy đủ nhân viên (bao gồm avatar)
  showDetailModal = false;
  showCreateModal = false;
  showEditModal = false;
  showResetPasswordModal = false;

  vaiTros: VaiTro[] = [];
  nhanViens: NhanVienListItemDto[] = [];
  employeesDropdown: EmployeeDropdownItem[] = []; // Danh sách NV với thông tin đã có TK
  currentUserVaiTroId: number = 999; // Mặc định = 999 để disable tất cả nếu chưa load
  avatarVersion = Date.now(); // Để tránh cache avatar

  // Quick employee creation
  newEmployeeName = ''; // Input tên NV mới
  creatingEmployee = false; // Đang tạo NV mới

  createForm: FormGroup;
  editForm: FormGroup;
  resetPasswordForm: FormGroup;

  // Password management
  showPasswordInCreateForm = true; // Hiển thị mật khẩu để HR thấy
  tempCredentials: { tenDangNhap: string; matKhau: string } | null = null; // Lưu tạm sau khi tạo
  loadingTempPassword = false;
  viewedTempPassword: string | null = null; // Mật khẩu tạm đã load

  // Auto-refresh để cập nhật thời gian đăng nhập lần cuối real-time
  private refreshInterval$?: Subscription;
  private readonly AUTO_REFRESH_MS = 10000; // 10 giây

  constructor(
    private api: TaiKhoanApiService,
    private nhanVienApi: NhanVienApiService,
    private toast: ToastService,
    private fb: FormBuilder,
    private meApi: MeApiService
  ) {
    this.createForm = this.fb.group({
      tenDangNhap: ['', [Validators.required, Validators.minLength(3)]],
      matKhau: ['', [Validators.required, Validators.minLength(6)]],
      matKhauXacNhan: ['', Validators.required],
      vaiTroId: [null, Validators.required],
      nvHoSoId: [null],
      trangThai: [true]
    });

    this.editForm = this.fb.group({
      vaiTroId: [null, Validators.required],
      nvHoSoId: [null],
      trangThai: [true]
    });

    this.resetPasswordForm = this.fb.group({
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    });
  }

  ngOnInit(): void {
    this.loadCurrentUserVaiTroId();
    this.loadList();
    this.loadVaiTros();
    this.loadNhanViens();
    this.loadEmployeesForDropdown(); // Load danh sách NV với thông tin đã có TK

    // Auto-refresh danh sách mỗi 10s để cập nhật thời gian đăng nhập lần cuối
    this.refreshInterval$ = interval(this.AUTO_REFRESH_MS).subscribe(() => {
      this.loadListSilent();
    });
  }

  ngOnDestroy(): void {
    this.refreshInterval$?.unsubscribe();
  }

  /**
   * Load danh sách silent (không hiện loading) để cập nhật thời gian đăng nhập
   */
  private loadListSilent(): void {
    this.api.getPaged(this.pageIndex, this.pageSize, this.keyword).subscribe({
      next: (res: PagedResultDto<AccountListItem>) => {
        this.list = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.applyStatusFilter();
      }
    });
  }

  // ============================
  // LOAD DATA
  // ============================

  loadList(triggeredByUser = false): void {
    this.loadingList = true;
    this.errorMsg = '';
    this.api
      .getPaged(this.pageIndex, this.pageSize, this.keyword)
      .pipe(finalize(() => (this.loadingList = false)))
      .subscribe({
        next: (res: PagedResultDto<AccountListItem>) => {
          this.list = res.items || [];
          this.totalCount = res.totalCount || 0;
          this.applyStatusFilter();
          if (triggeredByUser) {
            this.toast.success('Đã tải danh sách tài khoản');
          }
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger(this.errorMsg);
        },
      });
  }

  /**
   * Load vaiTroId của user hiện tại để kiểm tra quyền sửa/xóa
   */
  loadCurrentUserVaiTroId(): void {
    this.meApi.getMyProfile().subscribe({
      next: (profile) => {
        this.currentUserVaiTroId = profile.vaiTroId;
        console.log('[TaiKhoanComponent] Current user vaiTroId:', this.currentUserVaiTroId);
      },
      error: (err) => {
        console.error('[TaiKhoanComponent] Lỗi tải vaiTroId:', err);
        this.currentUserVaiTroId = 999; // Fallback - disable all
      }
    });
  }

  loadVaiTros(): void {
    console.log('[TaiKhoanComponent] Loading vai tros...');
    this.api.getVaiTros().subscribe({
      next: (res) => {
        console.log('[TaiKhoanComponent] Vai tros loaded:', res);
        // API đã filter trangThai = true rồi
        this.vaiTros = res;
        console.log('[TaiKhoanComponent] Total active vai tros:', this.vaiTros.length);
      },
      error: (err) => {
        console.error('[TaiKhoanComponent] Lỗi tải vai trò:', err);
        this.toast.danger('Không thể tải danh sách vai trò: ' + (err.error?.message || err.message));
      }
    });
  }

  /**
   * Load danh sách NV cũ (deprecated - giữ lại cho tương thích)
   */
  loadNhanViens(): void {
    this.api.getNhanViensForDropdown().subscribe({
      next: (res) => {
        this.nhanViens = res.items.filter(nv => nv.trangThaiLamViec === 1);
      },
      error: (err) => {
        console.error('Lỗi tải nhân viên:', err);
      }
    });
  }

  /**
   * Load danh sách NV mới với thông tin đã có TK hay chưa
   */
  loadEmployeesForDropdown(): void {
    this.api.getEmployeesForDropdownV2().subscribe({
      next: (res) => {
        this.employeesDropdown = res;
        console.log('[TaiKhoanComponent] Employees loaded:', this.employeesDropdown.length);
      },
      error: (err) => {
        console.error('Lỗi tải danh sách nhân viên:', err);
        this.toast.warning('Không thể tải danh sách nhân viên');
      }
    });
  }

  /**
   * Tạo nhân viên nhanh từ input name
   */
  createQuickEmployee(): void {
    if (!this.newEmployeeName.trim()) {
      this.toast.warning('Vui lòng nhập họ tên nhân viên');
      return;
    }

    this.creatingEmployee = true;
    this.api.createQuickEmployee({ hoTen: this.newEmployeeName.trim() }).subscribe({
      next: (res) => {
        this.toast.success(`Đã tạo nhân viên: ${res.hoTen} (${res.maNhanVien})`);
        // Refresh dropdown và tự động chọn NV vừa tạo
        this.loadEmployeesForDropdown();
        this.createForm.patchValue({ nvHoSoId: res.id });
        this.newEmployeeName = '';
        this.creatingEmployee = false;
      },
      error: (err) => {
        console.error('Lỗi tạo nhân viên:', err);
        this.toast.danger(err.error?.message || 'Không thể tạo nhân viên');
        this.creatingEmployee = false;
      }
    });
  }

  /**
   * Lấy danh sách NV chưa có TK
   */
  get availableEmployees(): EmployeeDropdownItem[] {
    return this.employeesDropdown.filter(e => !e.daCoTaiKhoan);
  }

  /**
   * Lấy danh sách NV đã có TK
   */
  get assignedEmployees(): EmployeeDropdownItem[] {
    return this.employeesDropdown.filter(e => e.daCoTaiKhoan);
  }

  /**
   * Kiểm tra NV có đang được gán cho TK khác không
   * @param nvId ID nhân viên
   * @param excludeAccountId ID tài khoản đang edit (loại trừ)
   */
  isEmployeeAssignedToOther(nvId: number | null, excludeAccountId?: number): boolean {
    if (!nvId) return false;
    const emp = this.employeesDropdown.find(e => e.id === nvId);
    if (!emp || !emp.daCoTaiKhoan) return false;
    // Nếu đang edit và NV này thuộc TK hiện tại thì OK
    if (excludeAccountId && emp.taiKhoanId === excludeAccountId) return false;
    return true;
  }

  applyStatusFilter(): void {
    this.filtered = this.list.filter((x) => {
      if (this.statusFilter === 'ACTIVE') return x.trangThai === true;
      if (this.statusFilter === 'INACTIVE') return x.trangThai === false;
      return true;
    });
  }

  // ============================
  // PAGINATION
  // ============================

  changePage(index: number): void {
    if (index < 1) return;
    const maxPage = Math.ceil(this.totalCount / this.pageSize);
    if (index > maxPage) return;
    this.pageIndex = index;
    this.loadList();
  }

  getTotalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  getPageNumbers(): number[] {
    const totalPages = this.getTotalPages();
    const pages: number[] = [];
    const maxVisible = 5;

    if (totalPages <= maxVisible) {
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      if (this.pageIndex <= 3) {
        for (let i = 1; i <= maxVisible; i++) {
          pages.push(i);
        }
      } else if (this.pageIndex >= totalPages - 2) {
        for (let i = totalPages - maxVisible + 1; i <= totalPages; i++) {
          pages.push(i);
        }
      } else {
        for (let i = this.pageIndex - 2; i <= this.pageIndex + 2; i++) {
          pages.push(i);
        }
      }
    }
    return pages;
  }

  // ============================
  // MODALS
  // ============================

  /**
   * Mở modal chi tiết tài khoản
   * API getById() sẽ tự động thêm timestamp và cache-control headers
   * để lấy dữ liệu real-time (đặc biệt là lanDangNhapCuoi)
   */
  openDetail(id: number): void {
    this.selectedId = id;
    this.loadingDetail = true;
    this.avatarVersion = Date.now(); // Refresh avatar để tránh cache
    this.api.getById(id)
      .pipe(finalize(() => (this.loadingDetail = false)))
      .subscribe({
        next: (res) => {
          this.detail = res;
          this.showDetailModal = true;

          // Nếu có gán nhân viên, lấy thêm thông tin đầy đủ (bao gồm avatar)
          if (res.nvHoSoId) {
            this.nhanVienApi.getFullProfileById(res.nvHoSoId).subscribe({
              next: (hoSo) => {
                this.hoSoNhanVien = hoSo;
              },
              error: (err) => {
                console.error('Không thể tải hồ sơ nhân viên:', err);
              }
            });
          } else {
            this.hoSoNhanVien = undefined;
          }
        },
        error: (err) => {
          this.toast.danger('Không thể tải chi tiết: ' + this.toMsg(err));
        },
      });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedId = null;
    this.detail = undefined;
    this.hoSoNhanVien = undefined;
    this.viewedTempPassword = null;
  }

  openCreateModal(): void {
    const randomPassword = this.generateRandomPassword();
    this.createForm.reset({ 
      trangThai: true,
      matKhau: randomPassword,
      matKhauXacNhan: randomPassword
    });
    this.tempCredentials = null;
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.createForm.reset();
    this.tempCredentials = null;
  }

  openEditModal(): void {
    if (!this.detail) return;
    this.editForm.patchValue({
      vaiTroId: this.detail.vaiTroId,
      nvHoSoId: this.detail.nvHoSoId,
      trangThai: this.detail.trangThai
    });
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.editForm.reset();
  }

  openResetPasswordModal(): void {
    this.resetPasswordForm.reset();
    this.showResetPasswordModal = true;
  }

  closeResetPasswordModal(): void {
    this.showResetPasswordModal = false;
    this.resetPasswordForm.reset();
  }

  // ============================
  // CRUD OPERATIONS
  // ============================

  onCreate(): void {
    if (this.createForm.invalid) {
      this.toast.warning('Vui lòng điền đầy đủ thông tin!');
      return;
    }

    const { matKhau, matKhauXacNhan, ...rest } = this.createForm.value;
    if (matKhau !== matKhauXacNhan) {
      this.toast.warning('Mật khẩu xác nhận không khớp!');
      return;
    }

    const dto: AccountCreateDto = {
      ...rest,
      matKhau,
    };

    this.saving = true;
    this.api.create(dto)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: (res) => {
          this.toast.success('Tạo tài khoản thành công!');
          this.closeCreateModal();
          this.loadList();
        },
        error: (err) => {
          this.toast.danger('Lỗi: ' + this.toMsg(err));
        },
      });
  }

  onUpdate(): void {
    if (this.editForm.invalid || !this.selectedId) {
      this.toast.warning('Vui lòng điền đầy đủ thông tin!');
      return;
    }

    const dto: AccountUpdateDto = this.editForm.value;

    this.saving = true;
    this.api.update(this.selectedId, dto)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: (res) => {
          this.toast.success('Cập nhật tài khoản thành công!');
          this.detail = res;
          this.closeEditModal();
          this.loadList();
        },
        error: (err) => {
          this.toast.danger('Lỗi: ' + this.toMsg(err));
        },
      });
  }

  onResetPassword(): void {
    if (this.resetPasswordForm.invalid || !this.selectedId) {
      this.toast.warning('Vui lòng điền đầy đủ thông tin!');
      return;
    }

    const { newPassword, confirmPassword } = this.resetPasswordForm.value;
    if (newPassword !== confirmPassword) {
      this.toast.warning('Mật khẩu xác nhận không khớp!');
      return;
    }

    const dto: ResetPasswordRequest = { newPassword };

    this.saving = true;
    this.api.resetPassword(this.selectedId, dto)
      .pipe(finalize(() => (this.saving = false)))
      .subscribe({
        next: () => {
          this.toast.success('Reset mật khẩu thành công!');
          this.closeResetPasswordModal();
        },
        error: (err) => {
          this.toast.danger('Lỗi: ' + this.toMsg(err));
        },
      });
  }

  onDelete(id: number): void {
    if (!confirm('Bạn có chắc muốn xóa tài khoản này?')) return;

    this.api.delete(id).subscribe({
      next: () => {
        this.toast.success('Xóa tài khoản thành công!');
        this.closeDetailModal();
        this.loadList();
      },
      error: (err) => {
        this.toast.danger('Lỗi: ' + this.toMsg(err));
      },
    });
  }

  // ============================
  // HELPERS
  // ============================

  /**
   * Sinh mật khẩu ngẫu nhiên 12 ký tự
   * Bao gồm: chữ hoa, chữ thường, số, ký tự đặc biệt
   */
  generateRandomPassword(): string {
    const lowercase = 'abcdefghijklmnopqrstuvwxyz';
    const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const numbers = '0123456789';
    const special = '!@#$%^&*';
    const all = lowercase + uppercase + numbers + special;

    let password = '';
    // Đảm bảo có ít nhất 1 ký tự mỗi loại
    password += lowercase[Math.floor(Math.random() * lowercase.length)];
    password += uppercase[Math.floor(Math.random() * uppercase.length)];
    password += numbers[Math.floor(Math.random() * numbers.length)];
    password += special[Math.floor(Math.random() * special.length)];

    // Thêm 8 ký tự ngẫu nhiên nữa
    for (let i = 0; i < 8; i++) {
      password += all[Math.floor(Math.random() * all.length)];
    }

    // Xáo trộn
    return password.split('').sort(() => Math.random() - 0.5).join('');
  }

  /**
   * Sinh lại mật khẩu mới trong form tạo tài khoản
   */
  regeneratePassword(): void {
    const newPassword = this.generateRandomPassword();
    this.createForm.patchValue({
      matKhau: newPassword,
      matKhauXacNhan: newPassword
    });
  }

  /**
   * Copy thông tin đăng nhập vào clipboard
   */
  copyCredentials(username: string, password: string): void {
    const text = `Tài khoản: ${username}\nMật khẩu: ${password}`;
    navigator.clipboard.writeText(text).then(() => {
      this.toast.success('Đã copy thông tin đăng nhập!');
    }).catch(() => {
      this.toast.danger('Không thể copy vào clipboard!');
    });
  }

  /**
   * Xem mật khẩu tạm của tài khoản chưa đăng nhập
   * Chỉ gọi khi canViewPassword = true (từ BE)
   */
  viewTempPassword(): void {
    if (!this.selectedId || !this.detail?.canViewPassword) return;

    this.loadingTempPassword = true;
    this.api.getTempPassword(this.selectedId).subscribe({
      next: (res) => {
        this.viewedTempPassword = res.matKhauTam;
        this.loadingTempPassword = false;
      },
      error: (err) => {
        console.error('Lỗi lấy mật khẩu tạm:', err);
        this.toast.danger(err.error?.message || 'Không thể xem mật khẩu');
        this.loadingTempPassword = false;
      }
    });
  }

  /**
   * Kiểm tra quyền sửa/xóa tài khoản
   * Chỉ cho phép nếu vaiTroId của current user <= vaiTroId của account
   * Ví dụ: HR_KETOAN (vaiTroId=2) chỉ được sửa/xóa HR_KETOAN (vaiTroId=2) hoặc EMPLOYEE (vaiTroId=3)
   *         Không được sửa/xóa SUPER_ADMIN (vaiTroId=1) hoặc cấp cao hơn
   */
  canEditOrDelete(accountVaiTroId: number): boolean {
    return this.currentUserVaiTroId <= accountVaiTroId;
  }

  getStatusBadge(trangThai: boolean): string {
    return trangThai ? 'badge bg-success-subtle text-success' : 'badge bg-danger-subtle text-danger';
  }

  getStatusLabel(trangThai: boolean): string {
    return trangThai ? 'Hoạt động' : 'Vô hiệu hóa';
  }

  getVaiTroName(id: number): string {
    const vt = this.vaiTros.find(v => v.id === id);
    return vt ? vt.tenVaiTro : '—';
  }

  getNhanVienName(id: number | null): string {
    if (!id) return '—';
    const nv = this.nhanViens.find(n => n.id === id);
    return nv ? `${nv.hoTen} (${nv.maNhanVien})` : '—';
  }

  /**
   * Format ngày giờ lần đăng nhập cuối theo kiểu real-time:
   * - "Vừa xong" (dưới 1 phút)
   * - "5 phút trước"
   * - "2 giờ trước"
   * - "Hôm nay 14:30"
   * - "Hôm qua 09:15"
   * - "3 ngày trước"
   * - "31/12/2024 14:30"
   */
  formatLastLogin(dateStr: string | null): string {
    if (!dateStr) return 'Chưa đăng nhập';

    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffSeconds = Math.floor(diffMs / 1000);
    const diffMinutes = Math.floor(diffSeconds / 60);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);

    const timeStr = date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });

    // Real-time format
    if (diffSeconds < 60) {
      return 'Vừa xong';
    }
    if (diffMinutes < 60) {
      return `${diffMinutes} phút trước`;
    }
    if (diffHours < 24 && this.isSameDay(date, now)) {
      return `Hôm nay ${timeStr}`;
    }
    if (diffDays === 1 || this.isYesterday(date, now)) {
      return `Hôm qua ${timeStr}`;
    }
    if (diffDays < 7) {
      return `${diffDays} ngày trước`;
    }
    return date.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private isSameDay(d1: Date, d2: Date): boolean {
    return d1.getDate() === d2.getDate() &&
      d1.getMonth() === d2.getMonth() &&
      d1.getFullYear() === d2.getFullYear();
  }

  private isYesterday(date: Date, now: Date): boolean {
    const yesterday = new Date(now);
    yesterday.setDate(yesterday.getDate() - 1);
    return this.isSameDay(date, yesterday);
  }

  /**
   * Lấy URL đầy đủ của avatar nhân viên
   */
  getAvatarUrl(): string | null {
    if (!this.hoSoNhanVien?.anhCaNhanUrl) return null;

    let url = this.hoSoNhanVien.anhCaNhanUrl;
    // Nếu là relative path, thêm base URL
    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      url = `${environment.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    }

    // Thêm timestamp để tránh cache
    return `${url}?t=${this.avatarVersion}`;
  }

  /**
   * Lấy chữ cái đầu từ họ tên để hiển thị khi không có avatar
   */
  getInitial(hoTen: string | null | undefined): string {
    if (!hoTen) return '?';
    const words = hoTen.trim().split(/\s+/);
    if (words.length >= 2) {
      // Lấy chữ cái đầu của tên (từ cuối) và họ (từ đầu)
      return (words[words.length - 1][0] + words[0][0]).toUpperCase();
    }
    return hoTen[0].toUpperCase();
  }

  private toMsg(err: any): string {
    return err?.error?.message || err?.message || 'Có lỗi xảy ra';
  }
}
