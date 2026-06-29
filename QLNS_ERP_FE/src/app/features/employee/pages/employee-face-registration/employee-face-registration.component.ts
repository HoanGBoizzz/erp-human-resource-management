import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FaceRecognitionApiService } from 'src/app/core/services/api/face-recognition-api.service';
import { FaceDataDto, RegisterFaceResponseDto } from 'src/app/core/models/face-recognition.model';
import { ToastService } from 'src/app/shared/services/toast.service';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
    selector: 'app-employee-face-registration',
    templateUrl: './employee-face-registration.component.html',
    styleUrls: ['./employee-face-registration.component.scss']
})
export class EmployeeFaceRegistrationComponent implements OnInit {
    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
    @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
    @ViewChild('canvasElement') canvasElement!: ElementRef<HTMLCanvasElement>;

    // Data
    myFaceData: FaceDataDto[] = [];
    currentUser: any = null;

    // Webcam
    isWebcamActive = false;
    stream: MediaStream | null = null;
    capturedImage: string | null = null;

    // UI State
    isLoading = false;
    isUploading = false;
    selectedFile: File | null = null;
    previewUrl: string | null = null;

    constructor(
        private faceApi: FaceRecognitionApiService,
        private auth: AuthService,
        private toast: ToastService
    ) { }

    ngOnInit(): void {
        this.currentUser = this.auth.currentUser;
        this.loadMyFaceData();
    }

    ngOnDestroy(): void {
        this.stopWebcam();
    }

    // ============ DATA LOADING ============
    loadMyFaceData(): void {
        this.isLoading = true;
        this.faceApi.getMyFaceData().subscribe({
            next: (data: FaceDataDto[]) => {
                this.myFaceData = data;
                this.isLoading = false;
            },
            error: (err: any) => {
                console.error('Error loading my face data:', err);
                this.toast.danger('Không thể tải dữ liệu khuôn mặt của bạn');
                this.isLoading = false;
            }
        });
    }

    deleteMyFace(faceId: number | undefined): void {
        if (!faceId) return;
        if (!confirm('Bạn có chắc muốn xóa ảnh này?')) return;

        this.faceApi.deleteMyFaceData(faceId).subscribe({
            next: () => {
                this.toast.success('Đã xóa ảnh');
                this.loadMyFaceData();
            },
            error: (err) => {
                console.error('Error deleting my face:', err);
                this.toast.danger(err.error?.message || 'Lỗi khi xóa ảnh');
            }
        });
    }

    // ============ WEBCAM CONTROL ============
    openCameraModal(): void {
        this.startWebcam();
    }

    closeCameraModal(): void {
        this.stopWebcam();
    }

    async startWebcam(): Promise<void> {
        try {
            // Step 1: Show video element FIRST (set flag để *ngIf render video)
            this.isWebcamActive = true;

            // Step 2: Request camera stream
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: {
                    width: { ideal: 1280 },
                    height: { ideal: 720 },
                    facingMode: 'user'
                }
            });

            // Step 3: Wait for Angular to render video element, then attach stream
            setTimeout(() => {
                if (this.videoElement && this.videoElement.nativeElement) {
                    this.videoElement.nativeElement.srcObject = this.stream;
                    this.videoElement.nativeElement.play();
                } else {
                    console.error('Video element not found after rendering');
                    this.stopWebcam();
                    this.toast.danger('Lỗi khởi tạo video element');
                }
            }, 100);
        } catch (err) {
            console.error('Error accessing webcam:', err);
            this.isWebcamActive = false;
            this.toast.danger('Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.');
        }
    }

    stopWebcam(): void {
        if (this.stream) {
            this.stream.getTracks().forEach(track => track.stop());
            this.stream = null;
            this.isWebcamActive = false;
        }
    }

    capturePhoto(): void {
        if (!this.videoElement || !this.canvasElement) return;

        const video = this.videoElement.nativeElement;
        const canvas = this.canvasElement.nativeElement;

        canvas.width = video.videoWidth;
        canvas.height = video.videoHeight;

        const context = canvas.getContext('2d');
        if (context) {
            context.drawImage(video, 0, 0, canvas.width, canvas.height);
            this.capturedImage = canvas.toDataURL('image/jpeg', 0.9);

            // Convert to File
            canvas.toBlob((blob) => {
                if (blob) {
                    const fileName = `my_face_${Date.now()}.jpg`;
                    this.selectedFile = new File([blob], fileName, { type: 'image/jpeg' });
                    this.previewUrl = this.capturedImage;
                }
            }, 'image/jpeg', 0.9);

            this.stopWebcam();
        }
    }

    resetCapture(): void {
        this.capturedImage = null;
        this.selectedFile = null;
        this.previewUrl = null;
        this.stopWebcam();
    }

    // ============ FILE UPLOAD ============
    onFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files.length > 0) {
            const file = input.files[0];

            // Validate file type
            if (!file.type.match(/image\/(jpeg|jpg|png)/)) {
                this.toast.danger('Chỉ chấp nhận file ảnh JPG, PNG');
                return;
            }

            // Validate file size (max 5MB)
            if (file.size > 5 * 1024 * 1024) {
                this.toast.danger('Kích thước file không được vượt quá 5MB');
                return;
            }

            this.selectedFile = file;

            // Create preview
            const reader = new FileReader();
            reader.onload = (e) => {
                this.previewUrl = e.target?.result as string;
            };
            reader.readAsDataURL(file);
        }
    }

    triggerFileInput(): void {
        this.fileInput.nativeElement.click();
    }

    // ============ FACE REGISTRATION ============
    async registerMyFace(): Promise<void> {
        if (!this.selectedFile) {
            this.toast.danger('Vui lòng chụp ảnh hoặc chọn file');
            return;
        }

        this.isUploading = true;

        this.faceApi.registerSelfFace(this.selectedFile).subscribe({
            next: (response: RegisterFaceResponseDto) => {
                if (response.success) {
                    this.toast.success(response.message || 'Đăng ký khuôn mặt thành công');
                    this.loadMyFaceData();
                    this.resetCapture();
                } else {
                    this.toast.danger(response.message || 'Đăng ký thất bại');
                }
                this.isUploading = false;
            },
            error: (err) => {
                console.error('Error registering face:', err);
                this.toast.danger(err.error?.message || 'Lỗi khi đăng ký khuôn mặt');
                this.isUploading = false;
            }
        });
    }
}
