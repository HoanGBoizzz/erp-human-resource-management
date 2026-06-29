import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { LuongCoBanDto, LuongCoBanUpdateDto } from 'src/app/core/models/luong.model';
import { LuongCoBanApiService } from 'src/app/core/services/api/luong-co-ban-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';

@Component({
    selector: 'app-luong-co-ban',
    templateUrl: './luong-co-ban.component.html',
    styleUrls: ['./luong-co-ban.component.scss']
})
export class LuongCoBanComponent implements OnInit {
    loading = false;
    saving = false;
    errorMsg = '';

    list: LuongCoBanDto[] = [];
    filtered: LuongCoBanDto[] = [];
    keyword = '';

    showModal = false;
    showHistoryModal = false;
    selected: LuongCoBanDto | null = null;
    history: LuongCoBanDto[] = [];

    form: FormGroup;

    constructor(
        private api: LuongCoBanApiService,
        private toast: ToastService,
        private fb: FormBuilder
    ) {
        this.form = this.fb.group({
            luongCoBan: [0, [Validators.required, Validators.min(0)]],
            phuCapCoDinh: [0, [Validators.min(0)]],
            soTaiKhoanNganHang: [''],
            tenNganHang: [''],
            chiNhanhNganHang: [''],
            ngayBatDauHieuLuc: [new Date().toISOString().slice(0, 10), Validators.required]
        });
    }

    ngOnInit(): void {
        this.loadAll();
    }

    loadAll(): void {
        this.loading = true;
        this.api.getAll().subscribe({
            next: data => {
                this.list = data;
                this.applyFilter();
                this.loading = false;
            },
            error: () => { this.loading = false; }
        });
    }

    applyFilter(): void {
        const kw = this.keyword.toLowerCase();
        this.filtered = kw
            ? this.list.filter(x =>
                x.hoTen.toLowerCase().includes(kw) ||
                x.maNhanVien?.toLowerCase().includes(kw) ||
                x.tenPhongBan?.toLowerCase()?.includes(kw))
            : [...this.list];
        // Sắp xếp theo tên A→Z (tiếng Việt)
        this.filtered.sort((a, b) => a.hoTen.localeCompare(b.hoTen, 'vi'));
    }

    openEdit(item: LuongCoBanDto): void {
        this.selected = item;
        this.form.setValue({
            luongCoBan: item.luongCoBan,
            phuCapCoDinh: item.phuCapCoDinh,
            soTaiKhoanNganHang: item.soTaiKhoanNganHang ?? '',
            tenNganHang: item.tenNganHang ?? '',
            chiNhanhNganHang: item.chiNhanhNganHang ?? '',
            ngayBatDauHieuLuc: new Date().toISOString().slice(0, 10)
        });
        this.showModal = true;
        this.errorMsg = '';
    }

    openHistory(item: LuongCoBanDto): void {
        this.api.getByNv(item.nvHoSoId).subscribe({
            next: data => {
                this.history = data;
                this.selected = item;
                this.showHistoryModal = true;
            }
        });
    }

    save(): void {
        if (this.form.invalid || !this.selected) return;
        this.saving = true;
        const payload: LuongCoBanUpdateDto = this.form.value;
        this.api.upsert(this.selected.nvHoSoId, payload).subscribe({
            next: () => {
                this.toast.success(`Đã cập nhật lương của ${this.selected!.hoTen}`);
                this.showModal = false;
                this.saving = false;
                this.loadAll();
            },
            error: (err) => {
                this.errorMsg = err?.error?.message ?? 'Lỗi khi lưu';
                this.saving = false;
            }
        });
    }

    formatCurrency(val: number): string {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
    }
}
