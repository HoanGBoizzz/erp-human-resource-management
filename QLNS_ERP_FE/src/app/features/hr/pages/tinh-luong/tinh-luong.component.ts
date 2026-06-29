import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { LuongApiService } from 'src/app/core/services/api/luong-api.service';
import { NhanVienApiService } from 'src/app/core/services/api/nhan-vien-api.service';
import { ChamCongApiService } from 'src/app/core/services/api/cham-cong-api.service';
import {
    BangLuongThangDto,
    BangLuongThangListItemDto,
    TinhLuongRequestDto,
} from 'src/app/core/models/luong.model';
import { NhanVienListItemDto } from 'src/app/core/models/nhan-vien.model';
import { BangCongThangSummaryDto } from 'src/app/core/models/cham-cong.model';
import { ToastService } from 'src/app/shared/services/toast.service';

@Component({
    selector: 'app-tinh-luong',
    templateUrl: './tinh-luong.component.html',
    styleUrls: ['./tinh-luong.component.scss'],
})
export class TinhLuongComponent implements OnInit {

    tinhLuongMode: 'all' | 'individual' = 'all';

    nhanViens: NhanVienListItemDto[] = [];
    loadingNhanViens = false;

    previewSalary: BangLuongThangDto | null = null;
    loadingPreview = false;

    processing = false;
    errorMsg = '';

    /** Danh sách bảng lương để kiểm tra trạng thái */
    list: BangLuongThangListItemDto[] = [];

    bangCongThangList: BangCongThangSummaryDto[] = [];
    bangCongThangLoadedNam = 0;

    form: FormGroup;

    constructor(
        private api: LuongApiService,
        private nhanVienApi: NhanVienApiService,
        private chamCongApi: ChamCongApiService,
        private toast: ToastService,
        private router: Router,
        private fb: FormBuilder
    ) {
        this.form = this.fb.group({
            nvHoSoId: [null],
            thang: [new Date().getMonth() + 1, [Validators.required, Validators.min(1), Validators.max(12)]],
            nam: [new Date().getFullYear(), [Validators.required, Validators.min(2020)]],
        });
    }

    ngOnInit(): void {
        this.loadNhanViens();
        this.loadList();
        this.loadBangCong(this.form.value.nam);
    }

    loadList(): void {
        this.api.getList().subscribe({
            next: (data) => { this.list = data || []; },
            error: () => { }
        });
    }

    loadNhanViens(): void {
        this.loadingNhanViens = true;
        this.nhanVienApi.getPaged({ pageIndex: 1, pageSize: 1000, keyword: '' })
            .pipe(finalize(() => (this.loadingNhanViens = false)))
            .subscribe({
                next: (data) => {
                    this.nhanViens = data.items.filter(nv => nv.trangThaiLamViec === 1);
                },
                error: () => {
                    this.toast.danger('Không thể tải danh sách nhân viên');
                    this.nhanViens = [];
                }
            });
    }

    loadBangCong(nam: number): void {
        this.chamCongApi.getBangCongThang(nam).subscribe({
            next: (list) => {
                this.bangCongThangList = list;
                this.bangCongThangLoadedNam = nam;
            },
            error: () => { this.bangCongThangList = []; }
        });
    }

    onModeChange(): void {
        this.previewSalary = null;
        if (this.tinhLuongMode === 'all') {
            this.form.patchValue({ nvHoSoId: null });
        }
    }

    onNhanVienChange(): void {
        const { nvHoSoId, thang, nam } = this.form.value;
        if (!nvHoSoId || !thang || !nam) {
            this.previewSalary = null;
            return;
        }
        // Không gọi preview nếu đã duyệt / đang chờ duyệt
        if (this.isEmployeeApproved(nvHoSoId, thang, nam) || this.isEmployeePending(nvHoSoId, thang, nam)) {
            this.previewSalary = null;
            return;
        }
        this.loadPreview(nvHoSoId, thang, nam);
    }

    onNamChange(): void {
        this.loadBangCong(this.form.value.nam);
        this.previewSalary = null;
    }

    loadPreview(nvHoSoId: number, thang: number, nam: number): void {
        this.loadingPreview = true;
        this.previewSalary = null;

        const payload: TinhLuongRequestDto = { nvHoSoId, thang, nam };
        this.api.tinhLuong(payload)
            .pipe(finalize(() => (this.loadingPreview = false)))
            .subscribe({
                next: (result) => { this.previewSalary = result; },
                error: (err: any) => {
                    this.previewSalary = null;
                    const msg = err?.error?.message || err?.error || '';
                    if (msg) this.toast.danger(msg);
                }
            });
    }

    isBangCongLocked(thang: number, nam: number): boolean {
        if (this.bangCongThangLoadedNam !== nam) return true;
        const entry = this.bangCongThangList.find(m => m.thang === thang && m.nam === nam);
        return entry?.trangThaiCong === 'DA_CHOT_CONG';
    }

    /** Kiểm tra nhân viên đã được GD duyệt hoặc khóa */
    isEmployeeApproved(nvId: number, thang: number, nam: number): boolean {
        const found = this.list.find(x => x.nvHoSoId === nvId && x.thang === thang && x.nam === nam);
        return found?.trangThai === 'DA_DUYET' || found?.trangThai === 'DA_KHOA';
    }

    /** Kiểm tra nhân viên đang chờ GD duyệt */
    isEmployeePending(nvId: number, thang: number, nam: number): boolean {
        const found = this.list.find(x => x.nvHoSoId === nvId && x.thang === thang && x.nam === nam);
        return found?.trangThai === 'CHO_DUYET_GIAM_DOC';
    }

    save(): void {
        if (this.tinhLuongMode === 'individual') {
            if (!this.form.value.nvHoSoId) {
                this.toast.warning('Vui lòng chọn nhân viên');
                return;
            }
            const { nvHoSoId, thang, nam } = this.form.value;
            if (this.isEmployeeApproved(nvHoSoId, thang, nam)) {
                this.toast.danger('Bảng lương này đã được Giám đốc duyệt, không thể tính lại');
                return;
            }
            if (this.isEmployeePending(nvHoSoId, thang, nam)) {
                this.toast.warning('Bảng lương này đang chờ Giám đốc duyệt, không thể tính lại');
                return;
            }
            if (this.previewSalary) {
                // Đã tính xong preview, xác nhận
                this.toast.success('Đã tính lương thành công!');
                this.loadList();
                this.previewSalary = null;
                this.form.patchValue({ nvHoSoId: null });
            } else {
                const { thang, nam } = this.form.value;
                if (!this.isBangCongLocked(thang, nam)) {
                    this.toast.warning(`Vui lòng chốt công tháng ${thang}/${nam} trước khi tính lương`);
                    return;
                }
                this.loadPreview(this.form.value.nvHoSoId, thang, nam);
            }
        } else {
            this.tinhLuongTatCa();
        }
    }

    tinhLuongTatCa(): void {
        const { thang, nam } = this.form.value;
        if (!thang || !nam) {
            this.toast.warning('Vui lòng chọn tháng và năm');
            return;
        }
        if (!this.isBangCongLocked(thang, nam)) {
            this.toast.warning(`Vui lòng chốt công tháng ${thang}/${nam} trước khi tính lương`);
            return;
        }

        // Lọc ra những NV chưa được duyệt/khóa
        const blocked = this.nhanViens.filter(nv => this.isEmployeeApproved(nv.id, thang, nam));
        const eligible = this.nhanViens.filter(nv => !this.isEmployeeApproved(nv.id, thang, nam));

        if (eligible.length === 0) {
            this.toast.warning('Tất cả nhân viên đã có bảng lương được Giám đốc duyệt, không thể tính lại');
            return;
        }

        const blockedMsg = blocked.length > 0
            ? ` (bỏ qua ${blocked.length} NV đã được duyệt)`
            : '';
        if (!confirm(`Bạn có chắc muốn tính lương cho ${eligible.length} nhân viên trong tháng ${thang}/${nam}?${blockedMsg}`)) return;

        this.processing = true;
        const requests = eligible.map(nv =>
            this.api.tinhLuong({ nvHoSoId: nv.id, thang, nam })
        );

        forkJoin(requests)
            .pipe(finalize(() => (this.processing = false)))
            .subscribe({
                next: (results) => {
                    const total = results.reduce((sum, r) => sum + (r.tongLuong || 0), 0);
                    this.toast.success(`Đã tính lương cho ${results.length} nhân viên. Tổng: ${this.formatCurrency(total)}`);
                    this.loadList();
                },
                error: (err: any) => {
                    this.toast.danger(err?.error?.message || 'Có lỗi xảy ra khi tính lương hàng loạt');
                }
            });
    }

    goToBangLuong(): void {
        this.router.navigate(['/hr/bang-luong']);
    }

    getThangStatus(thang: number, nam: number): string {
        if (!nam) return '';
        const count = this.list.filter(x => x.thang === thang && x.nam === nam).length;
        if (count === 0) return '- Chưa có bảng lương';
        return `- Đã tính ${count} bảng lương`;
    }

    getNhanVienStatus(nvId: number, thang: number, nam: number): string {
        if (!thang || !nam) return '';
        const found = this.list.find(x => x.nvHoSoId === nvId && x.thang === thang && x.nam === nam);
        if (!found) return '- Chưa tính';
        const statusMap: Record<string, string> = {
            'TAM_TINH': '✓ Tạm tính',
            'CHO_DUYET_GIAM_DOC': '✓ Chờ duyệt',
            'DA_DUYET': '✓ Đã duyệt',
            'TU_CHOI': '✓ Từ chối',
            'DA_KHOA': '✓ Đã khóa'
        };
        return statusMap[found.trangThai] || '✓ Đã tính';
    }

    // ─── Computed values from preview ────────────────────────────────────────
    get previewLuongNgay(): number {
        if (!this.previewSalary) return 0;
        return this.previewSalary.luongCoBanTinh / 26;
    }
    get previewLuongCong(): number {
        if (!this.previewSalary) return 0;
        return this.previewLuongNgay * this.previewSalary.tongCong;
    }
    get previewLuongOt(): number {
        if (!this.previewSalary) return 0;
        return (this.previewLuongNgay / 8) * this.previewSalary.tongOt * 1.5;
    }

    /** Giá trị thực tế được tính vào tổng (0 nếu flag tắt) */
    get phuCapActual(): number {
        return this.previewSalary?.coTinhPhuCap !== false ? (this.previewSalary?.phuCapTinh ?? 0) : 0;
    }
    get luongOtActual(): number {
        return this.previewSalary?.coTinhOT !== false ? this.previewLuongOt : 0;
    }
    get thuongActual(): number {
        return this.previewSalary?.coTinhThuong !== false ? (this.previewSalary?.thuong ?? 0) : 0;
    }
    get khauTruActual(): number {
        return this.previewSalary?.coTinhKhauTru !== false ? (this.previewSalary?.khauTru ?? 0) : 0;
    }

    /** Chuỗi hiển thị chi tiết phụ cấp, ngăn cách bởi ";" */
    get phuCapBreakdownStr(): string {
        const items = this.previewSalary?.chiTietPhuCap;
        if (!items || items.length === 0) return `${this.fmtNum(this.previewSalary?.phuCapTinh ?? 0)}`;
        return items.map(x => `${x.ten}: ${this.fmtNum(x.soTien)}`).join('; ');
    }

    /** Chuỗi hiển thị chi tiết khấu trừ (đi muộn + T&P), ngăn cách bởi ";" */
    get khauTruBreakdownStr(): string {
        if (!this.previewSalary) return '0';
        const parts: string[] = [];
        const diMuon = this.previewSalary.khauTruDiMuon ?? 0;
        const soLan = this.previewSalary.soLanDiMuon ?? 0;
        if (diMuon > 0)
            parts.push(`Phạt đi muộn (${soLan} lần × ${this.fmtNum(diMuon / (soLan || 1))}): ${this.fmtNum(diMuon)}`);
        const ktItems = this.previewSalary.chiTietKhauTruItems ?? [];
        for (const it of ktItems)
            parts.push(`${it.ten}: ${this.fmtNum(it.soTien)}`);
        return parts.length === 0 ? '0' : parts.join('; ');
    }

    formatCurrency(value: number): string {
        return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
    }
    fmtNum(value: number): string {
        return new Intl.NumberFormat('vi-VN').format(Math.round(value));
    }

    get availableYears(): number[] {
        const y = new Date().getFullYear();
        return Array.from({ length: 5 }, (_, i) => y - i);
    }
}
