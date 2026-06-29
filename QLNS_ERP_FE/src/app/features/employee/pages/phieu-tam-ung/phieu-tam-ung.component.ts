import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { NoiLamViecApiService, PhieuTamUngListItem } from 'src/app/core/services/api/noi-lam-viec-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';

@Component({
    selector: 'app-phieu-tam-ung',
    templateUrl: './phieu-tam-ung.component.html',
    styleUrls: ['./phieu-tam-ung.component.scss']
})
export class PhieuTamUngComponent implements OnInit, OnDestroy {
    list: PhieuTamUngListItem[] = [];
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
        this.form = this.fb.group({
            mucDich: ['', [Validators.required, Validators.maxLength(200)]],
            soTien: [null, [Validators.required, Validators.min(10000)]],
            ngayCanTamUng: ['', Validators.required],
            lyDo: ['', [Validators.required, Validators.maxLength(500)]]
        });

        // Realtime: tự động reload khi HR duyệt hoặc từ chối
        this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
            if (update.entityType === 'PhieuTamUng') {
                console.log('[PhieuTamUng] Realtime update:', update);
                this.loadList();
                if (update.action === 'APPROVED') {
                    this.toast.success('Phiếu tạm ứng của bạn đã được phê duyệt!');
                } else if (update.action === 'REJECTED') {
                    this.toast.warning('Phiếu tạm ứng của bạn bị từ chối.');
                }
            }
        });
    }

    ngOnDestroy(): void {
        this.entityUpdateSub?.unsubscribe();
    }

    loadList(): void {
        this.loading = true;
        this.api.getTamUngList().subscribe({
            next: data => { this.list = data; this.loading = false; },
            error: () => { this.toast.danger('Lỗi tải danh sách phiếu'); this.loading = false; }
        });
    }

    get cntChoDuyet(): number { return this.list.filter(x => x.trangThai === 'CHO_DUYET').length; }
    get cntDaDuyet(): number { return this.list.filter(x => x.trangThai === 'DA_DUYET').length; }
    get cntTuChoi(): number { return this.list.filter(x => x.trangThai === 'TU_CHOI').length; }
    get tongSoTien(): number { return this.list.reduce((s, x) => s + x.soTien, 0); }
    get soTienPreview(): number { return this.form.value.soTien || 0; }
    get isEditing(): boolean { return this.editingId !== null; }

    openModal(): void {
        this.editingId = null;
        const today = new Date().toISOString().split('T')[0];
        this.form.reset({ mucDich: '', soTien: null, ngayCanTamUng: today, lyDo: '' });
        this.showModal = true;
    }

    openEditModal(item: PhieuTamUngListItem): void {
        this.editingId = item.id;
        const ngay = item.ngayCanTamUng ? new Date(item.ngayCanTamUng).toISOString().split('T')[0] : '';
        this.form.patchValue({
            mucDich: item.mucDich,
            soTien: item.soTien,
            ngayCanTamUng: ngay,
            lyDo: item.lyDo
        });
        this.showModal = true;
    }

    closeModal(): void { this.showModal = false; this.editingId = null; }

    submit(): void {
        if (this.form.invalid) { this.form.markAllAsTouched(); return; }
        this.submitting = true;
        const v = this.form.value;
        const payload = {
            mucDich: v.mucDich.trim(),
            soTien: v.soTien,
            ngayCanTamUng: v.ngayCanTamUng,
            lyDo: v.lyDo.trim()
        };

        const call: any = this.isEditing
            ? this.api.updateTamUng(this.editingId!, payload)
            : this.api.createTamUng(payload);

        call.subscribe({
            next: () => {
                this.toast.success(this.isEditing ? 'Cập nhật phiếu thành công!' : 'Nộp phiếu tạm ứng thành công!');
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
        if (!confirm('Xóa phiếu tạm ứng này?')) return;
        this.api.deleteTamUng(id).subscribe({
            next: () => { this.toast.success('Đã xóa phiếu tạm ứng'); this.loadList(); },
            error: () => this.toast.danger('Không thể xóa phiếu này')
        });
    }

    formatCurrency(val: number): string {
        return val.toLocaleString('vi-VN') + ' đ';
    }

    loaiLabel(s: string): string {
        return s === 'CHO_DUYET' ? 'Chờ duyệt' : s === 'DA_DUYET' ? 'Đã duyệt' : 'Từ chối';
    }

    loaiClass(s: string): string {
        return s === 'CHO_DUYET' ? 'badge-warning' : s === 'DA_DUYET' ? 'badge-success' : 'badge-danger';
    }
}
