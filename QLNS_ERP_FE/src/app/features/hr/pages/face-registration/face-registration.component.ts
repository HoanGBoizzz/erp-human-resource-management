import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FaceRecognitionApiService } from 'src/app/core/services/api/face-recognition-api.service';
import { FaceDataDto, RegisterFaceResponseDto } from 'src/app/core/models/face-recognition.model';
import { ToastService } from 'src/app/shared/services/toast.service';
import { NhanVienApiService } from 'src/app/core/services/api/nhan-vien-api.service';

@Component({
    selector: 'app-face-registration',
    templateUrl: './face-registration.component.html',
    styleUrls: ['./face-registration.component.scss']
})
export class FaceRegistrationComponent implements OnInit {
    @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
    @ViewChild('videoElement') videoElement!: ElementRef<HTMLVideoElement>;
    @ViewChild('canvasElement') canvasElement!: ElementRef<HTMLCanvasElement>;

    // Data
    registeredEmployees: FaceDataDto[] = [];
    selectedEmployeeFaces: FaceDataDto[] = [];
    allEmployees: any[] = [];
    selectedEmployee: any = null;

    // Webcam
    isWebcamActive = false;
    stream: MediaStream | null = null;
    capturedImage: string | null = null;

    // UI State
    isLoading = false;
    isUploading = false;
    selectedFile: File | null = null;
    previewUrl: string | null = null;

    // Filter
    searchKeyword = '';

    constructor(
        private faceApi: FaceRecognitionApiService,
        private nhanVienApi: NhanVienApiService,
        private toast: ToastService
    ) { }

    ngOnInit(): void {
        this.loadRegisteredEmployees();
        this.loadAllEmployees();
    }

    ngOnDestroy(): void {
        this.stopWebcam();
    }

    get filteredEmployees(): any[] {
        const keyword = this.searchKeyword.trim().toLowerCase();
        if (!keyword) return this.allEmployees;
        return this.allEmployees.filter((e) =>
            e.hoTen?.toLowerCase().includes(keyword) ||
            e.maNhanVien?.toLowerCase().includes(keyword)
        );
    }

    get filteredRegistered(): FaceDataDto[] {
        const keyword = this.searchKeyword.trim().toLowerCase();
        if (!keyword) return this.registeredEmployees;
        return this.registeredEmployees.filter((e) =>
            e.tenNhanVien?.toLowerCase().includes(keyword) ||
            e.maNhanVien?.toLowerCase().includes(keyword)
        );
    }

    // ============ DATA LOADING ============
    loadRegisteredEmployees(): void {
        this.isLoading = true;
        this.faceApi.getRegisteredEmployees().subscribe({
            next: (data) => {
                this.registeredEmployees = data;
                this.isLoading = false;
            },
            error: (err) => {
                console.error('Error loading registered employees:', err);
                this.toast.danger('Không thể tải danh sách nhân viên đã đăng ký');
                this.isLoading = false;
            }
        });
    }

    loadAllEmployees(): void {
        this.nhanVienApi.getAllNhanVien().subscribe({
            next: (data) => {
                this.allEmployees = data;
            },
            error: (err) => {
                console.error('Error loading employees:', err);
            }
        });
    }

    // ============ EMPLOYEE SELECTION ============
    onEmployeeSelect(employee: any): void {
        this.selectedEmployee = employee;
        this.resetCapture();
        this.loadEmployeeFaces(employee.id);
    }

    clearSelection(): void {
        this.selectedEmployee = null;
        this.selectedEmployeeFaces = [];
        this.resetCapture();
    }

    loadEmployeeFaces(nvId: number): void {
        this.faceApi.getEmployeeFaceData(nvId).subscribe({
            next: (data) => {
                this.selectedEmployeeFaces = data;
            },
            error: (err) => {
                console.error('Error loading employee faces:', err);
                this.toast.danger('Không tải được danh sách ảnh của nhân viên');
            }
        });
    }

    // ============ WEBCAM CONTROL ============
    openCameraModal(): void {
        if (!this.selectedEmployee) {
            this.toast.danger('Vui lòng chọn nhân viên trước');
            return;
        }
        this.startWebcam();
    }

    closeCameraModal(): void {
        this.stopWebcam();
        this.isWebcamActive = false;
    }

    async startWebcam(): Promise<void> {
        try {
            this.isWebcamActive = true;
            this.stream = await navigator.mediaDevices.getUserMedia({
                video: { width: { ideal: 1280 }, height: { ideal: 720 }, facingMode: 'user' }
            });

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
        }
        this.isWebcamActive = false;
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

            canvas.toBlob((blob) => {
                if (blob) {
                    const fileName = `face_${this.selectedEmployee?.maNhanVien}_${Date.now()}.jpg`;
                    this.selectedFile = new File([blob], fileName, { type: 'image/jpeg' });
                    this.previewUrl = this.capturedImage;
                }
            }, 'image/jpeg', 0.9);

            this.stopWebcam();
        }
    }

    retakePhoto(): void {
        this.resetCapture();
        this.startWebcam();
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

            if (!file.type.match(/image\/(jpeg|jpg|png)/)) {
                this.toast.danger('Chỉ chấp nhận file ảnh JPG, PNG');
                return;
            }

            if (file.size > 5 * 1024 * 1024) {
                this.toast.danger('Kích thước file không được vượt quá 5MB');
                return;
            }

            this.selectedFile = file;

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
    async registerFace(): Promise<void> {
        if (!this.selectedEmployee) {
            this.toast.danger('Vui lòng chọn nhân viên');
            return;
        }

        if (!this.selectedFile) {
            this.toast.danger('Vui lòng chụp ảnh hoặc chọn file');
            return;
        }

        this.isUploading = true;

        this.faceApi.registerFace(this.selectedEmployee.id, this.selectedFile).subscribe({
            next: (response: RegisterFaceResponseDto) => {
                if (response.success) {
                    this.toast.success(response.message || 'Đăng ký khuôn mặt thành công');
                    this.loadRegisteredEmployees();
                    this.loadEmployeeFaces(this.selectedEmployee.id);
                    this.resetCapture();
                    this.clearSelection();
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

    // ============ FACE DATA MANAGEMENT ============
    deleteFace(employee: FaceDataDto, faceId?: number): void {
        const confirmMsg = faceId
            ? `Bạn có chắc muốn xóa ảnh này của ${employee.tenNhanVien}?`
            : `Bạn có chắc muốn xóa TẤT CẢ ảnh khuôn mặt của ${employee.tenNhanVien}?`;

        if (!confirm(confirmMsg)) return;

        const deleteObs = faceId
            ? this.faceApi.deleteFaceData(faceId)
            : this.faceApi.deleteAllFaceData(employee.nvHoSoId);

        deleteObs.subscribe({
            next: () => {
                this.toast.success('Xóa thành công');
                this.loadRegisteredEmployees();
                if (this.selectedEmployee) {
                    this.loadEmployeeFaces(this.selectedEmployee.id);
                }
            },
            error: (err) => {
                console.error('Error deleting face data:', err);
                this.toast.danger('Lỗi khi xóa dữ liệu khuôn mặt');
            }
        });
    }
}
