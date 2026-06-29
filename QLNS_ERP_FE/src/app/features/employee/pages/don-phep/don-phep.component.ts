import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { DonPhepApiService } from 'src/app/core/services/api/don-phep-api.service';
import {
  DonPhepDetailDto,
  DonPhepListItemDto,
  DonPhepThongKeVm,
  DonPhepEmployeeUpdateDto
} from 'src/app/core/models/don-phep.model';
import { ToastService } from 'src/app/shared/services/toast.service';
import { MeApiService } from 'src/app/core/services/api/me-api.service';
import { LoaiPhepService, LoaiPhep } from 'src/app/core/services/api/loai-phep.service';
import { ThongBaoApiService } from 'src/app/core/services/api/thong-bao-api.service';
import { SignalrService } from 'src/app/core/services/signalr.service';

@Component({
  selector: 'app-don-phep',
  templateUrl: './don-phep.component.html',
  styleUrls: ['./don-phep.component.scss']
})
export class DonPhepComponent implements OnInit, OnDestroy {
  loadingList = false;
  loadingCreate = false;
  detailLoading = false;
  errorMsg = '';

  // list
  all: DonPhepListItemDto[] = [];
  list: DonPhepListItemDto[] = [];
  searchCtrl = new FormControl<string>('', { nonNullable: true });
  statusCtrl = new FormControl<string>('ALL', { nonNullable: true });

  // stats FE (BE chưa có tổng hợp)
  thongKe: DonPhepThongKeVm = { tongPhepNam: 12, daSuDung: 0, conLai: 12 };

  // Animation for stats numbers
  animatingStats = false;
  displayTongPhepNam = 0;
  displayDaSuDung = 0;
  displayConLai = 0;

  // create form
  form!: FormGroup;
  loaiPhepList: LoaiPhep[] = [];
  loadingLoaiPhep = false;

  // detail (drawer)
  showDetail = false;
  selectedDetail: DonPhepDetailDto | null = null;

  // create modal
  showCreateModal = false;
  dateRangeError = '';
  editMode = false;
  editingId: number | null = null;

  // ID nhân viên (lấy từ API profile)
  private nvHoSoId: number | null = null;
  
  // SignalR subscription
  private entityUpdateSub?: Subscription;

  constructor(
    private api: DonPhepApiService,
    private fb: FormBuilder,
    private toast: ToastService,
    private meApi: MeApiService,
    private loaiPhepService: LoaiPhepService,
    private signalrService: SignalrService,
    private thongBaoApi: ThongBaoApiService
  ) { }

  ngOnInit(): void {
    this.form = this.fb.group({
      loaiPhepId: ['', Validators.required],
      tuNgay: ['', Validators.required],
      denNgay: ['', Validators.required],
      lyDo: ['', [Validators.required, Validators.maxLength(500)]],
    });

    this.searchCtrl.valueChanges.subscribe(() => this.applyFilter());
    this.statusCtrl.valueChanges.subscribe(() => this.applyFilter());

    // Initialize display values
    this.displayTongPhepNam = this.thongKe.tongPhepNam;
    this.displayDaSuDung = this.thongKe.daSuDung;
    this.displayConLai = this.thongKe.conLai;

    // Lấy nvHoSoId từ profile API trước khi load dữ liệu
    this.loadEmployeeId();

    // Load danh sách loại phép từ BE
    this.loadLoaiPhep();
    
    // Mark DON_PHEP notifications as read when user enters this page
    this.markRelatedNotificationsAsRead();
    
    // Subscribe to realtime entity updates
    this.entityUpdateSub = this.signalrService.entityUpdate$.subscribe(update => {
      if (update.entityType === 'DON_PHEP') {
        console.log('[DonPhep] Realtime update received:', update);
        // Reload list when any DON_PHEP is updated (approved/rejected)
        this.load();
        this.toast.info(`Đơn phép #${update.entityId} đã được ${update.action === 'APPROVED' ? 'duyệt' : 'từ chối'}`);
      }
    });
  }
  
  ngOnDestroy(): void {
    this.entityUpdateSub?.unsubscribe();
  }

  load(): void {
    this.loadingList = true;
    this.errorMsg = '';

    this.api.getList().subscribe({
      next: (res) => {
        let arr = Array.isArray(res) ? res : [];

        // Lọc danh sách theo nvHoSoId của nhân viên
        if (this.nvHoSoId != null) {
          arr = arr.filter(x => x.nvHoSoId === this.nvHoSoId);
        }

        this.all = arr;
        this.applyFilter();
        this.recalcThongKe();
        this.loadingList = false;
        this.toast.success('Đã làm mới danh sách đơn nghỉ phép!');
      },
      error: () => {
        this.errorMsg = 'Không tải được danh sách đơn nghỉ phép.';
        this.loadingList = false;
        this.toast.danger('Không thể tải danh sách. Vui lòng thử lại!');
      }
    });
  }

  applyFilter(): void {
    const q = (this.searchCtrl.value || '').trim().toLowerCase();
    const st = this.statusCtrl.value;

    this.list = this.all
      .filter(x => st === 'ALL' ? true : x.trangThai === st)
      .filter(x => {
        if (!q) return true;
        const hay = `${x.id} ${x.tenLoaiPhep} ${x.trangThai} ${x.tuNgay} ${x.denNgay}`.toLowerCase();
        return hay.includes(q);
      })
      .sort((a, b) => b.id - a.id);

    this.recalcThongKe();
  }

  private recalcThongKe(): void {
    const year = new Date().getFullYear();

    /**
     * LƯU Ý: Số ngày (soNgay) được tính từ backend
     * Backend tính: soNgay = (denNgay - tuNgay).Days + 1
     * VD: 24/12 -> 25/12 = 2 ngày (bao gồm cả 2 đầu)
     * Đây là cách tính đúng cho nghỉ phép (tính cả ngày bắt đầu và kết thúc)
     */
    const daSuDung = this.all
      .filter(x => x.trangThai === 'DA_DUYET')
      .filter(x => new Date(x.tuNgay).getFullYear() === year)
      .reduce((s, x) => s + (Number(x.soNgay) || 0), 0);

    const tong = this.thongKe.tongPhepNam;
    this.thongKe = {
      tongPhepNam: tong,
      daSuDung,
      conLai: Math.max(0, tong - daSuDung)
    };

    // Animate numbers on update
    this.animateNumbers();
  }

  private animateNumbers(): void {
    this.animatingStats = true;

    const duration = 600; // ms
    const steps = 30;
    const interval = duration / steps;

    const startTongPhep = this.displayTongPhepNam;
    const startDaSuDung = this.displayDaSuDung;
    const startConLai = this.displayConLai;

    const endTongPhep = this.thongKe.tongPhepNam;
    const endDaSuDung = this.thongKe.daSuDung;
    const endConLai = this.thongKe.conLai;

    const diffTongPhep = endTongPhep - startTongPhep;
    const diffDaSuDung = endDaSuDung - startDaSuDung;
    const diffConLai = endConLai - startConLai;

    let currentStep = 0;

    const timer = setInterval(() => {
      currentStep++;
      const progress = currentStep / steps;

      this.displayTongPhepNam = Math.round(startTongPhep + (diffTongPhep * progress));
      this.displayDaSuDung = Math.round(startDaSuDung + (diffDaSuDung * progress));
      this.displayConLai = Math.round(startConLai + (diffConLai * progress));

      if (currentStep >= steps) {
        clearInterval(timer);
        this.displayTongPhepNam = endTongPhep;
        this.displayDaSuDung = endDaSuDung;
        this.displayConLai = endConLai;
        this.animatingStats = false;
      }
    }, interval);
  }

  usedPercent(): number {
    const tong = this.thongKe.tongPhepNam || 1;
    const p = (this.thongKe.daSuDung / tong) * 100;
    return Math.max(0, Math.min(100, p));
  }

  submit(): void {
    this.errorMsg = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    if (this.nvHoSoId == null && !this.editMode) {
      this.errorMsg = 'Không xác định được ID nhân viên. Vui lòng tải lại trang.';
      this.toast.danger('Không xác định được thông tin nhân viên. Vui lòng tải lại trang!');
      return;
    }

    // ============================================================
    // VALIDATION: Kiểm tra loaiPhepId có tồn tại trong danh sách không
    // ============================================================
    const selectedLoaiPhepId = Number(this.form.value.loaiPhepId);
    if (!selectedLoaiPhepId || selectedLoaiPhepId <= 0) {
      this.errorMsg = 'Vui lòng chọn loại phép!';
      this.toast.danger(this.errorMsg);
      return;
    }

    const loaiPhepExists = this.loaiPhepList.some(lp => lp.id === selectedLoaiPhepId);
    if (!loaiPhepExists) {
      this.errorMsg = `Loại phép (ID: ${selectedLoaiPhepId}) không tồn tại. Vui lòng chọn loại phép hợp lệ!`;
      this.toast.danger(this.errorMsg);
      console.error('❌ Invalid loaiPhepId:', {
        selectedId: selectedLoaiPhepId,
        availableIds: this.loaiPhepList.map(lp => lp.id)
      });
      return;
    }

    // Kiểm tra lần cuối trước khi submit
    this.validateDateRange();
    if (this.dateRangeError) {
      return;
    }

    this.loadingCreate = true;

    if (this.editMode && this.editingId) {
      // Update mode - sử dụng endpoint employee mới
      if (this.nvHoSoId === null) {
        this.errorMsg = 'Không xác định được ID nhân viên.';
        this.loadingCreate = false;
        this.toast.danger('Không thể cập nhật đơn. Vui lòng tải lại trang!');
        return;
      }

      const updatePayload: DonPhepEmployeeUpdateDto = {
        nvHoSoId: this.nvHoSoId,
        loaiPhepId: Number(this.form.value.loaiPhepId),
        tuNgay: this.form.value.tuNgay,
        denNgay: this.form.value.denNgay,
        lyDo: String(this.form.value.lyDo || '').trim()
      };

      console.log('🔄 Update payload:', updatePayload);

      this.api.updateByEmployee(this.editingId, updatePayload).subscribe({
        next: () => {
          this.form.reset();
          this.loadingCreate = false;
          this.showCreateModal = false;
          this.editMode = false;
          this.editingId = null;
          this.load();
          this.closeDetail();
          this.toast.success('Cập nhật đơn xin nghỉ phép thành công!');
        },
        error: (err) => {
          console.error('❌ Update error:', err);
          console.error('❌ Error details:', {
            status: err.status,
            message: err.error?.message || err.message,
            error: err.error,
            fullError: err
          });

          // Xử lý message lỗi
          let msg = 'Cập nhật đơn thất bại.';

          if (err.error && typeof err.error === 'string') {
            msg = err.error;
          } else if (err.error?.message) {
            msg = err.error.message;
          } else if (err.message) {
            msg = err.message;
          }

          // Làm rõ một số lỗi phổ biến
          if (msg.includes('trùng')) {
            msg = '⚠️ ' + msg + ' Vui lòng chọn khoảng thời gian khác.';
          } else if (msg.includes('quá khứ') || msg.includes('lùi quá')) {
            msg = '📅 ' + msg;
          } else if (msg.includes('tương lai')) {
            msg = '📅 ' + msg;
          } else if (msg.includes('vượt quá')) {
            msg = '⏱️ ' + msg;
          }

          this.errorMsg = msg;
          this.loadingCreate = false;
          this.toast.danger(msg);
        }
      });
    } else {
      // Create mode
      if (this.nvHoSoId === null) {
        this.errorMsg = 'Không xác định được ID nhân viên.';
        this.loadingCreate = false;
        this.toast.danger('Không thể tạo đơn. Vui lòng tải lại trang!');
        return;
      }

      this.api.create({
        nvHoSoId: this.nvHoSoId,
        loaiPhepId: Number(this.form.value.loaiPhepId),
        tuNgay: this.form.value.tuNgay,
        denNgay: this.form.value.denNgay,
        lyDo: String(this.form.value.lyDo || '').trim()
      }).subscribe({
        next: () => {
          this.form.reset();
          this.loadingCreate = false;
          this.showCreateModal = false;
          this.load();
          this.toast.success('Gửi đơn xin nghỉ phép thành công!');
        },
        error: (err) => {
          console.error('❌ Create error:', err);

          // Xử lý message lỗi
          let msg = 'Gửi đơn thất bại.';

          if (err.error && typeof err.error === 'string') {
            msg = err.error;
          } else if (err.error?.message) {
            msg = err.error.message;
          } else if (err.message) {
            msg = err.message;
          }

          // Làm rõ một số lỗi phổ biến
          if (msg.includes('trùng')) {
            msg = '⚠️ ' + msg + ' Vui lòng chọn khoảng thời gian khác.';
          } else if (msg.includes('quá khứ') || msg.includes('lùi quá')) {
            msg = '📅 ' + msg;
          } else if (msg.includes('tương lai')) {
            msg = '📅 ' + msg;
          } else if (msg.includes('vượt quá')) {
            msg = '⏱️ ' + msg;
          }

          this.errorMsg = msg;
          this.loadingCreate = false;
          this.toast.danger(msg);
        }
      });
    }
  }

  openDetail(id: number): void {
    this.showDetail = true;
    this.detailLoading = true;
    this.selectedDetail = null;

    this.api.getDetail(id).subscribe({
      next: (res) => {
        this.selectedDetail = res;
        this.detailLoading = false;
      },
      error: () => {
        this.errorMsg = 'Không tải được chi tiết đơn.';
        this.detailLoading = false;
      }
    });
  }

  closeDetail(): void {
    this.showDetail = false;
    this.selectedDetail = null;
  }

  openCreateModal(): void {
    this.showCreateModal = true;
    this.form.reset();
    this.dateRangeError = '';
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
    this.form.reset();
    this.dateRangeError = '';
    this.editMode = false;
    this.editingId = null;
  }

  validateDateRange(): void {
    const tuNgay = this.form.get('tuNgay')?.value;
    const denNgay = this.form.get('denNgay')?.value;

    if (!tuNgay || !denNgay) {
      this.dateRangeError = '';
      this.form.get('tuNgay')?.setErrors(null);
      this.form.get('denNgay')?.setErrors(null);
      return;
    }

    const tu = new Date(tuNgay);
    const den = new Date(denNgay);

    // Kiểm tra ngày bắt đầu <= ngày kết thúc
    if (tu > den) {
      this.dateRangeError = 'Vui lòng chọn khoảng thời gian hợp lệ. Ngày bắt đầu phải trước ngày kết thúc.';
      this.form.get('tuNgay')?.setErrors({ invalidRange: true });
      this.form.get('denNgay')?.setErrors({ invalidRange: true });
      return;
    }

    // Kiểm tra trùng lặp với các đơn khác (chỉ warning, không block)
    const hasOverlap = this.checkDateOverlap(tu, den);
    if (hasOverlap) {
      this.dateRangeError = 'Cảnh báo: Khoảng thời gian này có thể trùng với đơn khác. Vui lòng kiểm tra lại.';
    } else {
      this.dateRangeError = '';
    }

    // Clear errors nếu chỉ có invalidRange
    const tuErrors = this.form.get('tuNgay')?.errors;
    const denErrors = this.form.get('denNgay')?.errors;

    if (tuErrors && tuErrors['invalidRange']) {
      this.form.get('tuNgay')?.setErrors(null);
    }
    if (denErrors && denErrors['invalidRange']) {
      this.form.get('denNgay')?.setErrors(null);
    }
  }

  /**
   * Kiểm tra xem khoảng thời gian có trùng với đơn khác không
   * (loại trừ đơn đang sửa nếu ở edit mode)
   */
  private checkDateOverlap(tuNgay: Date, denNgay: Date): boolean {
    tuNgay.setHours(0, 0, 0, 0);
    denNgay.setHours(0, 0, 0, 0);

    return this.all.some(don => {
      // Loại trừ đơn đang sửa
      if (this.editMode && this.editingId === don.id) {
        return false;
      }

      // Chỉ check với đơn chờ duyệt hoặc đã duyệt
      if (don.trangThai === 'TU_CHOI') {
        return false;
      }

      const donTu = new Date(don.tuNgay);
      const donDen = new Date(don.denNgay);
      donTu.setHours(0, 0, 0, 0);
      donDen.setHours(0, 0, 0, 0);

      // Check overlap: (donTu <= denNgay) AND (donDen >= tuNgay)
      return donTu <= denNgay && donDen >= tuNgay;
    });
  }

  editLeaveRequest(): void {
    if (!this.selectedDetail) return;

    // Chỉ cho phép sửa khi đang chờ duyệt
    if (this.selectedDetail.trangThai !== 'CHO_DUYET') {
      this.toast.warning('Chỉ được sửa đơn khi đang chờ duyệt!');
      return;
    }

    // Chặn nếu đã có thông tin duyệt (phòng trường hợp trạng thái chưa sync)
    if (this.selectedDetail.ngayDuyet || this.selectedDetail.nguoiDuyetId) {
      this.toast.warning('Đơn phép đã được xử lý trước đó!');
      return;
    }

    this.editMode = true;
    this.editingId = this.selectedDetail.id;

    // Convert ISO date (yyyy-MM-ddTHH:mm:ss) -> yyyy-MM-dd cho input[type="date"]
    const tuNgayValue = this.formatIsoToInputDate(this.selectedDetail.tuNgay);
    const denNgayValue = this.formatIsoToInputDate(this.selectedDetail.denNgay);

    console.log('📝 Edit mode - Date conversion:', {
      original: {
        tuNgay: this.selectedDetail.tuNgay,
        denNgay: this.selectedDetail.denNgay
      },
      converted: {
        tuNgay: tuNgayValue,
        denNgay: denNgayValue
      }
    });

    this.form.patchValue({
      loaiPhepId: this.selectedDetail.loaiPhepId,
      tuNgay: tuNgayValue,
      denNgay: denNgayValue,
      lyDo: this.selectedDetail.lyDo
    });
    this.showCreateModal = true;
    // Đóng drawer để tránh chồng layout
    this.showDetail = false;
  }

  /**
   * Convert ISO date string hoặc yyyy-MM-dd -> yyyy-MM-dd cho input[type="date"]
   * VD: "2024-12-25T00:00:00" -> "2024-12-25"
   * 
   * LƯU Ý QUAN TRỌNG:
   * - Input type="date" LUÔN yêu cầu format yyyy-MM-dd cho value
   * - Nhưng HIỂN THỊ theo locale của browser (dd/mm/yyyy ở VN, mm/dd/yyyy ở US)
   * - Đây KHÔNG phải lỗi, mà là cách hoạt động của HTML5 date input
   * - Người dùng sẽ thấy dd/mm/yyyy khi chọn ngày (theo cài đặt browser)
   * - Nhưng giá trị gửi lên server vẫn là yyyy-MM-dd (đúng chuẩn ISO 8601)
   */
  private formatIsoToInputDate(isoDateString: string): string {
    if (!isoDateString) return '';

    // Nếu đã là yyyy-MM-dd thì giữ nguyên
    if (/^\d{4}-\d{2}-\d{2}$/.test(isoDateString)) {
      return isoDateString;
    }

    // Nếu là ISO string, lấy phần trước 'T'
    if (isoDateString.includes('T')) {
      return isoDateString.split('T')[0];
    }

    // Thử parse và format lại
    try {
      const date = new Date(isoDateString);
      if (!isNaN(date.getTime())) {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
      }
    } catch (e) {
      console.error('Error parsing date:', e);
    }

    return isoDateString;
  }

  deleteLeaveRequest(): void {
    if (!this.selectedDetail) return;

    // Chỉ cho phép xóa khi đang chờ duyệt
    if (this.selectedDetail.trangThai !== 'CHO_DUYET') {
      this.toast.warning('Chỉ được xóa khi đơn đang chờ duyệt!');
      return;
    }

    // Chặn nếu đã có thông tin duyệt
    if (this.selectedDetail.ngayDuyet || this.selectedDetail.nguoiDuyetId) {
      this.toast.warning('Đơn phép đã được xử lý trước đó!');
      return;
    }

    // Kiểm tra ngày bắt đầu đã qua hay chưa
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tuNgay = new Date(this.selectedDetail.tuNgay);
    tuNgay.setHours(0, 0, 0, 0);

    if (tuNgay < today) {
      this.toast.danger('Không được xóa đơn đã bắt đầu hoặc trong quá khứ!');
      return;
    }

    if (!confirm('Bạn có chắc chắn muốn xóa đơn nghỉ phép này không?')) {
      return;
    }

    if (this.nvHoSoId === null) {
      this.toast.danger('Không xác định được thông tin nhân viên!');
      return;
    }

    console.log('🗑️ Delete request:', {
      donPhepId: this.selectedDetail.id,
      nvHoSoId: this.nvHoSoId
    });

    this.api.deleteByEmployee(this.selectedDetail.id, this.nvHoSoId).subscribe({
      next: () => {
        this.toast.success('Xóa đơn nghỉ phép thành công!');
        this.closeDetail();
        this.load();
      },
      error: (err) => {
        console.error('❌ Delete error:', err);
        console.error('❌ Error details:', {
          status: err.status,
          message: err.error?.message || err.message,
          error: err.error,
          fullError: err
        });
        const msg = err?.error?.message || err?.message || 'Không thể xóa đơn. Vui lòng thử lại!';
        this.toast.danger(msg);
      }
    });
  }

  statusText(st: string): string {
    switch (st) {
      case 'CHO_DUYET': return 'Chờ duyệt';
      case 'DA_DUYET': return 'Đã duyệt';
      case 'TU_CHOI': return 'Từ chối';
      default: return st || '-';
    }
  }

  badgeClass(st: string): string {
    switch (st) {
      case 'CHO_DUYET': return 'text-bg-warning';
      case 'DA_DUYET': return 'text-bg-success';
      case 'TU_CHOI': return 'text-bg-danger';
      default: return 'text-bg-secondary';
    }
  }

  getStatusIcon(st: string): string {
    switch (st) {
      case 'CHO_DUYET': return 'bi-clock-history';
      case 'DA_DUYET': return 'bi-check-circle-fill';
      case 'TU_CHOI': return 'bi-x-circle-fill';
      default: return 'bi-question-circle-fill';
    }
  }

  getStatusColor(st: string): string {
    switch (st) {
      case 'CHO_DUYET': return 'warning';
      case 'DA_DUYET': return 'success';
      case 'TU_CHOI': return 'danger';
      default: return 'secondary';
    }
  }

  /**
   * Lấy nvHoSoId từ API profile
   */
  private loadEmployeeId(): void {
    this.meApi.getMyProfile().subscribe({
      next: (profile) => {
        this.nvHoSoId = profile.nvHoSoId;
        // Sau khi có nvHoSoId, mới load danh sách đơn
        this.load();
      },
      error: () => {
        this.errorMsg = 'Không thể tải thông tin nhân viên. Vui lòng đăng nhập lại.';
        this.toast.danger('Không thể tải thông tin người dùng!');
      }
    });
  }

  /**
   * Load danh sách loại phép từ backend
   * QUAN TRỌNG: Phải load từ BE để tránh lỗi foreign key constraint
   */
  private loadLoaiPhep(): void {
    this.loadingLoaiPhep = true;
    this.loaiPhepService.getList().subscribe({
      next: (data) => {
        this.loaiPhepList = data || [];
        this.loadingLoaiPhep = false;

        if (this.loaiPhepList.length === 0) {
          this.toast.warning('Chưa có loại phép nào được cấu hình. Vui lòng liên hệ quản trị viên!');
        }
      },
      error: (err) => {
        console.error('❌ Load loại phép error:', err);
        this.loadingLoaiPhep = false;
        this.toast.danger('Không thể tải danh sách loại phép. Vui lòng thử lại!');
      }
    });
  }

  /**
   * Mark DON_PHEP notifications as read when user enters this page
   */
  private markRelatedNotificationsAsRead(): void {
    // Mark all DON_PHEP notifications as read (user has entered the page)
    // Note: We don't specify entityId because we want to mark ALL DON_PHEP notifications
    // This is different from marking a specific item's notification
    // For now, we'll rely on user clicking individual notifications
    // Or we could call the API for each item in the list, but that's inefficient
    // Best approach: User clicks notification -> marks as read -> navigates here
    console.log('[DonPhep] User entered page - notifications will be marked as read when clicked');
  }
}
