import { Component, OnInit, OnDestroy } from '@angular/core';
import { MeApiService } from 'src/app/core/services/api/me-api.service';
import { HoSoCaNhanDto } from 'src/app/core/models/ho-so-ca-nhan.model';
import { ToastService } from 'src/app/shared/services/toast.service';
import { UserAvatarService } from 'src/app/core/services/user-avatar.service';
import { finalize, Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';

@Component({
    selector: 'app-ho-so',
    templateUrl: './ho-so.component.html',
    styleUrls: ['./ho-so.component.scss'],
})
export class HoSoComponent implements OnInit, OnDestroy {
    loading = false;
    errorMsg = '';

    // Avatar upload
    uploadingAvatar = false;

    // Bank account edit
    editingBank = false;
    savingBank = false;
    bankAccountInput = '';

    // STK Image upload
    uploadingStk = false;
    showStkImageModal = false;

    // Avatar subscription để đồng bộ khi update từ profile-sidebar
    private avatarSubscription?: Subscription;

    // Init default to avoid optional-chaining warnings
    profile: HoSoCaNhanDto = {
        tenDangNhap: '',
        vaiTroId: 0,
        maVaiTro: '',
        tenVaiTro: '',
        nvHoSoId: 0,
        maNhanVien: '',
        hoTen: '',
        ngaySinh: null,
        gioiTinh: null,
        diaChi: null,
        soDienThoai: null,
        emailCaNhan: null,
        soTaiKhoanNganHang: null,
        anhCaNhanUrl: null,
        anhStkUrl: null,
        congViecHienTai: null,
    };

    constructor(
        private api: MeApiService,
        private toast: ToastService,
        private avatarService: UserAvatarService
    ) { }

    ngOnInit(): void {
        this.load();
        this.subscribeToAvatarChanges();
    }

    ngOnDestroy(): void {
        this.avatarSubscription?.unsubscribe();
    }

    /**
     * Subscribe vào avatar service để đồng bộ khi avatar thay đổi từ profile-sidebar
     */
    subscribeToAvatarChanges(): void {
        this.avatarSubscription = this.avatarService.avatar$.subscribe(newAvatarUrl => {
            if (newAvatarUrl && this.profile) {
                console.log('[HoSoComponent] Avatar updated from service:', newAvatarUrl);
                // Cập nhật avatar trong profile để UI tự động refresh
                this.profile.anhCaNhanUrl = newAvatarUrl;
            }
        });
    }

    load(): void {
        this.loading = true;
        this.errorMsg = '';

        this.api.getMyProfile().subscribe({
            next: (res) => {
                console.log('[HoSoComponent] Profile loaded:', res);
                this.profile = res;

                // Xử lý avatar URL
                const fullAvatarUrl = this.getFullAvatarUrl(res.anhCaNhanUrl);
                console.log('[HoSoComponent] Avatar URL from API:', res.anhCaNhanUrl);
                console.log('[HoSoComponent] Full avatar URL:', fullAvatarUrl);

                // Cập nhật profile với full URL
                this.profile.anhCaNhanUrl = fullAvatarUrl;

                // Xử lý STK Image URL
                if (res.anhStkUrl) {
                    this.profile.anhStkUrl = this.getFullAvatarUrl(res.anhStkUrl);
                }

                this.loading = false;

                // Sync avatar với service
                if (fullAvatarUrl) {
                    console.log('[HoSoComponent] Syncing avatar to service:', fullAvatarUrl);
                    this.avatarService.updateAvatar(fullAvatarUrl);
                }

                this.toast.success('Đã làm mới hồ sơ cá nhân!');
            },
            error: (err) => {
                console.error('[HoSoComponent] Error loading profile:', err);
                this.errorMsg = 'Không tải được hồ sơ cá nhân.';
                this.loading = false;
                this.toast.danger('Không thể tải hồ sơ. Vui lòng thử lại!');
            },
        });
    }

    // ============ AVATAR UPLOAD ============
    onAvatarSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) {
            console.log('[HoSoComponent] No file selected');
            return;
        }

        const file = input.files[0];
        console.log('[HoSoComponent] File selected:', {
            name: file.name,
            type: file.type,
            size: file.size,
            sizeInMB: (file.size / 1024 / 1024).toFixed(2) + ' MB'
        });

        // Validate file type
        if (!file.type.startsWith('image/')) {
            console.warn('[HoSoComponent] Invalid file type:', file.type);
            this.toast.danger('Vui lòng chọn file hình ảnh!');
            input.value = '';
            return;
        }

        // Validate file size (max 5MB)
        const maxSizeMB = 5;
        const maxSizeBytes = maxSizeMB * 1024 * 1024;
        if (file.size > maxSizeBytes) {
            console.warn('[HoSoComponent] File too large:', {
                fileSize: file.size,
                maxSize: maxSizeBytes,
                fileSizeMB: (file.size / 1024 / 1024).toFixed(2),
                maxSizeMB: maxSizeMB
            });
            this.toast.danger(`File ảnh không được vượt quá ${maxSizeMB}MB! (File hiện tại: ${(file.size / 1024 / 1024).toFixed(2)}MB)`);
            input.value = '';
            return;
        }

        console.log('[HoSoComponent] Starting avatar upload...');
        this.uploadingAvatar = true;

        this.api
            .uploadMyAvatar(file)
            .pipe(finalize(() => {
                this.uploadingAvatar = false;
                console.log('[HoSoComponent] Upload finished');
            }))
            .subscribe({
                next: (res) => {
                    console.log('[HoSoComponent] Upload success, response:', res);

                    // Xử lý avatar URL - có thể là relative hoặc full URL
                    const fullAvatarUrl = this.getFullAvatarUrl(res.url);
                    console.log('[HoSoComponent] Avatar URL from response:', res.url);
                    console.log('[HoSoComponent] Full avatar URL:', fullAvatarUrl);

                    // Cập nhật profile local với full URL
                    this.profile.anhCaNhanUrl = fullAvatarUrl;

                    // Sync với service để topbar cập nhật
                    if (fullAvatarUrl) {
                        this.avatarService.updateAvatar(fullAvatarUrl);
                    }

                    this.toast.success('Cập nhật ảnh đại diện thành công!');
                },
                error: (err) => {
                    console.error('[HoSoComponent] Upload error:', err);
                    this.toast.danger('Không thể tải lên ảnh. Vui lòng thử lại!');
                },
            });

        // Reset input
        input.value = '';
    }

    // ============ BANK ACCOUNT EDIT ============
    startEditBank(): void {
        this.editingBank = true;
        this.bankAccountInput = this.profile.soTaiKhoanNganHang || '';
    }

    cancelEditBank(): void {
        this.editingBank = false;
        this.bankAccountInput = '';
    }

    saveBank(): void {
        this.savingBank = true;

        this.api
            .updateMyBankAccount({ soTaiKhoanNganHang: this.bankAccountInput || null })
            .pipe(finalize(() => (this.savingBank = false)))
            .subscribe({
                next: () => {
                    console.log('[HoSo] ✅ STK updated successfully:', this.bankAccountInput);
                    this.profile.soTaiKhoanNganHang = this.bankAccountInput || null;
                    this.editingBank = false;
                    this.toast.success('Cập nhật số tài khoản thành công!');
                },
                error: (err) => {
                    console.error('[HoSo] ❌ STK update failed:', err);
                    this.toast.danger('Không thể cập nhật số tài khoản. Vui lòng thử lại!');
                },
            });
    }

    // ============ HELPERS ============
    /**
     * Xử lý avatar URL - nếu là relative path thì thêm base URL
     * Thêm timestamp để tránh cache trình duyệt
     */
    getFullAvatarUrl(url: string | null | undefined): string | null {
        if (!url) return null;

        let finalUrl = url;
        // Nếu là relative path, thêm base URL
        if (!url.startsWith('http://') && !url.startsWith('https://')) {
            finalUrl = `${environment.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
        }

        // Thêm timestamp để force reload ảnh mới nhất
        return `${finalUrl}?t=${new Date().getTime()}`;
    }

    initials(name: string): string {
        const s = (name || '').trim();
        if (!s) return 'NV';
        const parts = s.split(/\s+/).filter(Boolean);
        const a = parts[0]?.[0] || '';
        const b = parts.length > 1 ? parts[parts.length - 1]?.[0] || '' : '';
        return (a + b).toUpperCase();
    }

    genderText(v: number | null | undefined): string {
        if (v === 1) return 'Nam';
        if (v === 2) return 'Nữ';
        return '—';
    }

    workStatusText(v: number | null | undefined): string {
        if (v === 1) return 'Đang làm';
        if (v === 0) return 'Đã nghỉ';
        return '—';
    }

    // ============ STK IMAGE UPLOAD ============
    onStkImageSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files || input.files.length === 0) {
            return;
        }

        const file = input.files[0];

        // Validate file type
        if (!file.type.startsWith('image/')) {
            this.toast.danger('Vui lòng chọn file hình ảnh!');
            input.value = '';
            return;
        }

        // Validate file size (max 5MB)
        const maxSizeMB = 5;
        const maxSizeBytes = maxSizeMB * 1024 * 1024;
        if (file.size > maxSizeBytes) {
            this.toast.danger(`File ảnh không được vượt quá ${maxSizeMB}MB!`);
            input.value = '';
            return;
        }

        this.uploadingStk = true;

        this.api
            .uploadMyAnhStk(file)
            .pipe(finalize(() => {
                this.uploadingStk = false;
            }))
            .subscribe({
                next: (res) => {
                    const fullUrl = this.getFullAvatarUrl(res.url);
                    this.profile.anhStkUrl = fullUrl;
                    this.toast.success('Tải lên ảnh sao kê thành công!');
                },
                error: (err) => {
                    console.error('[HoSoComponent] Upload STK error:', err);
                    this.toast.danger('Không thể tải lên ảnh. Vui lòng thử lại!');
                },
            });

        input.value = '';
    }

    deleteStkImage(): void {
        if (!confirm('Bạn có chắc chắn muốn xóa ảnh sao kê?')) {
            return;
        }

        this.uploadingStk = true;

        this.api
            .deleteMyAnhStk()
            .pipe(finalize(() => {
                this.uploadingStk = false;
            }))
            .subscribe({
                next: () => {
                    this.profile.anhStkUrl = null;
                    this.toast.success('Đã xóa ảnh sao kê!');
                },
                error: (err) => {
                    console.error('[HoSoComponent] Delete STK error:', err);
                    this.toast.danger('Không thể xóa ảnh. Vui lòng thử lại!');
                },
            });
    }

    openStkImageModal(): void {
        this.showStkImageModal = true;
    }

    closeStkImageModal(): void {
        this.showStkImageModal = false;
    }
}
