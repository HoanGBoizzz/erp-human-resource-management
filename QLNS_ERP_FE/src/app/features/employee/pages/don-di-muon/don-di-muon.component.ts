import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { NoiLamViecApiService, DonDiMuonListItem } from 'src/app/core/services/api/noi-lam-viec-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';

@Component({
    selector: 'app-don-di-muon',
    templateUrl: './don-di-muon.component.html',
    styleUrls: ['./don-di-muon.component.scss']
})
export class DonDiMuonComponent implements OnInit, OnDestroy {
    list: DonDiMuonListItem[] = [];
    showModal = false;
    form!: FormGroup;
    timeError = '';
    loading = false;
    editingId: number | null = null;

    // SignalR subscription
    private entityUpdateSub?: Subscription;

    readonly loaiOptions = [
        { value: 'DI_MUON', label: 'Đi muộn', icon: 'bi-clock', color: 'warning' },
        { value: 'VE_SOM', label: 'Về sớm', icon: 'bi-door-open', color: 'info' },
        { value: 'CA_HAI', label: 'Cả hai (đi muộn + về sớm)', icon: 'bi-arrows-expand', color: 'danger' }
    ];

    constructor(
        private api: NoiLamViecApiService,
        private fb: FormBuilder,
        private toast: ToastService,
        private signalrService: SignalrService
    ) { }

    ngOnInit(): void {
        this.loadList();
        const today = new Date().toISOString().split('T')[0];
        this.form = this.fb.group({
            loai: ['DI_MUON', Validators.required],
            ngayApDung: [today, Validators.required],
            thoiGianBatDau: ['08:30', Validators.required],
            thoiGianKetThuc: ['09:00', Validators.required],
            lyDo: ['', [Validators.required, Validators.maxLength(500)]]
        });

        // Realtime: tự động reload khi HR duyệt hoặc từ chối
        this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
            if (update.entityType === 'DonDiMuon') {
                console.log('[DonDiMuon] Realtime update:', update);
                this.loadList();
                if (update.action === 'APPROVED') {
                    this.toast.success('Đơn xin phép của bạn đã được phê duyệt!');
                } else if (update.action === 'REJECTED') {
                    this.toast.warning('Đơn xin phép của bạn bị từ chối.');
                }
            }
        });
    }

    ngOnDestroy(): void {
        this.entityUpdateSub?.unsubscribe();
    }

    loadList(): void {
        this.loading = true;
        this.api.getDiMuonList().subscribe({
            next: data => { this.list = data; this.loading = false; },
            error: () => { this.toast.danger('Lỗi tải danh sách đơn'); this.loading = false; }
        });
    }

    get cntChoDuyet(): number { return this.list.filter(x => x.trangThai === 'CHO_DUYET').length; }
    get cntDaDuyet(): number { return this.list.filter(x => x.trangThai === 'DA_DUYET').length; }
    get cntTuChoi(): number { return this.list.filter(x => x.trangThai === 'TU_CHOI').length; }
    get isEditing(): boolean { return this.editingId !== null; }

    openModal(): void {
        this.editingId = null;
        const today = new Date().toISOString().split('T')[0];
        this.form.reset({ loai: 'DI_MUON', ngayApDung: today, thoiGianBatDau: '08:30', thoiGianKetThuc: '09:00', lyDo: '' });
        this.timeError = '';
        this.showModal = true;
    }

    openEditModal(item: DonDiMuonListItem): void {
        this.editingId = item.id;
        const ngay = item.ngayApDung ? new Date(item.ngayApDung).toISOString().split('T')[0] : '';
        this.form.patchValue({
            loai: item.loai,
            ngayApDung: ngay,
            thoiGianBatDau: item.thoiGianBatDau,
            thoiGianKetThuc: item.thoiGianKetThuc,
            lyDo: item.lyDo
        });
        this.timeError = '';
        this.showModal = true;
    }

    closeModal(): void { this.showModal = false; this.editingId = null; }

    submit(): void {
        if (this.form.invalid) { this.form.markAllAsTouched(); return; }
        const v = this.form.value;
        if (v.thoiGianBatDau >= v.thoiGianKetThuc) {
            this.timeError = 'Giờ kết thúc phải sau giờ bắt đầu';
            return;
        }
        this.timeError = '';
        const payload = {
            loai: v.loai,
            ngayApDung: v.ngayApDung,
            thoiGianBatDau: v.thoiGianBatDau,
            thoiGianKetThuc: v.thoiGianKetThuc,
            lyDo: v.lyDo.trim()
        };

        const call: any = this.editingId !== null
            ? this.api.updateDiMuon(this.editingId, payload)
            : this.api.createDiMuon(payload);

        call.subscribe({
            next: () => {
                this.toast.success(this.editingId !== null ? 'Cập nhật đơn thành công!' : 'Nộp đơn thành công!');
                this.loadList();
                this.closeModal();
            },
            error: () => this.toast.danger('Lỗi nộp đơn, vui lòng thử lại')
        });
    }

    delete(id: number): void {
        if (!confirm('Xóa đơn này?')) return;
        this.api.deleteDiMuon(id).subscribe({
            next: () => { this.toast.success('Đã xóa đơn'); this.loadList(); },
            error: () => this.toast.danger('Không thể xóa đơn này')
        });
    }

    loaiLabel(loai: string): string {
        return this.loaiOptions.find(x => x.value === loai)?.label || loai;
    }

    loaiIconClass(loai: string): string {
        const opt = this.loaiOptions.find(x => x.value === loai);
        return opt ? `bi ${opt.icon} text-${opt.color === 'warning' ? 'warning' : opt.color === 'info' ? 'info' : 'danger'}` : '';
    }

    trangThaiLabel(s: string): string {
        return s === 'CHO_DUYET' ? 'Chờ duyệt' : s === 'DA_DUYET' ? 'Đã duyệt' : 'Từ chối';
    }

    trangThaiClass(s: string): string {
        return s === 'CHO_DUYET' ? 'badge-warning' : s === 'DA_DUYET' ? 'badge-success' : 'badge-danger';
    }

    thoiGianRange(item: DonDiMuonListItem): string {
        return `${item.thoiGianBatDau} – ${item.thoiGianKetThuc}`;
    }
}
