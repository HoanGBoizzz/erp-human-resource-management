import { Component, OnInit, ViewChild, ElementRef, OnDestroy } from '@angular/core';
import { FaceRecognitionApiService } from 'src/app/core/services/api/face-recognition-api.service';
import { FaceRecognitionResultDto } from 'src/app/core/models/face-recognition.model';

@Component({
    selector: 'app-attendance-kiosk',
    templateUrl: './attendance-kiosk.component.html',
    styleUrls: ['./attendance-kiosk.component.scss']
})
export class AttendanceKioskComponent implements OnInit, OnDestroy {
    @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
    @ViewChild('canvasElement') canvasElement!: ElementRef<HTMLCanvasElement>;

    // Mode
    currentMode: 'idle' | 'check-in' | 'check-out' | 'check-in-ot' = 'idle';

    // Webcam
    stream: MediaStream | null = null;
    isWebcamActive = false;

    // Processing
    isProcessing = false;
    loadingProgress = 0;
    loadingInterval: any = null;
    capturedImage: string | null = null;

    // Result
    result: FaceRecognitionResultDto | null = null;
    showResult = false;

    // OT Confirmation Dialog
    showOtConfirmDialog = false;
    pendingOtImageFile: File | null = null;

    // Auto-reset timer
    resetTimer: any = null;
    countdown = 5;

    // Current time
    currentTime = new Date();
    timeInterval: any = null;

    constructor(private faceApi: FaceRecognitionApiService) { }

    ngOnInit(): void {
        this.updateTime();
        this.timeInterval = setInterval(() => this.updateTime(), 1000);
    }

    ngOnDestroy(): void {
        this.stopWebcam();
        if (this.timeInterval) {
            clearInterval(this.timeInterval);
        }
        if (this.resetTimer) {
            clearTimeout(this.resetTimer);
        }
        if (this.loadingInterval) {
            clearInterval(this.loadingInterval);
        }
    }

    updateTime(): void {
        this.currentTime = new Date();
    }

    // ============ MODE SELECTION ============
    selectMode(mode: 'check-in' | 'check-out' | 'check-in-ot'): void {
        this.currentMode = mode;
        this.startWebcam();
    }

    // ============ WEBCAM CONTROL ============
    async startWebcam(): Promise<void> {
        try {
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: { ideal: 1280 },
                    height: { ideal: 720 },
                    facingMode: 'user'
                }
            });

            setTimeout(() => {
                if (this.videoElement) {
                    this.videoElement.nativeElement.srcObject = this.stream;
                    this.videoElement.nativeElement.play();
                    this.isWebcamActive = true;
                }
            }, 100);

        } catch (err) {
            console.error('Error accessing webcam:', err);
            alert('Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.');
            this.resetToIdle();
        }
    }

    stopWebcam(): void {
        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
            this.isWebcamActive = false;
        }
    }

    // ============ CAPTURE & PROCESS ============
    captureAndProcess(): void {
        if (!this.videoElement || !this.canvasElement) return;

        const video = this.videoElement.nativeElement;
        const canvas = this.canvasElement.nativeElement;

        // OPTIMIZE: Resize image for speed (target < 3s)
        let width = video.videoWidth;
        let height = video.videoHeight;

        // Limit max width to 800px to reduce upload size
        if (width > 800) {
            const scale = 800 / width;
            width = 800;
            height = height * scale;
        }

        canvas.width = width;
        canvas.height = height;

        const context = canvas.getContext('2d');
        if (context) {
            context.drawImage(video, 0, 0, canvas.width, canvas.height);
            this.capturedImage = canvas.toDataURL('image/jpeg', 0.9);

            // Convert to File and process
            canvas.toBlob((blob) => {
                if (blob) {
                    const fileName = `attendance_${Date.now()}.jpg`;
                    const file = new File([blob], fileName, { type: 'image/jpeg' });
                    this.processAttendance(file);
                }
            }, 'image/jpeg', 0.9);

            this.stopWebcam();
        }
    }

    processAttendance(imageFile: File, xacNhanOt = false): void {
        this.isProcessing = true;
        this.loadingProgress = 0;

        // Simulate progress from 0% to 90% while waiting
        if (this.loadingInterval) clearInterval(this.loadingInterval);
        this.loadingInterval = setInterval(() => {
            if (this.loadingProgress < 90) {
                // Increment faster essentially to look responsive
                const step = this.loadingProgress < 60 ? 5 : 2;
                this.loadingProgress += step;
            }
        }, 100);

        const isOtMode = this.currentMode === 'check-in-ot';
        const apiCall = this.currentMode !== 'check-out'
            ? this.faceApi.checkInByFace(imageFile, xacNhanOt || isOtMode)
            : this.faceApi.checkOutByFace(imageFile);

        apiCall.subscribe({
            next: (response: FaceRecognitionResultDto) => {
                this.loadingProgress = 100;
                clearInterval(this.loadingInterval);

                setTimeout(() => {
                    this.isProcessing = false;

                    // Nếu BE yêu cầu xác nhận tăng ca → hiện dialog (không áp dụng cho mode OT)
                    if (response.requireOtConfirmation && this.currentMode !== 'check-in-ot') {
                        this.pendingOtImageFile = imageFile;
                        this.showOtConfirmDialog = true;
                        return;
                    }

                    this.result = response;
                    this.showResult = true;
                    this.startCountdown();
                }, 200);
            },
            error: (err) => {
                this.loadingProgress = 100;
                clearInterval(this.loadingInterval);

                setTimeout(() => {
                    // Kiểm tra lỗi 400 có requireOtConfirmation không (BE trả 400 khi !success)
                    const errBody = err.error as FaceRecognitionResultDto;
                    if (errBody?.requireOtConfirmation) {
                        this.isProcessing = false;
                        this.pendingOtImageFile = imageFile;
                        this.showOtConfirmDialog = true;
                        return;
                    }

                    console.error('Error processing attendance:', err);
                    this.result = {
                        success: false,
                        message: errBody?.message || 'Không nhận diện được khuôn mặt. Vui lòng thử lại.'
                    };
                    this.showResult = true;
                    this.isProcessing = false;
                    this.startCountdown();
                }, 200);
            }
        });
    }

    // ============ OT CONFIRMATION ============
    confirmOt(): void {
        this.showOtConfirmDialog = false;
        if (this.pendingOtImageFile) {
            this.processAttendance(this.pendingOtImageFile, true);
            this.pendingOtImageFile = null;
        }
    }

    rejectOt(): void {
        this.showOtConfirmDialog = false;
        this.pendingOtImageFile = null;
        this.capturedImage = null;
        // Quay lại camera để nhân viên chụp lại hoặc thoát
        this.startWebcam();
    }

    // ============ COUNTDOWN & RESET ============
    startCountdown(): void {
        this.countdown = 5;

        const countdownInterval = setInterval(() => {
            this.countdown--;

            if (this.countdown <= 0) {
                clearInterval(countdownInterval);
                this.resetToIdle();
            }
        }, 1000);
    }

    resetToIdle(): void {
        this.currentMode = 'idle';
        this.capturedImage = null;
        this.result = null;
        this.showResult = false;
        this.isProcessing = false;
        this.showOtConfirmDialog = false;
        this.pendingOtImageFile = null;
        this.stopWebcam();
        this.countdown = 5;
    }

    // ============ MANUAL ACTIONS ============
    cancel(): void {
        this.resetToIdle();
    }

    retake(): void {
        this.capturedImage = null;
        this.result = null;
        this.showResult = false;
        this.startWebcam();
    }

    // ============ HELPERS ============
    get modeTitle(): string {
        if (this.currentMode === 'check-in') return 'CHẤM CÔNG VÀO';
        if (this.currentMode === 'check-in-ot') return 'VÀO TĂNG CA';
        return 'CHẤM CÔNG RA';
    }

    get modeIcon(): string {
        if (this.currentMode === 'check-in') return 'fa-sign-in-alt';
        if (this.currentMode === 'check-in-ot') return 'fa-business-time';
        return 'fa-sign-out-alt';
    }

    get modeColor(): string {
        if (this.currentMode === 'check-in') return 'success';
        if (this.currentMode === 'check-in-ot') return 'warning';
        return 'danger';
    }
}
