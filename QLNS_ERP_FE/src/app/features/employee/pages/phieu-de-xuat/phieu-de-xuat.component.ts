import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { NoiLamViecApiService, PhieuDeXuatListItem } from 'src/app/core/services/api/noi-lam-viec-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';

@Component({
    selector: 'app-phieu-de-xuat',
    templateUrl: './phieu-de-xuat.component.html',
    styleUrls: ['./phieu-de-xuat.component.scss']
})
export class PhieuDeXuatComponent implements OnInit, OnDestroy {
    list: PhieuDeXuatListItem[] = [];
    showModal = false;
    form!: FormGroup;
    submitting = false;
    loading = false;
    editingId: number | null = null;

    // SignalR subscription
    private entityUpdateSub?: Subscription;

    constructor(
        private api: NoiLamViecApiService,
        private fb: FormBuilder,
        private toast: ToastService,
        private signalrService: SignalrService
    ) { }

    ngOnInit(): void {
        this.loadList();
        this.initForm();

        // Realtime: tự động reload khi HR duyệt hoặc từ chối
        this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
            if (update.entityType === 'PhieuDeXuat') {
                console.log('[PhieuDeXuat] Realtime update:', update);
                this.loadList();
                if (update.action === 'APPROVED') {
                    this.toast.success('Phiếu đề xuất của bạn đã được phê duyệt!');
                } else if (update.action === 'REJECTED') {
                    this.toast.warning('Phiếu đề xuất của bạn bị từ chối.');
                }
            }
        });
    }

    ngOnDestroy(): void {
        this.entityUpdateSub?.unsubscribe();
    }

    loadList(): void {
        this.loading = true;
        this.api.getDeXuatList().subscribe({
            next: data => { this.list = data; this.loading = false; },
            error: () => { this.toast.danger('Lỗi tải danh sách phiếu'); this.loading = false; }
        });
    }

    initForm(): void {
        this.form = this.fb.group({
            tenDungCu: ['', [Validators.required, Validators.maxLength(200)]],
            donViTinh: ['cái', [Validators.required]],
            soLuong: [1, [Validators.required, Validators.min(1)]],
            giaTien: [0, [Validators.required, Validators.min(1000)]],
            lyDo: ['', [Validators.required, Validators.maxLength(500)]]
        });
    }

    get tongTienPreview(): number {
        const { soLuong, giaTien } = this.form.value;
        return (soLuong || 0) * (giaTien || 0);
    }

    get cntChoDuyet(): number { return this.list.filter(x => x.trangThai === 'CHO_DUYET').length; }
    get cntDaDuyet(): number { return this.list.filter(x => x.trangThai === 'DA_DUYET').length; }
    get cntTuChoi(): number { return this.list.filter(x => x.trangThai === 'TU_CHOI').length; }
    get isEditing(): boolean { return this.editingId !== null; }

    openModal(): void {
        this.editingId = null;
        this.form.reset({ tenDungCu: '', donViTinh: 'cái', soLuong: 1, giaTien: 0, lyDo: '' });
        this.showModal = true;
    }

    openEditModal(item: PhieuDeXuatListItem): void {
        this.editingId = item.id;
        this.form.patchValue({
            tenDungCu: item.tenDungCu,
            donViTinh: item.donViTinh,
            soLuong: item.soLuong,
            giaTien: item.giaTien,
            lyDo: item.lyDo
        });
        this.showModal = true;
    }

    closeModal(): void {
        this.showModal = false;
        this.editingId = null;
    }

    submit(): void {
        if (this.form.invalid) {
            this.form.markAllAsTouched();
            return;
        }
        this.submitting = true;
        const v = this.form.value;
        const payload = {
            tenDungCu: v.tenDungCu.trim(),
            donViTinh: v.donViTinh,
            soLuong: v.soLuong,
            giaTien: v.giaTien,
            lyDo: v.lyDo.trim()
        };

        const call: any = this.isEditing
            ? this.api.updateDeXuat(this.editingId!, payload)
            : this.api.createDeXuat(payload);

        call.subscribe({
            next: () => {
                this.toast.success(this.isEditing ? 'Cập nhật phiếu thành công!' : 'Nộp phiếu đề xuất thành công!');
                this.loadList();
                this.closeModal();
                this.submitting = false;
            },
            error: () => {
                this.toast.danger('Lỗi xử lý phiếu, vui lòng thử lại');
                this.submitting = false;
            }
        });
    }

    delete(id: number): void {
        if (!confirm('Bạn có chắc muốn xóa phiếu đề xuất này?')) return;
        this.api.deleteDeXuat(id).subscribe({
            next: () => { this.toast.success('Đã xóa phiếu đề xuất'); this.loadList(); },
            error: () => this.toast.danger('Không thể xóa phiếu này')
        });
    }

    formatCurrency(val: number): string {
        return val.toLocaleString('vi-VN') + ' đ';
    }

    loaiLabel(loai: string): string {
        switch (loai) {
            case 'CHO_DUYET': return 'Chờ duyệt';
            case 'DA_DUYET': return 'Đã duyệt';
            case 'TU_CHOI': return 'Từ chối';
            default: return loai;
        }
    }

    loaiClass(loai: string): string {
        return loai === 'CHO_DUYET' ? 'badge-warning' : loai === 'DA_DUYET' ? 'badge-success' : 'badge-danger';
    }
}
