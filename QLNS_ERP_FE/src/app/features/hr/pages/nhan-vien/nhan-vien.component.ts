import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  NhanVienCreateDto,
  NhanVienDetailDto,
  NhanVienListItemDto,
  NhanVienUpdateDto,
  PagedResultDto,
  PagingRequestDto,
  TrangThaiLamViec,
} from 'src/app/core/models/nhan-vien.model';
import { HoSoCaNhanDto } from 'src/app/core/models/ho-so-ca-nhan.model';
import { NhanVienApiService } from 'src/app/core/services/api/nhan-vien-api.service';
import { LookupApiService, PhongBanLookupDto, ChucVuLookupDto } from 'src/app/core/services/api/lookup-api.service';
import { finalize, forkJoin } from 'rxjs';
import { ToastService } from 'src/app/shared/services/toast.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-nhan-vien',
  templateUrl: './nhan-vien.component.html',
  styleUrls: ['./nhan-vien.component.scss']
})
export class NhanVienComponent implements OnInit {
  loadingList = false;
  loadingDetail = false;
  savingWork = false;
  creating = false;
  errorMsg = '';

  pageIndex = 1;
  pageSize = 10;
  totalCount = 0;
  keyword = '';
  statusFilter: 'ALL' | 'ACTIVE' | 'RESIGNED' = 'ALL';

  list: NhanVienListItemDto[] = [];
  filtered: NhanVienListItemDto[] = [];

  selectedId: number | null = null;
  detail?: HoSoCaNhanDto;
  showDetailModal = false;
  showCreateModal = false;
  editWorkMode = false;
  avatarVersion = Date.now();

  // Dropdown data
  phongBanList: PhongBanLookupDto[] = [];
  chucVuList: ChucVuLookupDto[] = [];
  loadingLookup = false;

  hopDongFile: File | null = null;

  workForm: FormGroup;
  createForm: FormGroup;

  constructor(
    private api: NhanVienApiService,
    private lookupApi: LookupApiService,
    private toast: ToastService,
    private fb: FormBuilder
  ) {
    this.workForm = this.fb.group({
      phongBanId: [null, Validators.required],
      chucVuId: [null, Validators.required],
      ngayVaoLam: [null, Validators.required],
      ngayNghiViec: [null],
      loaiHopDong: [''],
      ngayKyHopDong: [null],
      ngayHetHanHopDong: [null],
      trangThaiLamViec: [1, Validators.required],
      ghiChu: [''],
    });

    this.createForm = this.fb.group({
      maNhanVien: ['', Validators.required],
      hoTen: ['', Validators.required],
      ngaySinh: [null],
      gioiTinh: [null],
      diaChi: [''],
      soDienThoai: [''],
      emailCaNhan: [''],
      phongBanId: [null, Validators.required],
      chucVuId: [null, Validators.required],
      ngayVaoLam: [null, Validators.required],
      loaiHopDong: [''],
      ngayKyHopDong: [null],
      ngayHetHanHopDong: [null],
    });
  }

  ngOnInit(): void {
    this.loadList();
    this.loadLookupData();
  }

  loadLookupData(): void {
    this.loadingLookup = true;
    forkJoin({
      phongBan: this.lookupApi.getPhongBanList(),
      chucVu: this.lookupApi.getChucVuList()
    })
      .pipe(finalize(() => (this.loadingLookup = false)))
      .subscribe({
        next: (res) => {
          this.phongBanList = res.phongBan;
          this.chucVuList = res.chucVu;
        },
        error: (err) => {
          console.error('Error loading lookup data:', err);
          this.toast.danger('Không thể tải danh sách phòng ban/chức vụ');
        }
      });
  }

  loadList(triggeredByUser = false): void {
    const request: PagingRequestDto = {
      pageIndex: this.pageIndex,
      pageSize: this.pageSize,
      keyword: this.keyword.trim(),
    };

    this.loadingList = true;
    this.errorMsg = '';
    this.api
      .getPaged(request)
      .pipe(finalize(() => (this.loadingList = false)))
      .subscribe({
        next: (res: PagedResultDto<NhanVienListItemDto>) => {
          this.list = res.items || [];
          this.totalCount = res.totalCount || 0;
          this.applyStatusFilter();
          if (triggeredByUser) {
            this.toast.success('Đã tải danh sách nhân viên');
          }
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger(this.errorMsg);
        },
      });
  }

  applyStatusFilter(): void {
    this.filtered = this.list.filter((x) => {
      if (this.statusFilter === 'ACTIVE') return x.trangThaiLamViec === 1;
      if (this.statusFilter === 'RESIGNED') return x.trangThaiLamViec !== 1;
      return true;
    });
  }

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

  openDetail(id: number): void {
    this.selectedId = id;
    this.detail = undefined;
    this.loadingDetail = true;
    this.showDetailModal = true;
    this.editWorkMode = false;
    this.avatarVersion = Date.now();
    this.api
      .getFullProfileById(id)
      .pipe(finalize(() => (this.loadingDetail = false)))
      .subscribe({
        next: (res) => {
          console.log('[HR NhanVien] ===== DETAIL LOADED =====');
          console.log('[HR NhanVien] Full response:', res);
          console.log('[HR NhanVien] STK:', res.soTaiKhoanNganHang);
          console.log('[HR NhanVien] Has tenDangNhap?', !!res.tenDangNhap);
          console.log('[HR NhanVien] Has congViecHienTai?', !!res.congViecHienTai);
          console.log('[HR NhanVien] ================================');

          this.detail = res;
          this.avatarVersion = Date.now();
          this.patchWorkForm(res);
          this.toast.info('Đã tải chi tiết nhân viên');
        },
        error: (err) => {
          console.error('[HR NhanVien] ❌ Error loading detail:', err);
          this.errorMsg = this.toMsg(err);
          this.toast.danger(this.errorMsg);
        },
      });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.selectedId = null;
    this.detail = undefined;
    this.editWorkMode = false;
    this.workForm.reset();
  }

  startEditWork(): void {
    if (!this.detail) return;
    this.editWorkMode = true;
  }

  cancelEditWork(): void {
    if (this.detail) {
      this.patchWorkForm(this.detail);
    }
    this.editWorkMode = false;
  }

  saveWork(): void {
    if (!this.detail || !this.selectedId) return;
    if (this.workForm.invalid) {
      this.workForm.markAllAsTouched();
      return;
    }

    const form = this.workForm.value;
    const dto: NhanVienUpdateDto = {
      hoTen: this.detail.hoTen,
      anhCaNhanUrl: this.detail.anhCaNhanUrl || null,
      ngaySinh: this.detail.ngaySinh || null,
      gioiTinh: this.detail.gioiTinh ?? null,
      diaChi: this.detail.diaChi || null,
      soDienThoai: this.detail.soDienThoai || null,
      emailCaNhan: this.detail.emailCaNhan || null,
      soTaiKhoanNganHang: this.detail.soTaiKhoanNganHang || null,
      anhStkUrl: this.detail.anhStkUrl || null,
      phongBanId: Number(form.phongBanId),
      chucVuId: Number(form.chucVuId),
      ngayVaoLam: form.ngayVaoLam,
      ngayNghiViec: form.ngayNghiViec || null,
      loaiHopDong: form.loaiHopDong || null,
      ngayKyHopDong: form.ngayKyHopDong || null,
      ngayHetHanHopDong: form.ngayHetHanHopDong || null,
      trangThaiLamViec: Number(form.trangThaiLamViec),
      ghiChu: form.ghiChu || null,
    };

    this.savingWork = true;
    this.api
      .update(this.detail.nvHoSoId, dto)
      .pipe(finalize(() => (this.savingWork = false)))
      .subscribe({
        next: () => {
          this.editWorkMode = false;
          this.toast.success('Đã cập nhật thông tin công việc');
          // Reload detail với full profile
          this.loadingDetail = true;
          this.api.getFullProfileById(this.detail!.nvHoSoId)
            .pipe(finalize(() => (this.loadingDetail = false)))
            .subscribe({
              next: (res) => {
                this.detail = res;
                this.patchWorkForm(res);
                this.loadList();
              }
            });
        },
        error: (err) => {
          this.errorMsg = this.toMsg(err);
          this.toast.danger(this.errorMsg);
        },
      });
  }

  openCreateModal(): void {
    this.hopDongFile = null;
    this.createForm.reset({
      maNhanVien: '',
      hoTen: '',
      ngaySinh: null,
      gioiTinh: null,
      diaChi: '',
      soDienThoai: '',
      emailCaNhan: '',
      phongBanId: null,
      chucVuId: null,
      ngayVaoLam: null,
      loaiHopDong: '',
      ngayKyHopDong: null,
      ngayHetHanHopDong: null,
    });
    this.showCreateModal = true;
  }

  onHopDongFileSelected(event: Event): void {
    this.hopDongFile = (event.target as HTMLInputElement).files?.[0] ?? null;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  saveCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    const val = this.createForm.value;
    const dto: NhanVienCreateDto = {
      maNhanVien: val.maNhanVien?.trim(),
      hoTen: val.hoTen?.trim(),
      ngaySinh: val.ngaySinh || null,
      gioiTinh: val.gioiTinh ?? null,
      diaChi: val.diaChi || null,
      soDienThoai: val.soDienThoai || null,
      emailCaNhan: val.emailCaNhan || null,
      soTaiKhoanNganHang: null,
      anhStkUrl: null,
      phongBanId: Number(val.phongBanId),
      chucVuId: Number(val.chucVuId),
      ngayVaoLam: val.ngayVaoLam,
      loaiHopDong: val.loaiHopDong || null,
      ngayKyHopDong: val.ngayKyHopDong || null,
      ngayHetHanHopDong: val.ngayHetHanHopDong || null,
    };

    this.creating = true;
    this.api.create(dto).subscribe({
      next: (result) => {
        if (this.hopDongFile) {
          this.api.uploadHopDong(result.id, this.hopDongFile)
            .pipe(finalize(() => (this.creating = false)))
            .subscribe({
              next: () => {
                this.toast.success('Đã thêm nhân viên mới (kèm hợp đồng)');
                this.showCreateModal = false;
                this.loadList(true);
              },
              error: () => {
                this.toast.success('Đã thêm nhân viên mới (lỗi tải hợp đồng, thử lại sau)');
                this.showCreateModal = false;
                this.loadList(true);
              }
            });
        } else {
          this.creating = false;
          this.toast.success('Đã thêm nhân viên mới');
          this.showCreateModal = false;
          this.loadList(true);
        }
      },
      error: (err) => {
        this.creating = false;
        this.errorMsg = this.toMsg(err);
        this.toast.danger(this.errorMsg);
      },
    });
  }

  getAvatarUrl(): string | null {
    if (!this.detail?.anhCaNhanUrl) return null;

    let url = this.detail.anhCaNhanUrl;
    // Nếu là relative path, thêm base URL
    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      url = `${environment.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    }

    // Thêm timestamp để tránh cache
    return `${url}?t=${this.avatarVersion}`;
  }

  private patchWorkForm(detail: HoSoCaNhanDto): void {
    const workInfo = detail.congViecHienTai;
    this.workForm.reset({
      phongBanId: workInfo?.phongBanId ?? null,
      chucVuId: workInfo?.chucVuId ?? null,
      ngayVaoLam: workInfo?.ngayVaoLam || null,
      ngayNghiViec: workInfo?.ngayNghiViec || null,
      loaiHopDong: workInfo?.loaiHopDong || '',
      ngayKyHopDong: workInfo?.ngayKyHopDong || null,
      ngayHetHanHopDong: workInfo?.ngayHetHanHopDong || null,
      trangThaiLamViec: workInfo?.trangThaiLamViec ?? 1,
      ghiChu: workInfo?.ghiChu || '',
    });
  }

  toMsg(err: any): string {
    if (!err) return 'Đã xảy ra lỗi';
    if (typeof err === 'string') return err;
    if (err.error?.message) return err.error.message;
    return err.message || 'Đã xảy ra lỗi';
  }

  getStatusBadge(trangThai: TrangThaiLamViec): string {
    return trangThai === 1
      ? 'badge rounded-pill bg-success-subtle text-success'
      : 'badge rounded-pill bg-secondary-subtle text-secondary';
  }

  getStatusLabel(trangThai: TrangThaiLamViec): string {
    return trangThai === 1 ? 'Đang làm' : 'Nghỉ việc';
  }

  getInitial(name?: string | null): string {
    if (!name || name.length === 0) return '?';
    return name.charAt(0).toUpperCase();
  }

  getGenderText(value: number | null | undefined): string {
    if (value === 1) return 'Nam';
    if (value === 2 || value === 0) return 'Nữ';
    return '—';
  }

  // ===== STK Image Modal for HR =====
  showStkModal = false;
  stkModalImageUrl = '';

  getFullUrl(url: string | null | undefined): string {
    if (!url) return '';
    if (url.startsWith('http://') || url.startsWith('https://')) {
      return url;
    }
    return `${environment.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
  }

  openStkImageModal(url: string | null | undefined): void {
    if (!url) return;
    this.stkModalImageUrl = this.getFullUrl(url);
    this.showStkModal = true;
  }

  closeStkImageModal(): void {
    this.showStkModal = false;
    this.stkModalImageUrl = '';
  }
}
