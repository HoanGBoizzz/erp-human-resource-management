// src/app/features/hr/pages/de-xuat-giam-doc/de-xuat-giam-doc.component.ts

import { Component, ElementRef, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
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

type Mode = 'LIST' | 'CREATE' | 'EDIT';

@Component({
    selector: 'app-de-xuat-giam-doc',
    templateUrl: './de-xuat-giam-doc.component.html',
    styleUrls: ['./de-xuat-giam-doc.component.scss'],
})
export class DeXuatGiamDocComponent implements OnInit, OnDestroy {

    // ─── State ──────────────────────────────────────────────────────────
    loading = false;
    saving = false;
    uploading = false;
    mode: Mode = 'LIST';

    // ─── Data ────────────────────────────────────────────────────────────
    list: DeXuatGiamDocListItemDto[] = [];
    filtered: DeXuatGiamDocListItemDto[] = [];
    q = '';
    statusFilter: DeXuatTrangThai | 'ALL' = 'ALL';

    // ─── KPI ─────────────────────────────────────────────────────────────
    kpiTotal = 0;
    kpiNhap = 0;
    kpiChoDuyet = 0;
    kpiDaDuyet = 0;
    kpiTuChoi = 0;

    // ─── Form ─────────────────────────────────────────────────────────────
    form: FormGroup;
    editingId = 0;

    // ─── Detail modal ────────────────────────────────────────────────────
    showDetailModal = false;
    detailLoading = false;
    selectedDetail: DeXuatGiamDocDetailDto | null = null;
    selectedId = 0;

    // ─── File upload ─────────────────────────────────────────────────────
    @ViewChild('fileInput') fileInputRef?: ElementRef<HTMLInputElement>;
    pendingFile: File | null = null;
    pendingFileId = 0;

    readonly apiBase = environment.apiBaseUrl;
    readonly LABEL = DEXUAT_TRANGTHAI_LABEL;
    readonly BADGE = DEXUAT_TRANGTHAI_BADGE;

    private entityUpdateSub?: Subscription;
    private notificationSub?: Subscription;

    // ─── Status filter options ──────────────────────────────────────────
    statusOptions: { value: DeXuatTrangThai | 'ALL'; label: string }[] = [
        { value: 'ALL', label: 'Tất cả' },
        { value: 'NHAP', label: 'Nháp' },
        { value: 'CHO_DUYET', label: 'Chờ duyệt' },
        { value: 'DA_DUYET', label: 'Đã duyệt' },
        { value: 'TU_CHOI', label: 'Từ chối' },
        { value: 'DA_THU_HOI', label: 'Đã thu hồi' },
    ];

    constructor(
        private api: DeXuatGiamDocApiService,
        private fb: FormBuilder,
        private toast: ToastService,
        private signalr: SignalrService,
    ) {
        this.form = this.fb.group({
            tenDeXuat: ['', [Validators.required, Validators.maxLength(300)]],
            moTa: [''],
            ngayDeXuat: ['', Validators.required],
        });
    }

    ngOnInit(): void {
        this.loadList();

        // Realtime: tự refresh khi giám đốc duyệt/từ chối
        this.entityUpdateSub = this.signalr.entityUpdate$.subscribe(update => {
            if (update.entityType === 'DE_XUAT_GIAM_DOC') {
                this.loadList();
            }
        });

        // Realtime: thông báo duyệt/từ chối từ giám đốc
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
        this.kpiTotal = this.list.length;
        this.kpiNhap = this.list.filter(x => x.trangThai === 'NHAP').length;
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
                (x.moTa ?? '').toLowerCase().includes(kw)
            );
        }
        this.filtered = data;
    }

    // ─── Form helpers ─────────────────────────────────────────────────────
    openCreate(): void {
        this.mode = 'CREATE';
        this.editingId = 0;
        this.pendingFile = null;
        this.form.reset({ ngayDeXuat: new Date().toISOString().substring(0, 10) });
    }

    openEdit(item: DeXuatGiamDocListItemDto): void {
        if (!this.canEdit(item)) {
            this.toast.danger('Phải thu hồi đề xuất trước khi chỉnh sửa');
            return;
        }
        this.mode = 'EDIT';
        this.editingId = item.id;
        this.pendingFile = null;
        this.form.patchValue({
            tenDeXuat: item.tenDeXuat,
            moTa: item.moTa ?? '',
            ngayDeXuat: item.ngayDeXuat?.substring(0, 10),
        });
    }

    cancelForm(): void {
        this.mode = 'LIST';
        this.editingId = 0;
        this.pendingFile = null;
        this.form.reset();
    }

    // ─── Can actions ──────────────────────────────────────────────────────
    canEdit(item: DeXuatGiamDocListItemDto): boolean {
        return item.trangThai === 'NHAP' || item.trangThai === 'DA_THU_HOI';
    }

    canDelete(item: DeXuatGiamDocListItemDto): boolean {
        return item.trangThai === 'NHAP' || item.trangThai === 'DA_THU_HOI';
    }

    canGuiDuyet(item: DeXuatGiamDocListItemDto): boolean {
        return item.trangThai === 'NHAP' || item.trangThai === 'DA_THU_HOI';
    }

    canThuHoi(item: DeXuatGiamDocListItemDto): boolean {
        return item.trangThai === 'CHO_DUYET';
    }

    // ─── CRUD ─────────────────────────────────────────────────────────────
    save(): void {
        if (this.form.invalid) { this.form.markAllAsTouched(); return; }
        this.saving = true;

        const dto = { ...this.form.value };
        // Ensure date is in proper format
        if (dto.ngayDeXuat) dto.ngayDeXuat = dto.ngayDeXuat.substring(0, 10);

        if (this.mode === 'CREATE') {
            this.api.create(dto).subscribe({
                next: (res) => {
                    this.toast.success('Tạo đề xuất thành công');
                    this.saving = false;
                    // Upload file nếu có
                    if (this.pendingFile) {
                        this.doUpload(res.id, this.pendingFile, () => {
                            this.cancelForm();
                            this.loadList();
                        });
                    } else {
                        this.cancelForm();
                        this.loadList();
                    }
                },
                error: (err) => {
                    this.toast.danger(err?.error?.message || 'Tạo đề xuất thất bại');
                    this.saving = false;
                },
            });
        } else {
            this.api.update(this.editingId, dto).subscribe({
                next: () => {
                    this.toast.success('Cập nhật thành công');
                    this.saving = false;
                    if (this.pendingFile) {
                        this.doUpload(this.editingId, this.pendingFile, () => {
                            this.cancelForm();
                            this.loadList();
                        });
                    } else {
                        this.cancelForm();
                        this.loadList();
                    }
                },
                error: (err) => {
                    this.toast.danger(err?.error?.message || 'Cập nhật thất bại');
                    this.saving = false;
                },
            });
        }
    }

    deleteItem(item: DeXuatGiamDocListItemDto): void {
        if (!this.canDelete(item)) {
            this.toast.danger('Phải thu hồi đề xuất trước khi xóa');
            return;
        }
        if (!confirm(`Xóa đề xuất "${item.tenDeXuat}"?`)) return;

        this.api.delete(item.id).subscribe({
            next: () => {
                this.toast.success('Đã xóa đề xuất');
                this.loadList();
            },
            error: (err) => this.toast.danger(err?.error?.message || 'Xóa thất bại'),
        });
    }

    // ─── Workflow ─────────────────────────────────────────────────────────
    guiDuyet(item: DeXuatGiamDocListItemDto): void {
        if (!confirm(`Gửi đề xuất "${item.tenDeXuat}" để giám đốc duyệt?`)) return;
        this.api.guiDuyet(item.id).subscribe({
            next: (res) => {
                this.toast.success(res.message);
                this.loadList();
            },
            error: (err) => this.toast.danger(err?.error?.message || 'Gửi duyệt thất bại'),
        });
    }

    thuHoi(item: DeXuatGiamDocListItemDto): void {
        if (!confirm(`Thu hồi đề xuất "${item.tenDeXuat}"? Đề xuất sẽ về trạng thái Nháp.`)) return;
        this.api.thuHoi(item.id).subscribe({
            next: (res) => {
                this.toast.success(res.message);
                this.loadList();
            },
            error: (err) => this.toast.danger(err?.error?.message || 'Thu hồi thất bại'),
        });
    }

    // ─── File ─────────────────────────────────────────────────────────────
    onFileSelected(event: Event): void {
        const file = (event.target as HTMLInputElement).files?.[0];
        if (file) this.pendingFile = file;
    }

    clearFile(): void {
        this.pendingFile = null;
        if (this.fileInputRef) this.fileInputRef.nativeElement.value = '';
    }

    private doUpload(id: number, file: File, callback: () => void): void {
        this.uploading = true;
        this.api.uploadFile(id, file).subscribe({
            next: () => { this.uploading = false; callback(); },
            error: () => { this.uploading = false; this.toast.danger('Upload file thất bại'); callback(); },
        });
    }

    formatFileSize(bytes?: number): string {
        if (!bytes) return '';
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
    }

    getFileUrl(url?: string): string {
        if (!url) return '';
        return url.startsWith('http') ? url : `${this.apiBase}${url}`;
    }

    // ─── Detail modal ─────────────────────────────────────────────────────
    openDetail(id: number): void {
        this.selectedId = id;
        this.showDetailModal = true;
        this.detailLoading = true;
        this.selectedDetail = null;

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
    }

    // ─── Utils ────────────────────────────────────────────────────────────
    trackById(_: number, item: DeXuatGiamDocListItemDto): number { return item.id; }
}
