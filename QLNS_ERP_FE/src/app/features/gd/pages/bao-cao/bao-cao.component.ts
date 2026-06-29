import { Component, OnInit, HostListener } from '@angular/core';
import { forkJoin } from 'rxjs';
import { NhanVienApiService } from 'src/app/core/services/api/nhan-vien-api.service';
import { LuongApiService } from 'src/app/core/services/api/luong-api.service';
import { DonPhepApiService } from 'src/app/core/services/api/don-phep-api.service';
import { DuAnApiService } from 'src/app/core/services/api/du-an-api.service';
import { DeXuatGiamDocApiService } from 'src/app/core/services/api/de-xuat-giam-doc-api.service';
import { PhongBanApiService } from 'src/app/core/services/api/phong-ban-api.service';

// ---------------------------------------------------------------------------
// Kiểu dữ liệu ngx-charts
// ---------------------------------------------------------------------------
export interface ChartItem { name: string; value: number; }
export interface ChartSeries { name: string; series: ChartItem[]; }

@Component({
  selector: 'app-bao-cao',
  templateUrl: './bao-cao.component.html',
  styleUrls: ['./bao-cao.component.scss'],
})
export class BaoCaoComponent implements OnInit {

  // ── Bộ lọc ──────────────────────────────────────────────────────────────
  selectedYear: number = new Date().getFullYear();
  years: number[] = [2023, 2024, 2025, 2026];

  // ── Trạng thái ──────────────────────────────────────────────────────────
  loading = false; exporting = false; errorMsg = '';
  todayStr = new Date().toLocaleDateString('vi-VN');

  // ── Kích thước chart (tự điều chỉnh theo màn hình) ─────────────────────
  pieView: [number, number] = [500, 360];
  barView: [number, number] = [500, 320];
  lineView: [number, number] = [500, 320];

  // ── Màu sắc (cast as any để tương thích ngx-charts Color type) ──────────
  // Đang làm = xanh rõ | Nghỉ = đỏ rõ
  colorNhanSu: any = { name: 'nhanSu', selectable: true, group: 'Ordinal', domain: ['#16A34A', '#DC2626'] };
  // Line chart lương: xanh dương đậm
  colorLuong: any = { name: 'luong', selectable: true, group: 'Ordinal', domain: ['#1D4ED8', '#0EA5E9', '#06B6D4'] };
  // Chờ = vàng | Đã duyệt = xanh lá | Từ chối = đỏ
  colorDonPhep: any = { name: 'donPhep', selectable: true, group: 'Ordinal', domain: ['#D97706', '#16A34A', '#DC2626'] };
  // Nhập liệu = xám | Chờ GĐ = vàng | Đã duyệt = xanh | Từ chối = đỏ
  colorDuAn: any = { name: 'duAn', selectable: true, group: 'Ordinal', domain: ['#475569', '#D97706', '#16A34A', '#DC2626'] };
  // Nháp = xám | Chờ = vàng | Đã duyệt = xanh | Từ chối = đỏ
  colorDeXuat: any = { name: 'deXuat', selectable: true, group: 'Ordinal', domain: ['#64748B', '#D97706', '#16A34A', '#DC2626'] };
  colorPhongBan: any = { name: 'phongBan', selectable: true, group: 'Ordinal', domain: ['#1D4ED8', '#16A34A', '#D97706', '#7C3AED', '#DC2626', '#0891B2', '#EA580C', '#92400E'] };

  // ── Metrics: Nhân sự ────────────────────────────────────────────────────
  metNhanSu = { total: 0, dangLam: 0, nghiViec: 0, nghiKhongLuong: 0 };

  // ── Metrics: Lương ──────────────────────────────────────────────────────
  metLuong = { tongQuyLuong: 0, luongTB: 0, tongOt: 0, soNguoi: 0 };

  // ── Metrics: Đơn phép ───────────────────────────────────────────────────
  metDonPhep = { total: 0, choDuyet: 0, daDuyet: 0, tuChoi: 0 };

  // ── Metrics: Dự án ──────────────────────────────────────────────────────
  metDuAn = { total: 0, choDuyet: 0, daDuyet: 0, tuChoi: 0, dangNhap: 0 };

  // ── Metrics: Đề xuất ────────────────────────────────────────────────────
  metDeXuat = { total: 0, nhap: 0, choDuyet: 0, daDuyet: 0, tuChoi: 0 };

  // ── Chart data ──────────────────────────────────────────────────────────
  nhanSuPie: ChartItem[] = [];
  nhanSuByPhongBan: ChartItem[] = [];

  luongTheoThang: ChartSeries[] = [];
  luongByPhongBan: ChartItem[] = [];

  donPhepPie: ChartItem[] = [];
  donPhepByLoai: ChartItem[] = [];

  duAnPie: ChartItem[] = [];

  deXuatPie: ChartItem[] = [];
  deXuatTheoThang: ChartItem[] = [];

  // ── Label formatter ─────────────────────────────────────────────────────
  formatMoney = (val: number) => this.formatCurrency(val);
  formatPercent = (val: number) => `${val}%`;

  constructor(
    private nhanVienApi: NhanVienApiService,
    private luongApi: LuongApiService,
    private donPhepApi: DonPhepApiService,
    private duAnApi: DuAnApiService,
    private deXuatApi: DeXuatGiamDocApiService,
    private phongBanApi: PhongBanApiService,
  ) { }

  ngOnInit(): void {
    this.updateChartSizes();
    this.loadAll();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.updateChartSizes();
  }

  // Responsive chart sizes
  private updateChartSizes(): void {
    const w = window.innerWidth;
    if (w < 768) {
      this.pieView = [300, 240];
      this.barView = [300, 240];
      this.lineView = [300, 240];
    } else if (w < 1200) {
      this.pieView = [420, 310];
      this.barView = [420, 300];
      this.lineView = [420, 300];
    } else {
      this.pieView = [500, 360];
      this.barView = [500, 320];
      this.lineView = [500, 320];
    }
  }

  onYearChange(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loading = true;
    this.errorMsg = '';

    forkJoin({
      nhanViens: this.nhanVienApi.getAllNhanVien(),
      luongs: this.luongApi.getList(),
      donPheps: this.donPhepApi.getList(),
      duAns: this.duAnApi.getList(),
      deXuats: this.deXuatApi.getList(),
    }).subscribe({
      next: ({ nhanViens, luongs, donPheps, duAns, deXuats }) => {
        const year = Number(this.selectedYear);

        // ── 1. NHÂN SỰ ─────────────────────────────────────────────────────
        const dangLam = nhanViens.filter(n => n.trangThaiLamViec === 1).length;
        const nghiViec = nhanViens.filter(n => n.trangThaiLamViec !== 1).length;

        this.metNhanSu = {
          total: nhanViens.length,
          dangLam,
          nghiViec,
          nghiKhongLuong: 0,
        };

        this.nhanSuPie = [
          { name: 'Đang làm việc', value: dangLam },
          { name: 'Nghỉ việc', value: nghiViec },
        ].filter(x => x.value > 0);

        // NV theo phòng ban
        const pbMap = new Map<string, number>();
        nhanViens.filter(n => n.trangThaiLamViec === 1).forEach(n => {
          const pb = n.tenPhongBan || 'Chưa phân phòng';
          pbMap.set(pb, (pbMap.get(pb) ?? 0) + 1);
        });
        this.nhanSuByPhongBan = this.mapToChart(pbMap);

        // ── 2. LƯƠNG ───────────────────────────────────────────────────────
        const luongYear = luongs.filter(l => l.nam === year);

        const tongQuy = luongYear.reduce((s, l) => s + (l.tongLuong || 0), 0);
        const tongOt = luongYear.reduce((s, l) => s + (l.tongOt || 0), 0);
        this.metLuong = {
          tongQuyLuong: tongQuy,
          luongTB: luongYear.length ? Math.round(tongQuy / luongYear.length) : 0,
          tongOt,
          soNguoi: luongYear.length,
        };

        // Quỹ lương theo tháng (grouped bar: T1..T12)
        const luongByThang = new Map<string, number>();
        for (let m = 1; m <= 12; m++) luongByThang.set(`T${m}`, 0);
        luongYear.forEach(l => {
          const key = `T${l.thang}`;
          luongByThang.set(key, (luongByThang.get(key) ?? 0) + l.tongLuong);
        });
        this.luongTheoThang = [{
          name: `Quỹ lương ${year}`,
          series: Array.from(luongByThang.entries()).map(([name, value]) => ({ name, value }))
        }];

        // Lương TB theo phòng ban
        const pbLuongSum = new Map<string, number>();
        const pbLuongCnt = new Map<string, number>();
        luongYear.forEach(l => {
          const pb = l.tenPhongBan || 'Khác';
          pbLuongSum.set(pb, (pbLuongSum.get(pb) ?? 0) + l.tongLuong);
          pbLuongCnt.set(pb, (pbLuongCnt.get(pb) ?? 0) + 1);
        });
        this.luongByPhongBan = Array.from(pbLuongSum.entries()).map(([name, sum]) => ({
          name,
          value: Math.round(sum / (pbLuongCnt.get(name) ?? 1)),
        }));

        // ── 3. ĐƠN PHÉP ───────────────────────────────────────────────────
        const donPhepYear = donPheps.filter(d => new Date(d.tuNgay).getFullYear() === year);
        const dpCho = donPhepYear.filter(d => d.trangThai === 'CHO_DUYET').length;
        const dpDuyet = donPhepYear.filter(d => d.trangThai === 'DA_DUYET').length;
        const dpTu = donPhepYear.filter(d => d.trangThai === 'TU_CHOI').length;

        this.metDonPhep = { total: donPhepYear.length, choDuyet: dpCho, daDuyet: dpDuyet, tuChoi: dpTu };

        this.donPhepPie = [
          { name: 'Chờ duyệt', value: dpCho },
          { name: 'Đã duyệt', value: dpDuyet },
          { name: 'Từ chối', value: dpTu },
        ].filter(x => x.value > 0);

        const loaiMap = new Map<string, number>();
        donPhepYear.forEach(d => loaiMap.set(d.tenLoaiPhep, (loaiMap.get(d.tenLoaiPhep) ?? 0) + 1));
        this.donPhepByLoai = this.mapToChart(loaiMap);

        // ── 4. DỰ ÁN ──────────────────────────────────────────────────────
        const daNhap = duAns.filter(d => d.trangThaiDuAn === 'DANG_NHAP').length;
        const daChoDuyet = duAns.filter(d => d.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC').length;
        const daDuyet2 = duAns.filter(d => d.trangThaiDuAn === 'DA_DUYET').length;
        const daTuChoi = duAns.filter(d => d.trangThaiDuAn === 'TU_CHOI').length;

        this.metDuAn = { total: duAns.length, choDuyet: daChoDuyet, daDuyet: daDuyet2, tuChoi: daTuChoi, dangNhap: daNhap };

        this.duAnPie = [
          { name: 'Đang nhập', value: daNhap },
          { name: 'Chờ duyệt GĐ', value: daChoDuyet },
          { name: 'Đã duyệt', value: daDuyet2 },
          { name: 'Từ chối', value: daTuChoi },
        ].filter(x => x.value > 0);

        // ── 5. ĐỀ XUẤT GIÁM ĐỐC ────────────────────────────────────────────
        const dxYear = deXuats.filter(d => new Date(d.createdAt).getFullYear() === year);
        const dxNhap = dxYear.filter(d => d.trangThai === 'NHAP' || d.trangThai === 'DA_THU_HOI').length;
        const dxCho = dxYear.filter(d => d.trangThai === 'CHO_DUYET').length;
        const dxDuyet = dxYear.filter(d => d.trangThai === 'DA_DUYET').length;
        const dxTuChoi = dxYear.filter(d => d.trangThai === 'TU_CHOI').length;

        this.metDeXuat = { total: dxYear.length, nhap: dxNhap, choDuyet: dxCho, daDuyet: dxDuyet, tuChoi: dxTuChoi };

        this.deXuatPie = [
          { name: 'Nháp / Thu hồi', value: dxNhap },
          { name: 'Chờ duyệt', value: dxCho },
          { name: 'Đã duyệt', value: dxDuyet },
          { name: 'Từ chối', value: dxTuChoi },
        ].filter(x => x.value > 0);

        // Đề xuất theo tháng
        const dxByThang = new Map<string, number>();
        for (let m = 1; m <= 12; m++) dxByThang.set(`T${m}`, 0);
        dxYear.forEach(d => {
          const m = new Date(d.createdAt).getMonth() + 1;
          const key = `T${m}`;
          dxByThang.set(key, (dxByThang.get(key) ?? 0) + 1);
        });
        this.deXuatTheoThang = Array.from(dxByThang.entries()).map(([name, value]) => ({ name, value }));

        this.loading = false;
      },
      error: (err) => {
        console.error(err);
        this.errorMsg = 'Không thể tải dữ liệu báo cáo.';
        this.loading = false;
      }
    });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  private mapToChart(map: Map<string, number>): ChartItem[] {
    return Array.from(map.entries())
      .map(([name, value]) => ({ name, value }))
      .sort((a, b) => b.value - a.value);
  }

  formatCurrency(val: number): string {
    if (val >= 1_000_000_000) return `${(val / 1_000_000_000).toFixed(1)}B`;
    if (val >= 1_000_000) return `${(val / 1_000_000).toFixed(1)}M`;
    if (val >= 1_000) return `${(val / 1_000).toFixed(0)}K`;
    return val.toLocaleString('vi-VN');
  }

  formatFullCurrency(val: number): string {
    return val.toLocaleString('vi-VN', { style: 'currency', currency: 'VND' });
  }

  getPercent(part: number, total: number): string {
    if (!total) return '0%';
    return `${Math.round((part / total) * 100)}%`;
  }

  // ── Export PDF (html2canvas + jsPDF, đầy đủ nội dung, không cắt) ─────────────────
  async exportPDF(): Promise<void> {
    this.exporting = true;
    try {
      const html2canvasMod = await import('html2canvas');
      const html2canvas = (html2canvasMod as any).default ?? html2canvasMod;
      const jsPDFMod = await import('jspdf');
      const jsPDF = (jsPDFMod as any).default ?? (jsPDFMod as any).jsPDF ?? jsPDFMod;

      // A4 landscape: 297 × 210 mm, margin 10mm each side
      const PAGE_W = 297, PAGE_H = 210, MARGIN = 10;
      const usableW = PAGE_W - MARGIN * 2;   // 277 mm
      const usableH = PAGE_H - MARGIN * 2;   // 190 mm

      const pdf = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'a4' });
      const year = this.selectedYear;

      // Cover page: render HTML element thành ảnh (hỗ trợ tiếng Việt)
      const coverEl = document.getElementById('pdf-cover');
      if (coverEl) {
        coverEl.style.display = 'block';
        const coverCanvas = await html2canvas(coverEl, { scale: 2, backgroundColor: '#ffffff', logging: false });
        coverEl.style.display = 'none';
        const coverPxPerMm = coverCanvas.width / usableW;
        const coverH = coverCanvas.height / coverPxPerMm;
        const coverY = MARGIN + (usableH - coverH) / 2;
        pdf.addImage(coverCanvas.toDataURL('image/png'), 'PNG', MARGIN, Math.max(MARGIN, coverY), usableW, coverH);
      }

      const sections = [
        'section-nhan-su',
        'section-luong',
        'section-don-phep',
        'section-du-an',
        'section-de-xuat',
      ];

      for (const sectionId of sections) {
        const el = document.getElementById(sectionId);
        if (!el) continue;

        // Capture toàn bộ section
        const canvas = await html2canvas(el, {
          scale: 2,
          backgroundColor: '#ffffff',
          logging: false,
          useCORS: true,
          scrollX: 0,
          scrollY: 0,
          windowWidth: el.scrollWidth,
          windowHeight: el.scrollHeight,
        });

        // Tỉ lệ px → mm
        const pxPerMm = canvas.width / usableW;
        const imgH_mm = canvas.height / pxPerMm;

        pdf.addPage();

        if (imgH_mm <= usableH) {
          // Vừa một trang — căn giữa theo chiều dọc
          const yOff = MARGIN + (usableH - imgH_mm) / 2;
          pdf.addImage(canvas.toDataURL('image/png'), 'PNG', MARGIN, yOff, usableW, imgH_mm);
        } else {
          // Ảnh cao hơn một trang → cắt thành từng trang
          let drawnMm = 0;
          let isFirstSlice = true;
          while (drawnMm < imgH_mm) {
            const sliceH_mm = Math.min(imgH_mm - drawnMm, usableH);
            const srcY_px = Math.round(drawnMm * pxPerMm);
            const sliceH_px = Math.round(sliceH_mm * pxPerMm);

            const sliceCanvas = document.createElement('canvas');
            sliceCanvas.width = canvas.width;
            sliceCanvas.height = sliceH_px;
            sliceCanvas.getContext('2d')!
              .drawImage(canvas, 0, srcY_px, canvas.width, sliceH_px, 0, 0, canvas.width, sliceH_px);

            if (!isFirstSlice) pdf.addPage();
            pdf.addImage(sliceCanvas.toDataURL('image/png'), 'PNG', MARGIN, MARGIN, usableW, sliceH_mm);
            drawnMm += sliceH_mm;
            isFirstSlice = false;
          }
        }
      }

      // Xoá trang trắng đầu tiên (trang cover + trang đầu tiên = 2 trang, nhưng trang cover là trang 1)
      // Giữ trang 1 (cover), các section bắt đầu từ trang 2
      pdf.save(`BaoCao_NhanSu_${year}_${this.formatDateFile(new Date())}.pdf`);

    } catch (err) {
      console.error('Export PDF error:', err);
    } finally {
      this.exporting = false;
    }
  }

  // ── Export Excel (giữ lại nếu cần) — không dùng nữa ──────────────────────────────
  async exportExcel(): Promise<void> {
    this.exporting = true;
    try {
      const html2canvasMod = await import('html2canvas');
      const html2canvas = (html2canvasMod as any).default ?? html2canvasMod;
      const ExcelJS = await import('exceljs');

      const wb = new ExcelJS.Workbook();
      wb.creator = 'QLNS-ERP';
      wb.created = new Date();
      const year = this.selectedYear;

      const capture = async (id: string): Promise<string | null> => {
        const el = document.getElementById(id);
        if (!el) return null;
        const canvas = await html2canvas(el, { scale: 2, backgroundColor: '#ffffff', logging: false, useCORS: true });
        return canvas.toDataURL('image/png').replace('data:image/png;base64,', '');
      };

      const buildSheet = (ws: any, title: string, headers: string[], rows: (string | number)[][], imgBase64: string | null) => {
        const colCount = headers.length;
        const lastCol = String.fromCharCode(64 + colCount);
        ws.mergeCells(`A1:${lastCol}1`);
        const titleCell = ws.getCell('A1');
        titleCell.value = title;
        titleCell.font = { bold: true, size: 14, color: { argb: 'FFFFFFFF' } };
        titleCell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF1D4ED8' } };
        titleCell.alignment = { horizontal: 'center', vertical: 'middle' };
        ws.getRow(1).height = 30;
        ws.addRow([]);
        const headerRow = ws.addRow(headers);
        headerRow.eachCell((cell: any) => {
          cell.font = { bold: true, color: { argb: 'FFFFFFFF' } };
          cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FF475569' } };
          cell.alignment = { horizontal: 'center', vertical: 'middle' };
          cell.border = { bottom: { style: 'thin', color: { argb: 'FFE2E8F0' } } };
        });
        ws.getRow(3).height = 22;
        rows.forEach((r, i) => {
          const row = ws.addRow(r);
          if (i % 2 === 1) {
            row.eachCell((cell: any) => {
              cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF8FAFC' } };
            });
          }
          row.height = 20;
        });
        ws.columns = headers.map((_any: any, i: number) => ({ width: i === 0 ? 34 : 20 }));
        if (imgBase64) {
          const imgStartRow = rows.length + 6;
          for (let i = 0; i < imgStartRow + 28; i++) ws.addRow([]);
          const imgId = wb.addImage({ base64: imgBase64, extension: 'png' });
          ws.addImage(imgId, { tl: { col: 0, row: imgStartRow } as any, ext: { width: 900, height: 420 } });
        }
      };

      const [img1, img2, img3, img4, img5] = await Promise.all([
        capture('section-nhan-su'),
        capture('section-luong'),
        capture('section-don-phep'),
        capture('section-du-an'),
        capture('section-de-xuat'),
      ]);

      buildSheet(wb.addWorksheet('1. Nhan su'), `NHAN SU - NAM ${year}`, ['Chi tieu', 'So lieu'], [
        ['Tong nhan vien', this.metNhanSu.total],
        ['Dang lam viec', this.metNhanSu.dangLam],
        ['Nghi viec', this.metNhanSu.nghiViec],
        ['So phong ban', this.nhanSuByPhongBan.length],
        ...this.nhanSuByPhongBan.map((r: ChartItem) => ['   ' + r.name, r.value]),
      ], img1);

      buildSheet(wb.addWorksheet('2. Luong'), `BANG LUONG NAM ${year}`, ['Chi tieu', 'So lieu (VND)'], [
        ['Tong quy luong', this.metLuong.tongQuyLuong],
        ['Luong trung binh / bang', this.metLuong.luongTB],
        ['Tong chi OT', this.metLuong.tongOt],
        ['So bang luong da tao', this.metLuong.soNguoi],
        ...(this.luongTheoThang[0]?.series ?? []).map((r: ChartItem) => ['   ' + r.name, r.value]),
        ...(this.luongByPhongBan.length ? [['--- Luong TB theo PB ---', '']] : []),
        ...this.luongByPhongBan.map((r: ChartItem) => ['   ' + r.name, r.value]),
      ], img2);

      buildSheet(wb.addWorksheet('3. Don phep'), `DON NGHI PHEP NAM ${year}`, ['Chi tieu', 'So lieu', 'Ti le'], [
        ['Tong don xin phep', this.metDonPhep.total, ''],
        ['Cho duyet', this.metDonPhep.choDuyet, this.getPercent(this.metDonPhep.choDuyet, this.metDonPhep.total)],
        ['Da duyet', this.metDonPhep.daDuyet, this.getPercent(this.metDonPhep.daDuyet, this.metDonPhep.total)],
        ['Tu choi', this.metDonPhep.tuChoi, this.getPercent(this.metDonPhep.tuChoi, this.metDonPhep.total)],
        ...this.donPhepByLoai.map((r: ChartItem) => ['   ' + r.name, r.value, this.getPercent(r.value, this.metDonPhep.total)]),
      ], img3);

      buildSheet(wb.addWorksheet('4. Du an'), `DU AN`, ['Chi tieu', 'So lieu', 'Ti le'], [
        ['Tong du an', this.metDuAn.total, ''],
        ['Dang nhap lieu', this.metDuAn.dangNhap, this.getPercent(this.metDuAn.dangNhap, this.metDuAn.total)],
        ['Cho duyet GD', this.metDuAn.choDuyet, this.getPercent(this.metDuAn.choDuyet, this.metDuAn.total)],
        ['Da duyet', this.metDuAn.daDuyet, this.getPercent(this.metDuAn.daDuyet, this.metDuAn.total)],
        ['Tu choi', this.metDuAn.tuChoi, this.getPercent(this.metDuAn.tuChoi, this.metDuAn.total)],
      ], img4);

      buildSheet(wb.addWorksheet('5. De xuat GD'), `DE XUAT GIAM DOC NAM ${year}`, ['Chi tieu', 'So lieu', 'Ti le'], [
        ['Tong de xuat', this.metDeXuat.total, ''],
        ['Nhap / Thu hoi', this.metDeXuat.nhap, this.getPercent(this.metDeXuat.nhap, this.metDeXuat.total)],
        ['Cho duyet', this.metDeXuat.choDuyet, this.getPercent(this.metDeXuat.choDuyet, this.metDeXuat.total)],
        ['Da duyet', this.metDeXuat.daDuyet, this.getPercent(this.metDeXuat.daDuyet, this.metDeXuat.total)],
        ['Tu choi', this.metDeXuat.tuChoi, this.getPercent(this.metDeXuat.tuChoi, this.metDeXuat.total)],
        ...this.deXuatTheoThang.map((r: ChartItem) => ['   ' + r.name, r.value, '']),
      ], img5);

      const buffer = await wb.xlsx.writeBuffer();
      const blob = new Blob([buffer as ArrayBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `BaoCao_NhanSu_${year}_${this.formatDateFile(new Date())}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);

    } catch (err) {
      console.error('Export Excel error:', err);
    } finally {
      this.exporting = false;
    }
  }

  private formatDateFile(d: Date): string {
    const dd = String(d.getDate()).padStart(2, '0');
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const yy = d.getFullYear();
    return `${dd}${mm}${yy}`;
  }
}
