import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ChamCongApiService } from 'src/app/core/services/api/cham-cong-api.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { Subscription } from 'rxjs';
import {
  BangCongThangDetailDto,
  BangCongThangSummaryDto,
  ChamCongNgayDto,
  ChamCongConfigDto,
} from 'src/app/core/models/cham-cong.model';

interface CalendarDay {
  date: Date;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  attendance: ChamCongNgayDto[];
  hasAttendance: boolean;
  isWeekend: boolean;
}

interface AttendanceStats {
  totalDays: number;
  lateIn: number;
  earlyOut: number;
  noRecord: number;
}

@Component({
  selector: 'app-cham-cong',
  templateUrl: './cham-cong.component.html',
  styleUrls: ['./cham-cong.component.scss'],
})
export class ChamCongComponent implements OnInit, OnDestroy {
  loadingList = false;
  loadingDetail = false;
  saving = false;
  errorMsg = '';

  private signalrSubscription?: Subscription;

  yearCtrl = new FormControl<number>(new Date().getFullYear(), { nonNullable: true });
  selectedMonth: number = new Date().getMonth() + 1;
  selectedYear: number = new Date().getFullYear();

  // Available years (±4 from current)
  availableYears: number[] = [];
  // All 12 months
  allMonths = [
    { value: 1, label: 'Tháng 1' },
    { value: 2, label: 'Tháng 2' },
    { value: 3, label: 'Tháng 3' },
    { value: 4, label: 'Tháng 4' },
    { value: 5, label: 'Tháng 5' },
    { value: 6, label: 'Tháng 6' },
    { value: 7, label: 'Tháng 7' },
    { value: 8, label: 'Tháng 8' },
    { value: 9, label: 'Tháng 9' },
    { value: 10, label: 'Tháng 10' },
    { value: 11, label: 'Tháng 11' },
    { value: 12, label: 'Tháng 12' },
  ];

  months: BangCongThangSummaryDto[] = [];
  selectedMonthId: number | null = null;

  detail: BangCongThangDetailDto = {
    id: 0,
    thang: 0,
    nam: 0,
    trangThaiCong: '',
    ngayChotCong: null,
    taiKhoanChotId: null,
    tenNguoiChot: null,
    ngayCongs: [],
  };

  // Calendar view
  calendarDays: CalendarDay[] = [];
  weekDays = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
  stats: AttendanceStats = { totalDays: 0, lateIn: 0, earlyOut: 0, noRecord: 0 };

  // Day detail modal
  showDayDetail = false;
  selectedDayAttendance: ChamCongNgayDto[] = [];
  selectedDate: Date | null = null;

  // Config modal
  showConfigModal = false;
  config: ChamCongConfigDto = {
    gioVao: '08:00',
    gioRa: '17:00',
    lateGraceMinutes: 15,
    earlyLeaveGraceMinutes: 15
  };

  constructor(
    private api: ChamCongApiService,
    private signalrService: SignalrService,
    private toast: ToastService,
  ) { }

  ngOnInit(): void {
    // Generate ±4 years from current year
    const currentYear = new Date().getFullYear();
    for (let i = currentYear - 4; i <= currentYear + 4; i++) {
      this.availableYears.push(i);
    }

    this.selectedYear = currentYear;
    this.selectedMonth = new Date().getMonth() + 1;

    this.loadMonths();

    // Subscribe to SignalR entity updates for realtime attendance changes
    this.signalrSubscription = this.signalrService.entityUpdate$.subscribe((update) => {
      if (update.entityType === 'ChamCong' && this.selectedMonthId) {
        console.log('[ChamCong] Realtime update received:', update);
        // Reload calendar data when attendance records change
        this.loadDetail();
      }
    });
  }

  ngOnDestroy(): void {
    // Clean up SignalR subscription
    this.signalrSubscription?.unsubscribe();
  }

  loadMonths(): void {
    const year = +this.yearCtrl.value; // Ép kiểu number (select trả về string)
    this.loadingList = true;
    this.errorMsg = '';

    this.api.getBangCongThang(year).subscribe({
      next: (res) => {
        this.months = Array.isArray(res) ? res : [];
        this.loadingList = false;

        // Auto load detail for selected month
        this.loadDetailForSelectedMonth();
      },
      error: (_err) => {
        this.errorMsg = 'Không tải được danh sách bảng công';
        this.loadingList = false;
      },
    });
  }

  onMonthChange(): void {
    // HTML select [value] trả string → ép kiểu number
    this.selectedMonth = +this.selectedMonth;
    this.loadDetailForSelectedMonth();
  }

  onYearChange(): void {
    // HTML select [value] trả string → ép kiểu number
    this.selectedYear = +this.yearCtrl.value;
    this.loadMonths();
  }

  loadDetailForSelectedMonth(): void {
    // Ép kiểu number rõ ràng cho an toàn (HTML select trả string)
    const month = +this.selectedMonth;
    const year = +this.selectedYear;

    // Find BangCongThangId for selected month and year
    const found = this.months.find(
      (m) => m.thang === month && m.nam === year
    );

    if (found) {
      this.selectedMonthId = found.id;
      this.loadDetail();
    } else {
      // No record found, clear calendar
      this.selectedMonthId = null;
      this.detail = {
        id: 0,
        thang: this.selectedMonth,
        nam: this.selectedYear,
        trangThaiCong: '',
        ngayChotCong: null,
        taiKhoanChotId: null,
        tenNguoiChot: null,
        ngayCongs: [],
      };
      this.buildCalendar();
      this.calculateStats();
    }
  }

  loadDetail(): void {
    if (!this.selectedMonthId) return;

    this.loadingDetail = true;
    this.errorMsg = '';

    this.api.getBangCongDetail(this.selectedMonthId).subscribe({
      next: (res) => {
        this.detail = res;
        this.buildCalendar();
        this.calculateStats();
        this.loadingDetail = false;
      },
      error: (_err) => {
        this.errorMsg = 'Không tải được chi tiết bảng công';
        this.loadingDetail = false;
      },
    });
  }

  // Config Modal Logic
  openConfig(): void {
    this.loadingDetail = true;
    this.api.getConfig().subscribe({
      next: (res) => {
        this.config = res;
        this.showConfigModal = true;
        this.loadingDetail = false;
      },
      error: () => {
        this.errorMsg = 'Không tải được cấu hình hệ thống';
        this.loadingDetail = false;
      }
    });
  }

  closeConfig(): void {
    this.showConfigModal = false;
  }

  saveConfig(): void {
    this.saving = true;
    this.api.updateConfig(this.config).subscribe({
      next: () => {
        this.saving = false;
        this.showConfigModal = false;
        // Optionally show success toast
        this.loadDetail(); // Reload if needed
      },
      error: () => {
        this.errorMsg = 'Lưu cấu hình thất bại';
        this.saving = false;
      }
    });
  }


  buildCalendar(): void {
    const year = this.detail.nam;
    const month = this.detail.thang - 1; // JS months are 0-based

    const firstDayOfMonth = new Date(year, month, 1);
    const lastDayOfMonth = new Date(year, month + 1, 0);

    // Start from Sunday of the week containing the 1st
    const startDate = new Date(firstDayOfMonth);
    const dayOfWeek = startDate.getDay(); // 0=Sunday, 1=Monday, etc.
    startDate.setDate(startDate.getDate() - dayOfWeek);

    // End on Saturday of the week containing the last day
    const endDate = new Date(lastDayOfMonth);
    const lastDayOfWeek = endDate.getDay();
    endDate.setDate(endDate.getDate() + (6 - lastDayOfWeek));

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    this.calendarDays = [];
    const currentDate = new Date(startDate);

    while (currentDate <= endDate) {
      const dateStr = this.formatDateISO(currentDate);
      const attendance =
        this.detail.ngayCongs?.filter(
          (ng) => ng.ngay && this.formatDateISO(new Date(ng.ngay)) === dateStr
        ) || [];

      const day: CalendarDay = {
        date: new Date(currentDate),
        dayNumber: currentDate.getDate(),
        isCurrentMonth: currentDate.getMonth() === month,
        isToday: currentDate.getTime() === today.getTime(),
        attendance: attendance,
        hasAttendance: attendance.length > 0,
        isWeekend: currentDate.getDay() === 0 || currentDate.getDay() === 6,
      };

      this.calendarDays.push(day);
      currentDate.setDate(currentDate.getDate() + 1);
    }
  }

  formatDateISO(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  calculateStats(): void {
    const uniqueDates = new Set<string>();
    let late = 0;
    let early = 0;
    let noRecord = 0;

    this.detail.ngayCongs?.forEach((ng) => {
      if (ng.ngay) {
        const dateStr = this.formatDateISO(new Date(ng.ngay));
        uniqueDates.add(dateStr);
      }

      if (ng.trangThai === 'LATE_IN' || ng.trangThai === 'TRE') late++;
      if (ng.trangThai === 'EARLY_OUT') early++;
      if (ng.trangThai === 'NO_RECORD' || ng.trangThai === 'ABSENT' || ng.trangThai === 'NGHI') noRecord++;
    });

    this.stats = {
      totalDays: uniqueDates.size,
      lateIn: late,
      earlyOut: early,
      noRecord: noRecord,
    };
  }

  onDayClick(day: CalendarDay): void {
    if (!day.isCurrentMonth || !day.hasAttendance) return;

    this.selectedDate = day.date;
    this.selectedDayAttendance = day.attendance || [];
    this.showDayDetail = true;
  }

  closeDayDetail(): void {
    this.showDayDetail = false;
    this.selectedDayAttendance = [];
    this.selectedDate = null;
  }

  previousMonth(): void {
    this.selectedMonth--;
    if (this.selectedMonth < 1) {
      this.selectedMonth = 12;
      this.selectedYear--;
      this.yearCtrl.setValue(this.selectedYear);
    } else {
      this.loadDetailForSelectedMonth();
    }
  }

  nextMonth(): void {
    this.selectedMonth++;
    if (this.selectedMonth > 12) {
      this.selectedMonth = 1;
      this.selectedYear++;
      this.yearCtrl.setValue(this.selectedYear);
    } else {
      this.loadDetailForSelectedMonth();
    }
  }

  canGoBack(): boolean {
    return true; // Always allow navigation
  }

  canGoNext(): boolean {
    return true; // Always allow navigation
  }

  isLocked(): boolean {
    if (!this.selectedMonthId) return false;
    return this.detail.trangThaiCong === 'DA_CHOT_CONG';
  }

  getMonthStatus(): string {
    const found = this.months.find(
      (m) => m.thang === this.selectedMonth && m.nam === this.selectedYear
    );
    return found?.trangThaiCong === 'DA_CHOT_CONG' ? 'Đã chốt' : 'Chưa chốt';
  }

  lockToggle(lock: boolean): void {
    if (!this.selectedMonthId) return;

    const action = lock ? 'chốt công' : 'mở chốt công';
    this.loadingDetail = true;
    this.errorMsg = '';

    this.api.lockBangCong({ bangCongThangId: this.selectedMonthId, lock }).subscribe({
      next: () => {
        // Reload cả danh sách tháng và chi tiết
        this.loadMonthsAndKeepSelection();
      },
      error: (err) => {
        const errorMessage = err?.error?.message || err?.error || err?.statusText;
        this.errorMsg = errorMessage || `Không thể ${action}`;
        this.loadingDetail = false;
      },
    });
  }

  private loadMonthsAndKeepSelection(): void {
    const currentMonthId = this.selectedMonthId;

    this.api.getBangCongThang(this.yearCtrl.value).subscribe({
      next: (res) => {
        this.months = Array.isArray(res) ? res : [];
        // Keep selection and reload detail
        if (currentMonthId) {
          this.selectedMonthId = currentMonthId;
          this.loadDetail();
        } else {
          this.loadingDetail = false;
        }
      },
      error: (_err) => {
        this.errorMsg = 'Không tải được danh sách bảng công';
        this.loadingDetail = false;
      },
    });
  }

  formatDateDisplay(dateStr: string | null | undefined): string {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    const day = date.getDate().toString().padStart(2, '0');
    const month = (date.getMonth() + 1).toString().padStart(2, '0');
    const year = date.getFullYear();
    return `${day}/${month}/${year}`;
  }

  formatTime(timeStr: string | null | undefined): string {
    if (!timeStr) return '—';
    // If ISO string (2025-12-01T08:00:00), extract HH:mm
    if (timeStr.includes('T')) {
      const timePart = timeStr.split('T')[1];
      return timePart.substring(0, 5); // HH:mm
    }
    // If already HH:mm format
    return timeStr;
  }

  statusText(st: string | null | undefined): string {
    if (!st) return '—';
    switch (st) {
      case 'DI_LAM':
      case 'PRESENT':
        return 'Đi làm';
      case 'LATE_IN':
      case 'TRE':
        return 'Trễ';
      case 'EARLY_OUT':
        return 'Về sớm';
      case 'NGHI':
      case 'ABSENT':
        return 'Nghỉ';
      case 'NGHI_PHEP':
        return 'Nghỉ phép';
      case 'NO_RECORD':
        return 'Chưa chấm';
      default:
        return st;
    }
  }

  statusBadgeClass(st: string | null | undefined): string {
    if (!st) return 'bg-secondary-subtle text-secondary';
    switch (st) {
      case 'DI_LAM':
      case 'PRESENT':
        return 'bg-success-subtle text-success';
      case 'LATE_IN':
      case 'TRE':
        return 'bg-warning-subtle text-warning';
      case 'EARLY_OUT':
        return 'bg-info-subtle text-info';
      case 'NGHI':
      case 'ABSENT':
        return 'bg-danger-subtle text-danger';
      case 'NGHI_PHEP':
        return 'bg-primary-subtle text-primary';
      case 'NO_RECORD':
        return 'bg-secondary-subtle text-secondary';
      default:
        return 'bg-secondary-subtle text-secondary';
    }
  }

  // ============================================================
  // EDIT & DELETE ATTENDANCE RECORDS
  // ============================================================
  editingAttendance: ChamCongNgayDto | null = null;
  showEditModal = false;
  editForm = {
    gioVao: '',
    gioRa: '',
    soGioOt: 0,
    trangThai: '',
    ghiChu: ''
  };

  editAttendance(record: ChamCongNgayDto): void {
    if (this.isLocked()) {
      alert('Bảng công đã chốt, không thể sửa!');
      return;
    }

    this.editingAttendance = record;
    this.editForm = {
      gioVao: record.gioVao || '',
      gioRa: record.gioRa || '',
      soGioOt: record.soGioOt || 0,
      trangThai: record.trangThai || '',
      ghiChu: record.ghiChu || ''
    };
    this.showEditModal = true;
  }

  closeEditModal(): void {
    this.showEditModal = false;
    this.editingAttendance = null;
  }

  saveEdit(): void {
    if (!this.editingAttendance) return;

    this.saving = true;
    this.errorMsg = '';

    const dto = {
      gioVao: this.editForm.gioVao || null,
      gioRa: this.editForm.gioRa || null,
      soGioOt: this.editForm.soGioOt,
      trangThai: this.editForm.trangThai,
      ghiChu: this.editForm.ghiChu || null
    };

    this.api.updateChamCong(this.editingAttendance.id, dto).subscribe({
      next: () => {
        this.saving = false;
        const hoTen = this.editingAttendance?.hoTen || 'nhân viên';
        this.toast.success(`Đã cập nhật chấm công của ${hoTen} thành công`);
        this.closeEditModal();
        // Reload calendar to reflect changes
        this.loadDetail();
        // Update the day detail modal data
        const dayAttendance = this.calendarDays.find(
          d => d.date.getTime() === this.selectedDate?.getTime()
        );
        if (dayAttendance) {
          this.selectedDayAttendance = dayAttendance.attendance;
        }
      },
      error: (err) => {
        const errorMessage = err?.error?.message || err?.error || err?.statusText;
        this.errorMsg = errorMessage || 'Không thể cập nhật chấm công';
        this.toast.danger(this.errorMsg);
        this.saving = false;
      }
    });
  }

  deleteAttendance(record: ChamCongNgayDto): void {
    if (this.isLocked()) {
      alert('Bảng công đã chốt, không thể xóa!');
      return;
    }

    if (!confirm(`Xác nhận xóa chấm công của ${record.hoTen} ngày ${this.formatDateDisplay(record.ngay)}?`)) {
      return;
    }

    this.saving = true;
    this.errorMsg = '';

    this.api.deleteChamCong(record.id).subscribe({
      next: () => {
        this.saving = false;
        // Remove from day detail modal
        this.selectedDayAttendance = this.selectedDayAttendance.filter(a => a.id !== record.id);
        // Reload calendar to reflect changes
        this.loadDetail();
      },
      error: (err) => {
        const errorMessage = err?.error?.message || err?.error || err?.statusText;
        this.errorMsg = errorMessage || 'Không thể xóa chấm công';
        this.saving = false;
        alert(this.errorMsg);
      }
    });
  }
}
