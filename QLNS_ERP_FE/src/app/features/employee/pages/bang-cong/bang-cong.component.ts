import { Component, OnDestroy, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormControl } from '@angular/forms';
import { ChamCongApiService } from 'src/app/core/services/api/cham-cong-api.service';
import { ChamCongNgayDto, ChamCongOfEmployeeDto } from 'src/app/core/models/cham-cong.model';
import { FaceRecognitionApiService } from 'src/app/core/services/api/face-recognition-api.service';
import { ToastService } from 'src/app/shared/services/toast.service';
import { SignalrService } from 'src/app/core/services/signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-bang-cong',
  templateUrl: './bang-cong.component.html',
  styleUrls: ['./bang-cong.component.scss'],
})
export class BangCongComponent implements OnInit, OnDestroy {
  @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
  @ViewChild('canvasElement') canvasElement!: ElementRef<HTMLCanvasElement>;

  loading = false;
  errorMsg = '';

  yearCtrl = new FormControl<number>(new Date().getFullYear(), { nonNullable: true });
  monthCtrl = new FormControl<number>(new Date().getMonth() + 1, { nonNullable: true });

  years: number[] = [];
  months: number[] = [];
  private readonly allMonths = Array.from({ length: 12 }, (_, i) => i + 1);
  rows: ChamCongNgayDto[] = [];

  todayStatus: ChamCongOfEmployeeDto | null = null;
  attendanceLoading = false;
  attendanceProcessing = false;
  attendanceError = '';
  pendingAction: 'check-in' | 'check-out' | 'check-in-ot' | 'check-out-ot' | null = null;
  cameraError = '';
  loadingProgress = 0;
  private progressInterval: any = null;

  // camera state
  isCameraOpen = false;
  stream: MediaStream | null = null;
  capturedImage: string | null = null;
  currentMode: 'check-in' | 'check-out' | 'check-in-ot' | 'check-out-ot' = 'check-in';
  videoReady = false;

  // stats
  totalOt = 0;
  diLam = 0;
  nghiPhep = 0;
  nghi = 0;

  viewMode: 'calendar' | 'table' = 'calendar';
  private signalrSubscription?: Subscription;

  constructor(
    private api: ChamCongApiService,
    private faceApi: FaceRecognitionApiService,
    private toast: ToastService,
    private signalrService: SignalrService
  ) { }

  ngOnInit(): void {
    this.loadYearOptions();
    this.loadToday();

    // Subscribe to SignalR for realtime attendance updates
    this.signalrSubscription = this.signalrService.entityUpdate$.subscribe((update) => {
      if (update.entityType === 'ChamCong') {
        console.log('[Employee] Realtime ChamCong update received:', update);
        // Reload current month data and today status
        this.loadMonth();
        this.loadToday();
      }
    });
  }

  ngOnDestroy(): void {
    this.stopCamera();
    this.signalrSubscription?.unsubscribe();
  }

  toggleView(mode: 'calendar' | 'table'): void {
    this.viewMode = mode;
  }

  onYearChange(): void {
    this.loadMonth();
  }

  refreshFilters(): void {
    this.loadYearOptions();
  }

  loadYearOptions(): void {
    this.loading = true;
    this.errorMsg = '';

    this.api.getMyYears().subscribe({
      next: (years) => {
        const currentYear = new Date().getFullYear();
        const rangeYears = Array.from({ length: 9 }, (_, idx) => currentYear - 4 + idx); // current +/-4 năm
        const merged = [...(years || []), ...rangeYears];
        this.years = Array.from(new Set(merged)).sort((a, b) => b - a);

        if (!this.years.includes(this.yearCtrl.value)) {
          this.yearCtrl.setValue(this.years[0]);
        }
        this.months = [...this.allMonths];
        this.loadMonth();
      },
      error: () => {
        const currentYear = new Date().getFullYear();
        this.years = Array.from({ length: 9 }, (_, idx) => currentYear - 4 + idx).sort((a, b) => b - a);
        this.months = [...this.allMonths];
        this.errorMsg = '';
        this.loadMonth();
      }
    });
  }

  loadMonth(): void {
    this.loading = true;
    this.errorMsg = '';

    const m = this.monthCtrl.value;
    const y = this.yearCtrl.value;

    this.api.getMyTimesheetMonth(m, y).subscribe({
      next: (res) => {
        this.rows = res || [];
        this.calcStats();
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMsg = 'Không tải được bảng công tháng.';
      }
    });
  }

  loadToday(): void {
    this.attendanceLoading = true;
    this.attendanceError = '';

    const today = new Date().toISOString().split('T')[0];

    this.api.getMyTimesheetDay(today).subscribe({
      next: (res) => {
        this.todayStatus = res || null;
        this.attendanceLoading = false;
      },
      error: () => {
        this.todayStatus = null;
        this.attendanceLoading = false;
      }
    });
  }

  calcStats(): void {
    this.totalOt = this.rows.reduce((s, x) => s + (x.soGioOt || 0), 0);
    this.diLam = this.rows.filter(x => x.trangThai === 'DI_LAM').length;
    this.nghiPhep = this.rows.filter(x => x.trangThai === 'NGHI_PHEP').length;
    this.nghi = this.rows.filter(x => x.trangThai === 'NGHI').length;
  }

  badgeClass(st: string): string {
    switch (st) {
      case 'DI_LAM': return 'text-bg-success';
      case 'NGHI_PHEP': return 'text-bg-warning';
      case 'NGHI': return 'text-bg-danger';
      case 'TRE': return 'text-bg-info';
      default: return 'text-bg-secondary';
    }
  }

  statusText(st: string): string {
    switch (st) {
      case 'DI_LAM': return 'Đi làm';
      case 'NGHI_PHEP': return 'Nghỉ phép';
      case 'NGHI': return 'Nghỉ';
      case 'TRE': return 'Trễ';
      default: return st || '-';
    }
  }

  getStatusIcon(st: string): string {
    switch (st) {
      case 'DI_LAM': return 'bi-check-circle-fill';
      case 'NGHI_PHEP': return 'bi-calendar-x-fill';
      case 'NGHI': return 'bi-x-circle-fill';
      case 'TRE': return 'bi-clock-fill';
      default: return 'bi-question-circle-fill';
    }
  }

  getStatusColor(st: string): string {
    switch (st) {
      case 'DI_LAM': return 'success';
      case 'NGHI_PHEP': return 'warning';
      case 'NGHI': return 'danger';
      case 'TRE': return 'info';
      default: return 'secondary';
    }
  }

  openCamera(mode: 'check-in' | 'check-out' | 'check-in-ot' | 'check-out-ot'): void {
    this.currentMode = mode;
    this.attendanceError = '';
    this.cameraError = '';
    this.capturedImage = null;
    this.videoReady = false;
    this.isCameraOpen = true;
    this.startCamera();
  }

  closeCamera(): void {
    this.isCameraOpen = false;
    this.capturedImage = null;
    this.pendingAction = null;
    this.stopCamera();
  }

  async startCamera(): Promise<void> {
    try {
      this.stream = await navigator.mediaDevices.getUserMedia({
        video: {
          width: { ideal: 1280 },
          height: { ideal: 720 },
          facingMode: 'user'
        }
      });

      setTimeout(() => {
        if (this.videoElement?.nativeElement) {
          this.videoElement.nativeElement.srcObject = this.stream;
          this.videoElement.nativeElement.play();
        }
      }, 100);
    } catch (err) {
      console.error('Không thể mở camera', err);
      this.cameraError = 'Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.';
      this.isCameraOpen = false;
      this.stopCamera();
    }
  }

  stopCamera(): void {
    if (this.stream) {
      this.stream.getTracks().forEach(track => track.stop());
      this.stream = null;
    }
    this.videoReady = false;
  }

  onVideoLoaded(): void {
    this.videoReady = true;
  }

  captureAndProcess(): void {
    if (!this.videoElement || !this.canvasElement) return;

    const video = this.videoElement.nativeElement;
    if (!this.videoReady || video.videoWidth === 0 || video.videoHeight === 0) {
      this.cameraError = 'Camera chưa sẵn sàng. Vui lòng đợi vài giây rồi thử lại.';
      return;
    }

    const canvas = this.canvasElement.nativeElement;

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    const context = canvas.getContext('2d');
    if (!context) return;

    context.drawImage(video, 0, 0, canvas.width, canvas.height);
    this.capturedImage = canvas.toDataURL('image/jpeg', 0.9);

    canvas.toBlob((blob) => {
      if (!blob) { return; }
      const fileName = `attendance_${Date.now()}.jpg`;
      const file = new File([blob], fileName, { type: 'image/jpeg' });
      this.submitAttendance(file, this.currentMode);
    }, 'image/jpeg', 0.9);

    this.stopCamera();
  }

  submitAttendance(file: File, mode: 'check-in' | 'check-out' | 'check-in-ot' | 'check-out-ot'): void {
    this.attendanceError = '';
    this.attendanceProcessing = true;
    this.pendingAction = mode;
    this.startProgressSimulation();

    const isCheckOut = mode === 'check-out' || mode === 'check-out-ot';
    const request$ = isCheckOut
      ? this.faceApi.checkOutByFace(file, mode === 'check-out-ot')
      : this.faceApi.checkInByFace(file, mode === 'check-in-ot');

    request$.subscribe({
      next: (res) => {
        this.stopProgressSimulation();
        this.toast.success(res?.message || 'Chấm công thành công');
        this.attendanceProcessing = false;
        this.pendingAction = null;
        this.isCameraOpen = false;
        this.loadToday();
        this.loadMonth();
      },
      error: (err) => {
        this.stopProgressSimulation();
        this.attendanceProcessing = false;
        this.pendingAction = null;
        this.attendanceError = err?.error?.message || 'Không chấm công được. Vui lòng thử lại!';
        this.isCameraOpen = false;
        this.toast.danger(this.attendanceError);
      }
    });
  }

  private startProgressSimulation(): void {
    this.loadingProgress = 0;
    const maxDuration = 3000; // 3 giây tối đa
    const intervalMs = 50; // Cập nhật mỗi 50ms
    const increment = (100 / maxDuration) * intervalMs;

    this.progressInterval = setInterval(() => {
      this.loadingProgress += increment;
      if (this.loadingProgress >= 100) {
        this.loadingProgress = 100;
        this.stopProgressSimulation();
      }
    }, intervalMs);
  }

  private stopProgressSimulation(): void {
    if (this.progressInterval) {
      clearInterval(this.progressInterval);
      this.progressInterval = null;
    }
    this.loadingProgress = 100;
    setTimeout(() => {
      this.loadingProgress = 0;
    }, 500);
  }

  formatTime(val: string | null | undefined): string {
    if (!val) return '-';
    // Handles "2026-03-02T08:00:00" or "08:00:00" or "08:00"
    const match = val.match(/T?(\d{2}:\d{2})/);
    return match ? match[1] : val;
  }
}
