import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PhuCapApiService } from '../../../../core/services/api/phu-cap-api.service';
import { LuongApiService } from '../../../../core/services/api/luong-api.service';
import { NhanVienApiService } from '../../../../core/services/api/nhan-vien-api.service';
import {
    PhuCapLoaiDto, PhuCapLoaiCreateDto, NvPhuCapDto, NvPhuCapCreateDto,
    BangLuongThangListItemDto, BangLuongItemDto, BangLuongItemCreateDto
} from '../../../../core/models/luong.model';
import { NhanVienListItemDto } from '../../../../core/models/nhan-vien.model';
import { ToastService } from 'src/app/shared/services/toast.service';

@Component({
    selector: 'app-phu-cap',
    templateUrl: './phu-cap.component.html',
    styleUrls: ['./phu-cap.component.scss']
})
export class PhuCapComponent implements OnInit {
    // Tabs
    activeTab: 'loai' | 'nhan-vien' | 'thuong-phat' = 'loai';

    // Loại phụ cấp
    loaiList: PhuCapLoaiDto[] = [];
    loaiFiltered: PhuCapLoaiDto[] = [];
    loaiKeyword = '';
    showLoaiModal = false;
    selectedLoai: PhuCapLoaiDto | null = null;
    loaiForm!: FormGroup;

    // Phụ cấp nhân viên
    nvPhuCapList: NvPhuCapDto[] = [];
    nvPhuCapFiltered: NvPhuCapDto[] = [];
    nvKeyword = '';
    showNvModal = false;
    selectedNvPhuCap: NvPhuCapDto | null = null;
    nvForm!: FormGroup;

    // Employee search dropdown in NV modal
    nhanVienList: NhanVienListItemDto[] = [];
    nvSearchTerm = '';
    nvDropdownFiltered: NhanVienListItemDto[] = [];
    showNvDropdown = false;

    // Currency display strings
    soTienDisplayNv = '';
    soTienDisplayItem = '';

    loading = false;
    saving = false;
    errorMsg = '';

    // ─── Thưởng & Phạt (BangLuongItem) ──────────────────────────────────────
    bangLuongList: BangLuongThangListItemDto[] = [];
    bangLuongFiltered: BangLuongThangListItemDto[] = [];
    blKeyword = '';
    blThangFilter: number | '' = '';
    blNamFilter: number | '' = new Date().getFullYear();
    selectedBangLuong: BangLuongThangListItemDto | null = null;
    blItems: BangLuongItemDto[] = [];
    loadingItems = false;
    showItemModal = false;
    itemForm!: FormGroup;

    constructor(
        private fb: FormBuilder,
        private phuCapApi: PhuCapApiService,
        private luongApi: LuongApiService,
        private nhanVienApi: NhanVienApiService,
        private toast: ToastService
    ) { }

    ngOnInit(): void {
        this.buildForms();
        this.loadLoai();
        this.loadNvPhuCap();
        this.loadBangLuong();
        this.loadNhanVien();
    }

    buildForms(): void {
        this.loaiForm = this.fb.group({
            tenPhuCap: ['', Validators.required],
            moTa: [''],
            laCoDinh: [true],
            donVi: ['VND'],
            thuTu: [0]
        });

        this.nvForm = this.fb.group({
            nvHoSoId: [null, Validators.required],
            phuCapLoaiId: [null, Validators.required],
            soTien: [0, [Validators.required, Validators.min(0)]],
            ngayBatDau: ['', Validators.required],
            ngayKetThuc: [''],
            ghiChu: ['']
        });

        this.itemForm = this.fb.group({
            loai: ['THUONG', Validators.required],
            lyDo: ['', Validators.required],
            soTien: [0, [Validators.required, Validators.min(1)]]
        });
    }

    // ─── Loại phụ cấp ─────────────────────────────────────────────────────────
    loadLoai(): void {
        this.phuCapApi.getAllLoai().subscribe({
            next: data => {
                this.loaiList = data;
                this.applyLoaiFilter();
            }
        });
    }

    applyLoaiFilter(): void {
        const kw = this.loaiKeyword.toLowerCase();
        this.loaiFiltered = kw
            ? this.loaiList.filter(x =>
                x.tenPhuCap.toLowerCase().includes(kw) ||
                (x.moTa ?? '').toLowerCase().includes(kw))
            : [...this.loaiList];
    }

    openAddLoai(): void {
        this.selectedLoai = null;
        this.loaiForm.reset({ laCoDinh: true, donVi: 'VND', thuTu: 0 });
        this.errorMsg = '';
        this.showLoaiModal = true;
    }

    openEditLoai(item: PhuCapLoaiDto): void {
        this.selectedLoai = item;
        this.loaiForm.patchValue({
            tenPhuCap: item.tenPhuCap,
            moTa: item.moTa,
            laCoDinh: item.laCoDinh,
            donVi: item.donVi,
            thuTu: item.thuTu
        });
        this.errorMsg = '';
        this.showLoaiModal = true;
    }

    saveLoai(): void {
        if (this.loaiForm.invalid) return;
        this.saving = true;
        this.errorMsg = '';
        const payload: PhuCapLoaiCreateDto = this.loaiForm.value;

        const call = this.selectedLoai
            ? this.phuCapApi.updateLoai(this.selectedLoai.id, payload)
            : this.phuCapApi.createLoai(payload);

        call.subscribe({
            next: () => {
                this.saving = false;
                this.showLoaiModal = false;
                this.toast.show(this.selectedLoai ? 'Cập nhật thành công' : 'Đã thêm loại phụ cấp', 'success');
                this.loadLoai();
            },
            error: err => {
                this.saving = false;
                this.errorMsg = err?.error?.message ?? 'Có lỗi xảy ra';
            }
        });
    }

    toggleLoai(item: PhuCapLoaiDto): void {
        this.phuCapApi.toggleLoai(item.id).subscribe({
            next: () => {
                item.dangHoatDong = !item.dangHoatDong;
                this.toast.show(item.dangHoatDong ? 'Đã kích hoạt' : 'Đã tắt loại phụ cấp', 'success');
            }
        });
    }

    // ─── Phụ cấp nhân viên ────────────────────────────────────────────────────
    loadNvPhuCap(): void {
        this.phuCapApi.getAll().subscribe({
            next: data => {
                this.nvPhuCapList = data;
                this.applyNvFilter();
            }
        });
    }

    applyNvFilter(): void {
        const kw = this.nvKeyword.toLowerCase();
        this.nvPhuCapFiltered = kw
            ? this.nvPhuCapList.filter(x =>
                (x.hoTen ?? '').toLowerCase().includes(kw) ||
                x.tenPhuCap.toLowerCase().includes(kw))
            : [...this.nvPhuCapList];
    }

    openAddNvPhuCap(): void {
        this.selectedNvPhuCap = null;
        this.nvForm.reset({ soTien: 0 });
        this.nvSearchTerm = '';
        this.soTienDisplayNv = '';
        this.nvDropdownFiltered = this.nhanVienList.slice(0, 30);
        this.showNvDropdown = false;
        this.errorMsg = '';
        this.showNvModal = true;
    }

    openEditNvPhuCap(item: NvPhuCapDto): void {
        this.selectedNvPhuCap = item;
        const ngayBat = item.ngayBatDau ? item.ngayBatDau.substring(0, 10) : '';
        const ngayKet = item.ngayKetThuc ? item.ngayKetThuc.substring(0, 10) : '';
        const nv = this.nhanVienList.find(x => x.id === item.nvHoSoId);
        this.nvSearchTerm = nv ? `${nv.maNhanVien} - ${nv.hoTen}` : item.hoTen;
        this.nvDropdownFiltered = this.nhanVienList.slice(0, 30);
        this.showNvDropdown = false;
        this.soTienDisplayNv = this.fmtSoTien(item.soTien);
        this.nvForm.patchValue({
            nvHoSoId: item.nvHoSoId,
            phuCapLoaiId: item.phuCapLoaiId,
            soTien: item.soTien,
            ngayBatDau: ngayBat,
            ngayKetThuc: ngayKet,
            ghiChu: item.ghiChu
        });
        this.errorMsg = '';
        this.showNvModal = true;
    }

    saveNvPhuCap(): void {
        if (this.nvForm.invalid) return;
        this.saving = true;
        this.errorMsg = '';
        const payload: NvPhuCapCreateDto = this.nvForm.value;

        const call = this.selectedNvPhuCap
            ? this.phuCapApi.update(this.selectedNvPhuCap.id, { ...payload, dangApDung: this.selectedNvPhuCap.dangApDung })
            : this.phuCapApi.create(payload);

        call.subscribe({
            next: () => {
                this.saving = false;
                this.showNvModal = false;
                this.toast.show('Đã lưu phụ cấp nhân viên', 'success');
                this.loadNvPhuCap();
            },
            error: err => {
                this.saving = false;
                this.errorMsg = err?.error?.message ?? 'Có lỗi xảy ra';
            }
        });
    }

    deleteNvPhuCap(id: number): void {
        if (!confirm('Bạn có chắc muốn xóa phụ cấp này?')) return;
        this.phuCapApi.delete(id).subscribe({
            next: () => {
                this.toast.show('Đã xóa', 'success');
                this.loadNvPhuCap();
            }
        });
    }

    formatCurrency(val: number): string {
        if (!val && val !== 0) return '—';
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val);
    }

    /** Format a raw number as Vietnamese thousands: 1.000.000 */
    fmtSoTien(val: number | null | undefined): string {
        if (!val && val !== 0) return '';
        return Math.round(val).toLocaleString('vi-VN');
    }

    /** Parse a formatted string like '1.000.000' back to number */
    private parseSoTien(str: string): number {
        return parseInt(str.replace(/\./g, '').replace(/[^\d]/g, ''), 10) || 0;
    }

    onSoTienInputNv(event: Event): void {
        const raw = (event.target as HTMLInputElement).value;
        const num = this.parseSoTien(raw);
        this.nvForm.patchValue({ soTien: num });
        this.soTienDisplayNv = num ? this.fmtSoTien(num) : '';
    }

    onSoTienInputItem(event: Event): void {
        const raw = (event.target as HTMLInputElement).value;
        const num = this.parseSoTien(raw);
        this.itemForm.patchValue({ soTien: num });
        this.soTienDisplayItem = num ? this.fmtSoTien(num) : '';
    }

    // ─── Employee dropdown helpers ──────────────────────────────────────────
    loadNhanVien(): void {
        this.nhanVienApi.getAllNhanVien().subscribe({
            next: data => {
                this.nhanVienList = data;
                this.nvDropdownFiltered = data.slice(0, 30);
            }
        });
    }

    onNvSearch(event: Event): void {
        this.nvSearchTerm = (event.target as HTMLInputElement).value;
        const kw = this.nvSearchTerm.toLowerCase();
        this.nvDropdownFiltered = kw
            ? this.nhanVienList.filter(x =>
                x.hoTen.toLowerCase().includes(kw) ||
                x.maNhanVien.toLowerCase().includes(kw)).slice(0, 30)
            : this.nhanVienList.slice(0, 30);
        this.showNvDropdown = true;
        // clear form selection when user types
        this.nvForm.patchValue({ nvHoSoId: null });
    }

    selectNhanVienInModal(nv: NhanVienListItemDto): void {
        this.nvSearchTerm = `${nv.maNhanVien} - ${nv.hoTen}`;
        this.nvForm.patchValue({ nvHoSoId: nv.id });
        this.showNvDropdown = false;
    }

    // ─── Thưởng & Phạt ────────────────────────────────────────────────────────
    loadBangLuong(): void {
        this.luongApi.getList().subscribe({
            next: data => {
                this.bangLuongList = data;
                this.applyBlFilter();
            }
        });
    }

    applyBlFilter(): void {
        const kw = this.blKeyword.toLowerCase();
        this.bangLuongFiltered = this.bangLuongList.filter(x => {
            const matchThang = this.blThangFilter === '' || x.thang === Number(this.blThangFilter);
            const matchNam = this.blNamFilter === '' || x.nam === Number(this.blNamFilter);
            const matchKw = !kw || x.hoTen.toLowerCase().includes(kw);
            return matchThang && matchNam && matchKw;
        });
    }

    selectBangLuong(item: BangLuongThangListItemDto): void {
        this.selectedBangLuong = item;
        this.loadingItems = true;
        this.luongApi.getItems(item.id).subscribe({
            next: data => { this.blItems = data; this.loadingItems = false; },
            error: () => { this.loadingItems = false; }
        });
    }

    openAddItem(): void {
        this.itemForm.reset({ loai: 'THUONG', soTien: 0 });
        this.soTienDisplayItem = '';
        this.errorMsg = '';
        this.showItemModal = true;
    }

    saveItem(): void {
        if (!this.selectedBangLuong || this.itemForm.invalid) return;
        this.saving = true;
        this.errorMsg = '';
        const payload: BangLuongItemCreateDto = this.itemForm.value;
        this.luongApi.addItem(this.selectedBangLuong.id, payload).subscribe({
            next: item => {
                this.blItems.push(item);
                this.saving = false;
                this.showItemModal = false;
                this.toast.show('Đã thêm khoản thưởng/phạt', 'success');
                // refresh the list row totals
                this.loadBangLuong();
            },
            error: err => {
                this.saving = false;
                this.errorMsg = err?.error?.message ?? 'Có lỗi xảy ra';
            }
        });
    }

    deleteBlItem(itemId: number): void {
        if (!this.selectedBangLuong) return;
        if (!confirm('Xóa khoản này?')) return;
        this.luongApi.deleteItem(this.selectedBangLuong.id, itemId).subscribe({
            next: () => {
                this.blItems = this.blItems.filter(x => x.id !== itemId);
                this.toast.show('Đã xóa', 'success');
                this.loadBangLuong();
            }
        });
    }

    getStatusLabel(s: string): string {
        const map: Record<string, string> = {
            TAM_TINH: 'Tạm tính', CHO_DUYET_GIAM_DOC: 'Chờ duyệt',
            DA_DUYET: 'Đã duyệt', TU_CHOI: 'Từ chối', DA_KHOA: 'Đã khóa'
        };
        return map[s] ?? s;
    }

    get blTongThuong(): number {
        return this.blItems.filter(i => i.loai === 'THUONG').reduce((a, b) => a + b.soTien, 0);
    }

    get blTongKhauTru(): number {
        return this.blItems.filter(i => i.loai === 'KHAU_TRU').reduce((a, b) => a + b.soTien, 0);
    }

    // Filtered sub-lists for grouped display
    get blThuongItems() {
        return this.blItems.filter(i => i.loai === 'THUONG');
    }

    get blKhauTruItems() {
        return this.blItems.filter(i => i.loai === 'KHAU_TRU');
    }

    // Net balance helpers
    get blNetPositive(): boolean {
        return this.blTongThuong >= this.blTongKhauTru;
    }

    get blNetAbs(): number {
        return Math.abs(this.blTongThuong - this.blTongKhauTru);
    }

    // Summary stats across filtered payroll list
    get blFilteredThuongTotal(): number {
        return this.bangLuongFiltered.reduce((s, r) => s + (r.thuong || 0), 0);
    }

    get blFilteredKhauTruTotal(): number {
        return this.bangLuongFiltered.reduce((s, r) => s + (r.khauTru || 0), 0);
    }
}
