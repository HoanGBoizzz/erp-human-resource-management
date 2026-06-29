import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from 'src/environments/environment';

const AVATAR_STORAGE_KEY = 'qlns_user_avatar';

@Injectable({ providedIn: 'root' })
export class UserAvatarService {
    private avatarUrl$ = new BehaviorSubject<string | null>(this.loadFromStorage());

    /** Observable để subscribe */
    get avatar$() {
        return this.avatarUrl$.asObservable();
    }

    /** Lấy giá trị hiện tại */
    get currentAvatar(): string | null {
        return this.avatarUrl$.value;
    }

    /**
     * Xử lý avatar URL - nếu là relative path thì thêm base URL
     */
    getFullAvatarUrl(url: string | null | undefined): string | null {
        if (!url) return null;
        // Nếu đã là full URL (bắt đầu bằng http:// hoặc https://)
        if (url.startsWith('http://') || url.startsWith('https://')) {
            return url;
        }
        // Nếu là relative path, thêm base URL
        return `${environment.apiBaseUrl}${url.startsWith('/') ? '' : '/'}${url}`;
    }

    /** Cập nhật avatar mới */
    updateAvatar(url: string | null): void {
        const fullUrl = this.getFullAvatarUrl(url);
        console.log('[UserAvatarService] Updating avatar:', url, '-> Full URL:', fullUrl);
        // Lưu clean URL (không có timestamp) vào storage để có thể load lại sau
        this.saveToStorage(url);
        // Emit URL với timestamp để force browser reload ảnh mới (tránh cache cũ)
        const displayUrl = fullUrl ? `${fullUrl.split('?')[0]}?t=${Date.now()}` : null;
        this.avatarUrl$.next(displayUrl);
    }

    /** Clear avatar (khi logout) */
    clearAvatar(): void {
        localStorage.removeItem(AVATAR_STORAGE_KEY);
        this.avatarUrl$.next(null);
    }

    private saveToStorage(url: string | null): void {
        if (url) {
            localStorage.setItem(AVATAR_STORAGE_KEY, url);
        } else {
            localStorage.removeItem(AVATAR_STORAGE_KEY);
        }
    }

    private loadFromStorage(): string | null {
        const stored = localStorage.getItem(AVATAR_STORAGE_KEY);
        if (!stored) return null;
        const fullUrl = this.getFullAvatarUrl(stored);
        // Thêm timestamp khi load từ storage để tránh browser cache
        return fullUrl ? `${fullUrl.split('?')[0]}?t=${Date.now()}` : null;
    }
}
