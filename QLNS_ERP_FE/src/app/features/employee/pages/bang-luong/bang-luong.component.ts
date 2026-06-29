import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup } from '@angular/forms';
import { LuongApiService } from 'src/app/core/services/api/luong-api.service';
import {
  LuongCuaToiDto,
  LuongFilterVm,
  LuongThongKeVm,
  TrangThaiLuong
} from 'src/app/core/models/luong.model';

@Component({
  selector: 'app-bang-luong',
  templateUrl: './bang-luong.component.html',
  styleUrls: ['./bang-luong.component.scss'],
})
export class BangLuongComponent implements OnInit {
  loading = false;
  errorMsg = '';

  // data
  all: LuongCuaToiDto[] = [];
  list: LuongCuaToiDto[] = [];
  expandedKey: string | null = null;

  // stats
  thongKe: LuongThongKeVm = {
    nam: new Date().getFullYear(),
    tongLuongNam: 0,
    tongOtNam: 0,
    tongCongNam: 0,
    thangGanNhat: null,
    luongThangGanNhat: null
  };

  // filters
  filterForm!: FormGroup;
  keywordCtrl = new FormControl<string>('', { nonNullable: true });

  readonly trangThaiOptions: Array<{ value: TrangThaiLuong | 'ALL'; label: string }> = [
    { value: 'ALL', label: 'Tất cả trạng thái' },
    { value: 'TAM_TINH', label: 'Tạm tính' },
    { value: 'CHO_DUYET_GIAM_DOC', label: 'Chờ Giám đốc duyệt' },
    { value: 'DA_DUYET', label: 'Đã duyệt' },
    { value: 'DA_KHOA', label: 'Đã khóa' },
    { value: 'TU_CHOI', label: 'Từ chối' },
  ];

  constructor(
    private api: LuongApiService,
    private fb: FormBuilder
  ) { }

  ngOnInit(): void {
    this.initFilters();
    this.bindFilterChanges();
    this.load();
  }

  private initFilters(): void {
    const now = new Date();
    const year = now.getFullYear();

    this.filterForm = this.fb.group({
      nam: [year],
      thang: ['ALL'],
      trangThai: ['ALL'],
    });
  }

  private bindFilterChanges(): void {
    this.filterForm.valueChanges.subscribe(() => this.applyFilter());
    this.keywordCtrl.valueChanges.subscribe(() => this.applyFilter());
  }

  load(): void {
    this.loading = true;
    this.errorMsg = '';

    this.api.getMySalary().subscribe({
      next: (res) => {
        this.all = Array.isArray(res) ? res : [];
        this.applyFilter();
        this.recalcThongKe();
        this.loading = false;
      },
      error: () => {
        this.errorMsg = 'Không tải được dữ liệu lương. Vui lòng imply lại.';
        this.loading = false;
      }
    });
  }

  private applyFilter(): void {
    const f = this.getFilter();
    const q = (f.keyword || '').trim().toLowerCase();

    this.list = this.all
      .filter(x => (f.nam ? x.nam === Number(f.nam) : true))
      .filter(x => (f.thang === 'ALL' ? true : x.thang === Number(f.thang)))
      .filter(x => (f.trangThai === 'ALL' ? true : x.trangThai === f.trangThai))
      .filter(x => {
        if (!q) return true;
        const haystack = `${x.thang}/${x.nam} ${x.trangThai}`.toLowerCase();
        return haystack.includes(q);
      })
      .sort((a, b) => (b.nam - a.nam) || (b.thang - a.thang));

    // nếu đang expand 1 row mà row đó bị filter mất thì collapse
    if (this.expandedKey && !this.list.some(x => this.keyOf(x) === this.expandedKey)) {
      this.expandedKey = null;
    }

    this.recalcThongKe();
  }

  private getFilter(): LuongFilterVm {
    const v = this.filterForm.value as any;
    return {
      nam: Number(v.nam),
      thang: (v.thang ?? 'ALL') as any,
      trangThai: (v.trangThai ?? 'ALL') as any,
      keyword: this.keywordCtrl.value
    };
  }

  private recalcThongKe(): void {
    const f = this.getFilter();
    const year = f.nam;

    const inYear = this.all.filter(x => x.nam === year);
    const tongLuongNam = inYear.reduce((s, x) => s + (Number(x.tongLuong) || 0), 0);
    const tongOtNam = inYear.reduce((s, x) => s + (Number(x.tongOt) || 0), 0);
    const tongCongNam = inYear.reduce((s, x) => s + (Number(x.tongCong) || 0), 0);

    const latest = [...inYear].sort((a, b) => (b.nam - a.nam) || (b.thang - a.thang))[0] || null;

    this.thongKe = {
      nam: year,
      tongLuongNam,
      tongOtNam,
      tongCongNam,
      thangGanNhat: latest ? latest.thang : null,
      luongThangGanNhat: latest ? latest.tongLuong : null
    };
  }

  toggleExpand(item: LuongCuaToiDto): void {
    const key = this.keyOf(item);
    this.expandedKey = this.expandedKey === key ? null : key;
  }

  isExpanded(item: LuongCuaToiDto): boolean {
    return this.expandedKey === this.keyOf(item);
  }

  private keyOf(x: LuongCuaToiDto): string {
    return `${x.nam}-${x.thang}`;
  }

  statusText(st: string): string {
    switch (st) {
      case 'TAM_TINH': return 'Tạm tính';
      case 'CHO_DUYET_GIAM_DOC': return 'Chờ Giám đốc duyệt';
      case 'DA_DUYET': return 'Đã duyệt';
      case 'DA_KHOA': return 'Đã khóa';
      case 'TU_CHOI': return 'Từ chối';
      default: return st || '-';
    }
  }

  statusBadgeClass(st: string): string {
    switch (st) {
      case 'TAM_TINH': return 'badge-tam-tinh';
      case 'CHO_DUYET_GIAM_DOC': return 'badge-cho-duyet';
      case 'DA_DUYET': return 'badge-da-duyet';
      case 'DA_KHOA': return 'badge-da-duyet';
      case 'TU_CHOI': return 'badge-tu-choi';
      default: return 'badge-secondary';
    }
  }

  getStatusClass(st: string): string {
    switch (st) {
      case 'TAM_TINH': return 'pending';
      case 'CHO_DUYET_GIAM_DOC': return 'pending';
      case 'DA_DUYET': return 'approved';
      case 'TU_CHOI': return 'rejected';
      default: return 'pending';
    }
  }

  monthLabel(x: LuongCuaToiDto): string {
    const mm = String(x.thang).padStart(2, '0');
    return `${mm}/${x.nam}`;
  }

  getNet(item: LuongCuaToiDto): number {
    return (Number(item.tongLuong) || 0);
  }

  /** Số tháng đã có bảng lương trong năm đang chọn */
  get soThangDaTinh(): number {
    const f = this.filterForm?.value as any;
    const year = f?.nam ? Number(f.nam) : new Date().getFullYear();
    return this.all.filter(x => x.nam === year).length;
  }

  /** Lương trung bình/tháng của năm đang chọn (chỉ các tháng đã tính) */
  get luongTrungBinh(): number {
    const n = this.soThangDaTinh;
    return n > 0 ? Math.round(this.thongKe.tongLuongNam / n) : 0;
  }

  /** Phần trăm đóng góp của từng thành phần so với tổng lương */
  pctOf(value: number, total: number): number {
    if (!total || !value) return 0;
    return Math.round(Math.abs(value) / Math.abs(total) * 100);
  }

  /** Màu badge theo trạng thái, dùng cho Bootstrap text-bg-* */
  statusBootstrapClass(st: string): string {
    switch (st) {
      case 'DA_DUYET': return 'text-bg-success';
      case 'DA_KHOA': return 'text-bg-success';
      case 'CHO_DUYET_GIAM_DOC': return 'text-bg-warning';
      case 'TU_CHOI': return 'text-bg-danger';
      case 'TAM_TINH': return 'text-bg-secondary';
      default: return 'text-bg-secondary';
    }
  }
}
