import { Component, EventEmitter, OnInit, Output, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MeApiService } from 'src/app/core/services/api/me-api.service';
import { AuthApiService } from 'src/app/core/services/api/auth-api.service';
import { HoSoCaNhanDto } from 'src/app/core/models/ho-so-ca-nhan.model';
import { ToastService } from '../../services/toast.service';
import { UserAvatarService } from 'src/app/core/services/user-avatar.service';
import { environment } from 'src/environments/environment';
import { Subscription } from 'rxjs';

@Component({
    selector: 'app-profile-sidebar',
    templateUrl: './profile-sidebar.component.html',
    styleUrls: ['./profile-sidebar.component.scss']
})
export class ProfileSidebarComponent implements OnInit, OnDestroy {
    @Output() close = new EventEmitter<void>();

    profile: HoSoCaNhanDto | null = null;
    loading = false;

    // Tab hiện tại: 'info' | 'bank' | 'password'
    activeTab: 'info' | 'bank' | 'password' = 'info';

    // Form cập nhật STK
    bankForm!: FormGroup;
    updatingBank = false;

    // Form đổi mật khẩu
    passwordForm!: FormGroup;
    updatingPassword = false;
    showCurrentPassword = false;
    showNewPassword = false;
    showConfirmPassword = false;

    // Upload avatar
    uploadingAvatar = false;
    uploadingStkImage = false;

    // Cached URLs để tránh lỗi ExpressionChangedAfterItHasBeenCheckedError
    displayAvatarUrl: string | null = null;
    displayStkImageUrl: string | null = null;

    // Subscription để đồng bộ avatar
    private avatarSubscription?: Subscription;

    constructor(
        private meApi: MeApiService,
        private authApi: AuthApiService,
        private fb: FormBuilder,
        private toast: ToastService,
        private avatarService: UserAvatarService
    ) { }

    ngOnInit(): void {
        this.initForms();
        this.loadProfile();
        this.subscribeToAvatarChanges();
    }

    ngOnDestroy(): void {
        this.avatarSubscription?.unsubscribe();
    }

    /**
     * Subscribe vào avatar service để đồng bộ khi avatar thay đổi từ bên ngoài
     */
    subscribeToAvatarChanges(): void {
        this.avatarSubscription = this.avatarService.avatar$.subscribe(newAvatarUrl => {
            if (this.profile && newAvatarUrl) {
                // Cập nhật avatar trong profile với timestamp để force refresh
                // Loại bỏ timestamp cũ nếu có
                const cleanUrl = newAvatarUrl.split('?')[0];
                const urlWithTimestamp = `${cleanUrl}?t=${new Date().getTime()}`;
                this.profile.anhCaNhanUrl = urlWithTimestamp;
                // Cập nhật displayAvatarUrl để UI refresh
                this.displayAvatarUrl = urlWithTimestamp;
            }
        });
    }

    initForms(): void {
        this.bankForm = this.fb.group({
            soTaiKhoanNganHang: ['', [Validators.maxLength(50)]]
        });

        this.passwordForm = this.fb.group({
            currentPassword: ['', [Validators.required, Validators.minLength(6)]],
            newPassword: ['', [Validators.required, Validators.minLength(6)]],
            confirmPassword: ['', [Validators.required]]
        }, {
            validators: this.passwordMatchValidator
        });
    }

    passwordMatchValidator(group: FormGroup): { [key: string]: boolean } | null {
        const newPwd = group.get('newPassword')?.value;
        const confirmPwd = group.get('confirmPassword')?.value;
        return newPwd === confirmPwd ? null : { passwordMismatch: true };
    }

    loadProfile(): void {
        this.loading = true;
        this.meApi.getMyProfile().subscribe({
            next: (data) => {
                this.profile = data;
                this.bankForm.patchValue({
                    soTaiKhoanNganHang: data.soTaiKhoanNganHang || ''
                });

                // Nếu service chưa có avatar (chưa upload trong session này),
                // seed từ API để topbar & các component khác đều hiển thị được
                if (!this.avatarService.currentAvatar && data.anhCaNhanUrl) {
                    this.avatarService.updateAvatar(data.anhCaNhanUrl);
                }

                // Lấy avatar từ service (sau khi đã seed nếu cần)
                const avatarFromService = this.avatarService.currentAvatar;
                if (avatarFromService) {
                    this.displayAvatarUrl = avatarFromService;
                } else {
                    this.displayAvatarUrl = this.getFullImageUrl(data.anhCaNhanUrl);
                }

                // Cache STK image URL
                this.displayStkImageUrl = this.getFullImageUrl(data.anhStkUrl);
                this.loading = false;
            },
            error: (err) => {
                console.error('Lỗi tải hồ sơ:', err);
                this.toast.danger('Không thể tải thông tin hồ sơ!');
                this.loading = false;
            }
        });
    }

    onClose(): void {
        this.close.emit();
    }

    switchTab(tab: 'info' | 'bank' | 'password'): void {
        this.activeTab = tab;
    }

    // ============================================================
    // CẬP NHẬT SỐ TÀI KHOẢN
    // ============================================================
    updateBankAccount(): void {
        if (this.bankForm.invalid) {
            this.bankForm.markAllAsTouched();
            return;
        }

        this.updatingBank = true;
        const dto = {
            soTaiKhoanNganHang: this.bankForm.value.soTaiKhoanNganHang?.trim() || null
        };

        this.meApi.updateMyBankAccount(dto).subscribe({
            next: (res) => {
                this.toast.success(res.message || 'Cập nhật số tài khoản thành công!');
                this.updatingBank = false;
                this.loadProfile(); // Reload để cập nhật dữ liệu mới
            },
            error: (err) => {
                console.error('Lỗi cập nhật STK:', err);
                this.toast.danger(err.error?.message || 'Cập nhật số tài khoản thất bại!');
                this.updatingBank = false;
            }
        });
    }

    // ============================================================
    // ĐỔI MẬT KHẨU
    // ============================================================
    changePassword(): void {
        if (this.passwordForm.invalid) {
            this.passwordForm.markAllAsTouched();
            return;
        }

        this.updatingPassword = true;
        const dto = {
            oldPassword: this.passwordForm.value.currentPassword,
            newPassword: this.passwordForm.value.newPassword
        };

        this.authApi.changePassword(dto).subscribe({
            next: (res) => {
                this.toast.success(res.message || 'Đổi mật khẩu thành công!');
                this.passwordForm.reset();
                this.updatingPassword = false;
                // Reset visibility toggles
                this.showCurrentPassword = false;
                this.showNewPassword = false;
                this.showConfirmPassword = false;
            },
            error: (err) => {
                console.error('Lỗi đổi mật khẩu:', err);
                this.toast.danger(err.error?.message || 'Đổi mật khẩu thất bại!');
                this.updatingPassword = false;
            }
        });
    }

    // ============================================================
    // UPLOAD AVATAR
    // ============================================================
    onAvatarFileSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) return;

        const file = input.files[0];

        // Validate file
        const maxSize = 2 * 1024 * 1024; // 2MB
        if (file.size > maxSize) {
            this.toast.danger('Kích thước ảnh không được vượt quá 2MB!');
            return;
        }

        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png'];
        if (!allowedTypes.includes(file.type)) {
            this.toast.danger('Chỉ chấp nhận file ảnh JPG, JPEG, PNG!');
            return;
        }

        this.uploadingAvatar = true;
        this.meApi.uploadMyAvatar(file).subscribe({
            next: (res) => {
                this.toast.success(res.message || 'Cập nhật ảnh đại diện thành công!');

                // Cập nhật avatar URL ngay lập tức
                if (this.profile && res.url) {
                    const fullUrl = this.getFullImageUrl(res.url);
                    this.profile.anhCaNhanUrl = res.url;
                    this.displayAvatarUrl = fullUrl;
                    // Cập nhật vào UserAvatarService để đồng bộ toàn app
                    this.avatarService.updateAvatar(res.url);
                }

                this.uploadingAvatar = false;
            },
            error: (err) => {
                console.error('Lỗi upload avatar:', err);
                this.toast.danger(err.error?.message || 'Upload ảnh thất bại!');
                this.uploadingAvatar = false;
            }
        });
    }

    // ============================================================
    // UPLOAD ẢNH TÀI KHOẢN NGÂN HÀNG
    // ============================================================
    onStkImageSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) return;

        const file = input.files[0];

        // Validate file
        const maxSize = 2 * 1024 * 1024; // 2MB
        if (file.size > maxSize) {
            this.toast.danger('Kích thước ảnh không được vượt quá 2MB!');
            return;
        }

        const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png'];
        if (!allowedTypes.includes(file.type)) {
            this.toast.danger('Chỉ chấp nhận file ảnh JPG, JPEG, PNG!');
            return;
        }

        this.uploadingStkImage = true;
        this.meApi.uploadMyAnhStk(file).subscribe({
            next: (res) => {
                this.toast.success(res.message || 'Cập nhật ảnh tài khoản ngân hàng thành công!');

                // Cập nhật ảnh STK ngay lập tức
                if (this.profile && res.url) {
                    const fullUrl = this.getFullImageUrl(res.url);
                    this.profile.anhStkUrl = res.url;
                    this.displayStkImageUrl = fullUrl;
                }

                this.uploadingStkImage = false;
            },
            error: (err) => {
                console.error('Lỗi upload ảnh STK:', err);
                this.toast.danger(err.error?.message || 'Upload ảnh tài khoản ngân hàng thất bại!');
                this.uploadingStkImage = false;
            }
        });
    }

    // ============================================================
    // HELPERS
    // ============================================================
    getFullImageUrl(url: string | null | undefined): string | null {
        if (!url) return null;

        let finalUrl = url;
        // Nếu là relative path, thêm base URL
        if (!url.startsWith('http://') && !url.startsWith('https://')) {
            finalUrl = `${environment.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
        }

        // Thêm timestamp để force reload ảnh mới nhất (loại bỏ timestamp cũ nếu có)
        const cleanUrl = finalUrl.split('?')[0];
        return `${cleanUrl}?t=${new Date().getTime()}`;
    }

    getGenderLabel(gioiTinh: number | null | undefined): string {
        if (gioiTinh === 0) return 'Nữ';
        if (gioiTinh === 1) return 'Nam';
        return 'Chưa xác định';
    }

    formatDate(date: string | null | undefined): string {
        if (!date) return 'Chưa cập nhật';
        const d = new Date(date);
        return d.toLocaleDateString('vi-VN');
    }
}
