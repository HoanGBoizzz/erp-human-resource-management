import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import {
    NoiLamViecApiService,
    PhieuDeXuatListItem,
    PhieuTamUngListItem,
    DonDiMuonListItem
} from 'src/app/core/services/api/noi-lam-viec-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import { NotificationService } from 'src/app/core/services/notification.service';

type ActiveTab = 'de-xuat' | 'tam-ung' | 'don-di-muon';
type AnyRequest = PhieuDeXuatListItem | PhieuTamUngListItem | DonDiMuonListItem;

@Component({
    selector: 'app-yeu-cau-noi-lam-viec',
    templateUrl: './yeu-cau-noi-lam-viec.component.html',
    styleUrls: ['./yeu-cau-noi-lam-viec.component.scss']
})
export class YeuCauNoiLamViecComponent implements OnInit, OnDestroy {

    // ── Tab ──────────────────────────────────────────────────────────────────
    activeTab: ActiveTab = 'de-xuat';

    // ── Data ─────────────────────────────────────────────────────────────────
    deXuatList: PhieuDeXuatListItem[] = [];
    tamUngList: PhieuTamUngListItem[] = [];
    diMuonList: DonDiMuonListItem[] = [];
    loading = false;

    // ── Filter ───────────────────────────────────────────────────────────────
    filterDeXuat = 'ALL';
    filterTamUng = 'ALL';
    filterDiMuon = 'ALL';
    searchText = '';

    // ── Modal ─────────────────────────────────────────────────────────────────
    showModal = false;
    detailLoading = false;
    saving = false;
    selectedItem: AnyRequest | null = null;

    // SignalR subscriptions
    private entityUpdateSub?: Subscription;
    private notificationSub?: Subscription;

    // Approve form
    approveForm!: FormGroup;

    readonly statusOptions = [
        { value: 'ALL', label: 'Tất cả' },
        { value: 'CHO_DUYET', label: 'Chờ duyệt' },
        { value: 'DA_DUYET', label: 'Đã duyệt' },
        { value: 'TU_CHOI', label: 'Từ chối' }
    ];

    constructor(
        private api: NoiLamViecApiService,
        private fb: FormBuilder,
        private toast: ToastService,
        private signalrService: SignalrService,
        private notificationService: NotificationService,
    ) { }

    ngOnInit(): void {
        this.approveForm = this.fb.group({
            chapNhan: [null as boolean | null],
            lyDoTuChoi: ['']
        });
        this.loadAll();

        // Realtime: tự động tải lại khi nhân viên gửi đơn mới hoặc thạng có cập nhật
        this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
            const nlvTypes = ['PhieuDeXuat', 'PhieuTamUng', 'DonDiMuon'];
            if (nlvTypes.includes(update.entityType)) {
                console.log('[YeuCau] Realtime update:', update);
                this.loadAll();
                if (update.action === 'CREATED') {
                    this.toast.info('Có đơn yêu cầu mới cần xử lý!');
                }
            }
        });

        // Realtime: khi nhận thông báo mới (badge sidebar cập nhật ngay)
        this.notificationSub = this.signalrService.notification$.subscribe(n => {
            const nlvEntities = ['PhieuDeXuat', 'PhieuTamUng', 'DonDiMuon'];
            if (n.relatedEntity && nlvEntities.includes(n.relatedEntity)) {
                this.notificationService.refresh(); // cập nhật badge ngay lập tức
            }
        });
    }

    ngOnDestroy(): void {
        this.entityUpdateSub?.unsubscribe();
        this.notificationSub?.unsubscribe();
    }

    loadAll(): void {
        this.loading = true;
        let done = 0;
        const check = () => { if (++done === 3) this.loading = false; };

        this.api.getDeXuatList().subscribe({ next: d => { this.deXuatList = d; check(); }, error: () => check() });
        this.api.getTamUngList().subscribe({ next: d => { this.tamUngList = d; check(); }, error: () => check() });
        this.api.getDiMuonList().subscribe({ next: d => { this.diMuonList = d; check(); }, error: () => check() });
    }

    reload(): void { this.loadAll(); }

    setTab(tab: ActiveTab): void {
        this.activeTab = tab;
        this.searchText = '';
    }

    // ── KPI numbers ──────────────────────────────────────────────────────────
    get totalChoDuyet(): number {
        return this.countStatus(this.deXuatList, 'CHO_DUYET')
            + this.countStatus(this.tamUngList, 'CHO_DUYET')
            + this.countStatus(this.diMuonList, 'CHO_DUYET');
    }
    get totalDaDuyet(): number {
        return this.countStatus(this.deXuatList, 'DA_DUYET')
            + this.countStatus(this.tamUngList, 'DA_DUYET')
            + this.countStatus(this.diMuonList, 'DA_DUYET');
    }
    get totalTuChoi(): number {
        return this.countStatus(this.deXuatList, 'TU_CHOI')
            + this.countStatus(this.tamUngList, 'TU_CHOI')
            + this.countStatus(this.diMuonList, 'TU_CHOI');
    }
    get totalAll(): number {
        return this.deXuatList.length + this.tamUngList.length + this.diMuonList.length;
    }

    countStatus(list: AnyRequest[], status: string): number {
        return list.filter(x => x.trangThai === status).length;
    }

    // ── Filtered lists ────────────────────────────────────────────────────────
    get filteredDeXuat(): PhieuDeXuatListItem[] {
        return this.applyFilter(this.deXuatList, this.filterDeXuat) as PhieuDeXuatListItem[];
    }
    get filteredTamUng(): PhieuTamUngListItem[] {
        return this.applyFilter(this.tamUngList, this.filterTamUng) as PhieuTamUngListItem[];
    }
    get filteredDiMuon(): DonDiMuonListItem[] {
        return this.applyFilter(this.diMuonList, this.filterDiMuon) as DonDiMuonListItem[];
    }

    private applyFilter(list: AnyRequest[], status: string): AnyRequest[] {
        let result = status === 'ALL' ? list : list.filter(x => x.trangThai === status);
        if (this.searchText.trim()) {
            const q = this.searchText.trim().toLowerCase();
            result = result.filter(x => x.hoTenNhanVien?.toLowerCase().includes(q) || x.maNhanVien?.toLowerCase().includes(q));
        }
        return result;
    }

    // ── Modal ─────────────────────────────────────────────────────────────────
    openModal(item: AnyRequest): void {
        this.selectedItem = item;
        this.approveForm.reset({ chapNhan: null, lyDoTuChoi: '' });
        this.showModal = true;
    }

    closeModal(): void {
        this.showModal = false;
        this.selectedItem = null;
        this.approveForm.reset();
        this.saving = false;
    }

    canProcess(): boolean {
        return this.selectedItem?.trangThai === 'CHO_DUYET';
    }

    submitApprove(): void {
        if (!this.canProcess()) return;

        const chapNhan = this.approveForm.get('chapNhan')?.value;
        if (chapNhan === null || chapNhan === undefined) {
            this.toast.warning('Vui lòng chọn đồng ý hoặc từ chối');
            return;
        }
        const lyDoTuChoi = this.approveForm.get('lyDoTuChoi')?.value?.trim();
        if (chapNhan === false && !lyDoTuChoi) {
            this.toast.warning('Vui lòng nhập lý do từ chối');
            return;
        }

        this.saving = true;
        const id = this.selectedItem!.id;

        let call$;
        if (this.activeTab === 'de-xuat') {
            call$ = this.api.duyetDeXuat({ phieuId: id, chapNhan, lyDoTuChoi: chapNhan ? undefined : lyDoTuChoi });
        } else if (this.activeTab === 'tam-ung') {
            call$ = this.api.duyetTamUng({ phieuId: id, chapNhan, lyDoTuChoi: chapNhan ? undefined : lyDoTuChoi });
        } else {
            call$ = this.api.duyetDiMuon({ donId: id, chapNhan, lyDoTuChoi: chapNhan ? undefined : lyDoTuChoi });
        }

        call$.subscribe({
            next: () => {
                this.toast.success(chapNhan ? 'Đã phê duyệt thành công!' : 'Đã từ chối đơn yêu cầu');
                this.closeModal();
                this.loadAll();
            },
            error: () => {
                this.toast.danger('Có lỗi xảy ra, vui lòng thử lại');
                this.saving = false;
            }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    trangThaiLabel(s: string): string {
        return s === 'CHO_DUYET' ? 'Chờ duyệt' : s === 'DA_DUYET' ? 'Đã duyệt' : 'Từ chối';
    }

    trangThaiClass(s: string): string {
        return s === 'CHO_DUYET' ? 'badge-warning' : s === 'DA_DUYET' ? 'badge-success' : 'badge-danger';
    }

    formatCurrency(val: number): string {
        return val?.toLocaleString('vi-VN') + ' đ';
    }

    isDeXuat(item: AnyRequest): item is PhieuDeXuatListItem {
        return 'tenDungCu' in item;
    }

    isTamUng(item: AnyRequest): item is PhieuTamUngListItem {
        return 'mucDich' in item;
    }

    isDiMuon(item: AnyRequest): item is DonDiMuonListItem {
        return 'loai' in item;
    }
}
