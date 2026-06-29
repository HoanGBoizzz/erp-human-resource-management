import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import {
    ThamSoApiService,
    ThamSoHeThongDto,
} from 'src/app/core/services/api/tham-so-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';

/** Định nghĩa một thành phần trong công thức tính tổng lương */
export interface CongThucComponent {
    ma: string;
    nhan: string;    // Tên hiển thị ngắn
    moTa: string;
    loai: 'cong' | 'tru' | 'co_ban'; // cộng / trừ / cố định (không toggle)
    icon: string;
}

/** Các thành phần toggle được trong công thức lương */
export const FORMULA_COMPONENTS: CongThucComponent[] = [
    {
        ma: 'LUONG_CO_TINH_PHU_CAP',
        nhan: 'Phụ cấp',
        moTa: 'Cộng phụ cấp (ăn, đi lại, nhà ở...) vào tổng lương',
        loai: 'cong',
        icon: 'bi-gift',
    },
    {
        ma: 'LUONG_CO_TINH_OT',
        nhan: 'Lương OT',
        moTa: 'Cộng lương làm thêm giờ (tính theo hệ số OT)',
        loai: 'cong',
        icon: 'bi-clock-history',
    },
    {
        ma: 'LUONG_CO_TINH_THUONG',
        nhan: 'Thưởng & phạt',
        moTa: 'Cộng khoản thưởng hoặc phạt từ hệ thống T&P',
        loai: 'cong',
        icon: 'bi-trophy',
    },
    {
        ma: 'LUONG_CO_TINH_KHAU_TRU',
        nhan: 'Khấu trừ',
        moTa: 'Trừ các khoản khấu trừ (phạt đi muộn, T&P âm...)',
        loai: 'tru',
        icon: 'bi-dash-circle',
    },
];

/** Nhãn mô tả cho các tham số số trong bảng */
const NHAN_MAP: Record<string, { label: string; moTa: string; nhom: string }> = {
    LUONG_NGAY_CONG_CHUAN: {
        label: 'Số ngày công chuẩn / tháng',
        moTa: 'Mặc định 26 ngày. Dùng để tính lương ngày = Lương cơ bản ÷ Số ngày.',
        nhom: 'Công thức lương',
    },
    LUONG_GIO_LAM_CHUAN: {
        label: 'Số giờ làm việc chuẩn / ngày',
        moTa: 'Mặc định 8 giờ. Dùng để tính lương giờ OT.',
        nhom: 'Công thức lương',
    },
    LUONG_HE_SO_OT: {
        label: 'Hệ số nhân lương OT',
        moTa: 'Mặc định 1.5 (= 150%). Lương OT = Giờ OT × Lương giờ × Hệ số.',
        nhom: 'Công thức lương',
    },
    LUONG_PHAT_DI_MUON: {
        label: 'Phạt đi muộn (VNĐ / lần)',
        moTa: 'Mặc định 30.000 VNĐ. Trừ vào khấu trừ khi nhân viên đến muộn > gia cú.',
        nhom: 'Kỷ luật',
    },
    CHAM_CONG_GIO_VAO: {
        label: 'Giờ vào làm chuẩn',
        moTa: 'Mặc định 08:00. Nhân viên đến sau giờ này + gia cú sẽ bị tính muộn.',
        nhom: 'Chấm công',
    },
    CHAM_CONG_GIO_RA: {
        label: 'Giờ ra về chuẩn',
        moTa: 'Mặc định 17:00. Dùng để tính số giờ OT.',
        nhom: 'Chấm công',
    },
    CHAM_CONG_GIO_GIA_CU: {
        label: 'Gia cú đi muộn (phút)',
        moTa: 'Số phút dung sai. Ví dụ: 1 → vào lúc 08:01 vẫn không bị phạt.',
        nhom: 'Chấm công',
    },
};

/** Set các mã là cờ công thức — ẩn khỏi bảng tham số thô */
const FORMULA_FLAG_KEYS = new Set(FORMULA_COMPONENTS.map(c => c.ma));

@Component({
    selector: 'app-cau-hinh-luong',
    templateUrl: './cau-hinh-luong.component.html',
    styleUrls: ['./cau-hinh-luong.component.scss'],
})
export class CauHinhLuongComponent implements OnInit {
    list: ThamSoHeThongDto[] = [];
    loading = false;
    saving = false;
    togglingFlag: string | null = null; // mã đang được toggle

    showModal = false;
    editId: number | null = null;
    form: FormGroup;

    showDeleteConfirm = false;
    deleteTarget: ThamSoHeThongDto | null = null;

    readonly NHAN_MAP = NHAN_MAP;
    readonly formulaComponents = FORMULA_COMPONENTS;

    constructor(
        private api: ThamSoApiService,
        private toast: ToastService,
        private fb: FormBuilder
    ) {
        this.form = this.fb.group({
            maThamSo: ['', [Validators.required, Validators.pattern(/^[A-Z0-9_]+$/)]],
            giaTri: ['', Validators.required],
            moTa: [''],
            ngayBatDauHieuLuc: [this.todayStr(), Validators.required],
            ngayKetThucHieuLuc: [''],
        });
    }

    ngOnInit(): void {
        this.loadAll();
    }

    loadAll(): void {
        this.loading = true;
        this.api.getAll().subscribe({
            next: (data) => { this.list = data; this.loading = false; },
            error: () => { this.toast.danger('Không thể tải danh sách tham số.'); this.loading = false; },
        });
    }

    // ── Formula flags ────────────────────────────────────────────────────────

    /** Trả về true nếu thành phần đang được bật (default: bật nếu chưa có DB) */
    getFlag(ma: string): boolean {
        const item = this.list.find(x => x.maThamSo === ma);
        if (!item) return true; // default: enabled
        return item.giaTri !== '0';
    }

    /** Bật/tắt một thành phần công thức, tự động create hoặc update DB */
    toggleFlag(ma: string): void {
        if (this.togglingFlag) return; // prevent double-click
        const current = this.getFlag(ma);
        const newVal = current ? '0' : '1';
        const existing = this.list.find(x => x.maThamSo === ma);

        this.togglingFlag = ma;
        let req$: Observable<any>;

        if (existing) {
            req$ = this.api.update(existing.id, {
                giaTri: newVal,
                moTa: existing.moTa,
                ngayBatDauHieuLuc: existing.ngayBatDauHieuLuc,
            });
        } else {
            req$ = this.api.create({
                maThamSo: ma,
                giaTri: newVal,
                ngayBatDauHieuLuc: this.todayStr(),
            });
        }

        req$.subscribe({
            next: () => {
                this.togglingFlag = null;
                const comp = FORMULA_COMPONENTS.find(c => c.ma === ma);
                this.toast.success(`${comp?.nhan ?? ma}: ${newVal === '1' ? 'Đã bật ✓' : 'Đã tắt ✗'}`);
                this.loadAll();
            },
            error: (err: any) => {
                this.togglingFlag = null;
                this.toast.danger(err?.error?.message ?? 'Không thể lưu cấu hình.');
            },
        });
    }

    // ── Param table ─────────────────────────────────────────────────────────

    getLabel(maThamSo: string): string {
        return NHAN_MAP[maThamSo]?.label ?? maThamSo;
    }

    getMoTa(maThamSo: string): string {
        return NHAN_MAP[maThamSo]?.moTa ?? '';
    }

    getNhom(maThamSo: string): string {
        return NHAN_MAP[maThamSo]?.nhom ?? 'Khác';
    }

    /** Danh sách nhóm, loại bỏ các mã là cờ công thức */
    get nhomList(): string[] {
        const set = new Set(
            this.list
                .filter(x => !FORMULA_FLAG_KEYS.has(x.maThamSo))
                .map(x => this.getNhom(x.maThamSo))
        );
        return Array.from(set);
    }

    getByNhom(nhom: string): ThamSoHeThongDto[] {
        return this.list.filter(x => !FORMULA_FLAG_KEYS.has(x.maThamSo) && this.getNhom(x.maThamSo) === nhom);
    }

    openCreate(): void {
        this.editId = null;
        this.form.reset({
            maThamSo: '',
            giaTri: '',
            moTa: '',
            ngayBatDauHieuLuc: this.todayStr(),
            ngayKetThucHieuLuc: '',
        });
        this.form.get('maThamSo')!.enable();
        this.showModal = true;
    }

    openEdit(item: ThamSoHeThongDto): void {
        this.editId = item.id;
        this.form.patchValue({
            maThamSo: item.maThamSo,
            giaTri: item.giaTri,
            moTa: item.moTa ?? '',
            ngayBatDauHieuLuc: item.ngayBatDauHieuLuc?.substring(0, 10) ?? this.todayStr(),
            ngayKetThucHieuLuc: item.ngayKetThucHieuLuc?.substring(0, 10) ?? '',
        });
        this.form.get('maThamSo')!.disable();
        this.showModal = true;
    }

    closeModal(): void {
        this.showModal = false;
        this.editId = null;
    }

    save(): void {
        if (this.form.invalid) { this.form.markAllAsTouched(); return; }
        const val = this.form.getRawValue();
        const payload = {
            maThamSo: val.maThamSo?.toUpperCase().trim(),
            giaTri: val.giaTri?.trim(),
            moTa: val.moTa?.trim() || undefined,
            ngayBatDauHieuLuc: val.ngayBatDauHieuLuc,
            ngayKetThucHieuLuc: val.ngayKetThucHieuLuc || undefined,
        };
        this.saving = true;
        const req$: Observable<any> =
            this.editId != null ? this.api.update(this.editId, payload) : this.api.create(payload as any);

        req$.subscribe({
            next: () => {
                this.toast.success(this.editId ? 'Cập nhật thành công.' : 'Thêm tham số thành công.');
                this.closeModal();
                this.loadAll();
                this.saving = false;
            },
            error: (err: any) => {
                this.toast.danger(err?.error?.message ?? 'Có lỗi xảy ra.');
                this.saving = false;
            },
        });
    }

    confirmDelete(item: ThamSoHeThongDto): void {
        this.deleteTarget = item;
        this.showDeleteConfirm = true;
    }

    cancelDelete(): void {
        this.showDeleteConfirm = false;
        this.deleteTarget = null;
    }

    doDelete(): void {
        if (!this.deleteTarget) return;
        this.api.delete(this.deleteTarget.id).subscribe({
            next: () => {
                this.toast.success('Đã xóa tham số.');
                this.showDeleteConfirm = false;
                this.deleteTarget = null;
                this.loadAll();
            },
            error: (err: any) => {
                this.toast.danger(err?.error?.message ?? 'Không thể xóa.');
            },
        });
    }

    /** True nếu có bất kỳ thành phần nào đang bị tắt */
    get hasDisabledComponents(): boolean {
        return this.formulaComponents.some(c => !this.getFlag(c.ma));
    }

    private todayStr(): string {
        return new Date().toISOString().substring(0, 10);
    }
}
