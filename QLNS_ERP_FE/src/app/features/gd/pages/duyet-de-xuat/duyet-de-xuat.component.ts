// src/app/features/gd/pages/duyet-de-xuat/duyet-de-xuat.component.ts

import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import {
  DeXuatGiamDocListItemDto,
  DeXuatGiamDocDetailDto,
  DeXuatTrangThai,
  DEXUAT_TRANGTHAI_LABEL,
  DEXUAT_TRANGTHAI_BADGE,
} from 'src/app/core/models/de-xuat-giam-doc.model';
import { DeXuatGiamDocApiService } from 'src/app/core/services/api/de-xuat-giam-doc-api.service';

@Component({
  selector: 'app-duyet-de-xuat',
  templateUrl: './duyet-de-xuat.component.html',
  styleUrls: ['./duyet-de-xuat.component.scss'],
})
export class DuyetDeXuatComponent implements OnInit, OnDestroy {

  // ─── State ──────────────────────────────────────────────────────────
  loading = false;
  saving = false;

  // ─── Data ────────────────────────────────────────────────────────────
  list: DeXuatGiamDocListItemDto[] = [];
  filtered: DeXuatGiamDocListItemDto[] = [];
  q = '';
  statusFilter: DeXuatTrangThai | 'ALL' = 'CHO_DUYET';

  // ─── KPI ─────────────────────────────────────────────────────────────
  kpiTong = 0;
  kpiChoDuyet = 0;
  kpiDaDuyet = 0;
  kpiTuChoi = 0;

  // ─── Detail + Approve Modal ──────────────────────────────────────────
  showDetailModal = false;
  detailLoading = false;
  selectedDetail: DeXuatGiamDocDetailDto | null = null;
  selectedId = 0;
  approveForm: FormGroup;

  readonly apiBase = environment.apiBaseUrl;
  readonly LABEL = DEXUAT_TRANGTHAI_LABEL;
  readonly BADGE = DEXUAT_TRANGTHAI_BADGE;

  private entityUpdateSub?: Subscription;
  private notificationSub?: Subscription;

  statusOptions: { value: DeXuatTrangThai | 'ALL'; label: string }[] = [
    { value: 'ALL', label: 'Tất cả' },
    { value: 'CHO_DUYET', label: 'Chờ duyệt' },
    { value: 'DA_DUYET', label: 'Đã duyệt' },
    { value: 'TU_CHOI', label: 'Từ chối' },
    { value: 'NHAP', label: 'Nháp' },
    { value: 'DA_THU_HOI', label: 'Đã thu hồi' },
  ];

  constructor(
    private api: DeXuatGiamDocApiService,
    private fb: FormBuilder,
    private toast: ToastService,
    private signalr: SignalrService,
  ) {
    this.approveForm = this.fb.group({
      dongY: [null as boolean | null],
      lyDoTuChoi: [''],
    });
  }

  ngOnInit(): void {
    this.loadList();

    // Realtime: tự động reload danh sách khi có đề xuất mới hoặc được cập nhật
    this.entityUpdateSub = this.signalr.entityUpdate$.subscribe(update => {
      if (update.entityType === 'DE_XUAT_GIAM_DOC') {
        this.loadList();
      }
    });

    // Realtime: nhận thông báo mới cũng reload (kèm cập nhật badge)
    this.notificationSub = this.signalr.notification$.subscribe(n => {
      if (n.relatedEntity === 'DE_XUAT_GIAM_DOC') {
        this.loadList();
      }
    });
  }

  ngOnDestroy(): void {
    this.entityUpdateSub?.unsubscribe();
    this.notificationSub?.unsubscribe();
  }

  // ─── Load ─────────────────────────────────────────────────────────────
  loadList(): void {
    this.loading = true;
    this.api.getList().subscribe({
      next: (data) => {
        this.list = data;
        this.calcKpi();
        this.applyFilter();
        this.loading = false;
      },
      error: () => {
        this.toast.danger('Không tải được danh sách đề xuất');
        this.loading = false;
      },
    });
  }

  calcKpi(): void {
    this.kpiTong = this.list.length;
    this.kpiChoDuyet = this.list.filter(x => x.trangThai === 'CHO_DUYET').length;
    this.kpiDaDuyet = this.list.filter(x => x.trangThai === 'DA_DUYET').length;
    this.kpiTuChoi = this.list.filter(x => x.trangThai === 'TU_CHOI').length;
  }

  applyFilter(): void {
    let data = [...this.list];
    if (this.statusFilter !== 'ALL') data = data.filter(x => x.trangThai === this.statusFilter);
    if (this.q.trim()) {
      const kw = this.q.toLowerCase();
      data = data.filter(x =>
        x.tenDeXuat.toLowerCase().includes(kw) ||
        (x.tenNguoiTao ?? '').toLowerCase().includes(kw) ||
        (x.moTa ?? '').toLowerCase().includes(kw)
      );
    }
    this.filtered = data;
  }

  // ─── Detail modal ─────────────────────────────────────────────────────
  openDetail(id: number): void {
    this.selectedId = id;
    this.showDetailModal = true;
    this.detailLoading = true;
    this.selectedDetail = null;
    this.approveForm.reset({ dongY: null, lyDoTuChoi: '' });

    this.api.getDetail(id).subscribe({
      next: (data) => { this.selectedDetail = data; this.detailLoading = false; },
      error: () => {
        this.toast.danger('Không thể tải chi tiết');
        this.closeDetail();
      },
    });
  }

  closeDetail(): void {
    this.showDetailModal = false;
    this.selectedId = 0;
    this.selectedDetail = null;
    this.detailLoading = false;
    this.approveForm.reset();
  }

  // ─── Approve ──────────────────────────────────────────────────────────
  canApprove(): boolean {
    return this.selectedDetail?.trangThai === 'CHO_DUYET';
  }

  setDongY(val: boolean): void {
    this.approveForm.patchValue({ dongY: val });
    if (val) this.approveForm.patchValue({ lyDoTuChoi: '' });
  }

  submitApprove(): void {
    if (!this.canApprove()) return;

    const dongY = this.approveForm.get('dongY')?.value;
    if (dongY === null) { this.toast.danger('Vui lòng chọn Đồng ý hoặc Từ chối'); return; }
    if (dongY === false && !this.approveForm.get('lyDoTuChoi')?.value?.trim()) {
      this.toast.danger('Vui lòng nhập lý do từ chối'); return;
    }

    this.saving = true;
    const dto = {
      dongY,
      lyDoTuChoi: dongY === false ? this.approveForm.get('lyDoTuChoi')?.value : undefined,
    };

    this.api.duyet(this.selectedId, dto).subscribe({
      next: (res) => {
        this.toast.success(res.message);
        this.saving = false;
        this.closeDetail();
        this.loadList();
      },
      error: (err) => {
        this.toast.danger(err?.error?.message || 'Duyệt thất bại');
        this.saving = false;
      },
    });
  }

  // ─── Utils ────────────────────────────────────────────────────────────
  getFileUrl(url?: string): string {
    if (!url) return '';
    return url.startsWith('http') ? url : `${this.apiBase}${url}`;
  }

  trackById(_: number, item: DeXuatGiamDocListItemDto): number { return item.id; }
}
