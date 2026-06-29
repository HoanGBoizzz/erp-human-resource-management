import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { UserAvatarService } from 'src/app/core/services/user-avatar.service';
import { SignalrService, ThongBaoDto } from 'src/app/core/services/signalr.service';
import { ThongBaoApiService } from 'src/app/core/services/api/thong-bao-api.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
})
export class TopbarComponent implements OnInit, OnDestroy {
  user = this.auth.currentUser;
  avatarUrl: string | null = null;
  showProfileSidebar = false;

  // Notification state
  showNotificationDropdown = false;
  notifications: ThongBaoDto[] = [];
  unreadCount = 0;

  private avatarSub?: Subscription;
  private notificationSub?: Subscription;
  private unreadSub?: Subscription;

  constructor(
    private auth: AuthService,
    private avatarService: UserAvatarService,
    private signalrService: SignalrService,
    private thongBaoApi: ThongBaoApiService,
    private router: Router
  ) { }

  ngOnInit(): void {
    // Subscribe để nhận avatar updates
    this.avatarSub = this.avatarService.avatar$.subscribe(url => {
      console.log('[TopbarComponent] Avatar updated:', url);
      this.avatarUrl = url;
    });

    // Subscribe to notifications
    this.notificationSub = this.thongBaoApi.notifications$.subscribe(notifications => {
      console.log('[TopbarComponent] Notifications updated:', notifications.length);
      this.notifications = notifications;
    });

    // Subscribe to unread count
    this.unreadSub = this.signalrService.unreadCount$.subscribe(count => {
      console.log('[TopbarComponent] Unread count updated:', count);
      this.unreadCount = count;
    });

    // Load initial notifications
    console.log('[TopbarComponent] Refreshing notifications...');
    this.thongBaoApi.refresh();
  }

  ngOnDestroy(): void {
    this.avatarSub?.unsubscribe();
    this.notificationSub?.unsubscribe();
    this.unreadSub?.unsubscribe();
  }

  /** Toggle notification dropdown */
  toggleNotifications(): void {
    this.showNotificationDropdown = !this.showNotificationDropdown;
    if (this.showNotificationDropdown) {
      this.thongBaoApi.getUnread().subscribe();
    }
  }

  /** Click outside to close dropdown */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.notification-wrapper')) {
      this.showNotificationDropdown = false;
    }
  }

  /** Click notification - mark as read and navigate */
  onNotificationClick(notification: ThongBaoDto): void {
    this.thongBaoApi.markAsRead(notification.id).subscribe(() => {
      if (notification.link) {
        this.router.navigateByUrl(notification.link);
      }
      this.showNotificationDropdown = false;
    });
  }

  /** Mark all as read */
  markAllAsRead(): void {
    this.thongBaoApi.markAllAsRead().subscribe();
  }

  /** Format time ago */
  formatTimeAgo(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Vừa xong';
    if (diffMins < 60) return `${diffMins} phút trước`;

    const diffHours = Math.floor(diffMins / 60);
    if (diffHours < 24) return `${diffHours} giờ trước`;

    const diffDays = Math.floor(diffHours / 24);
    return `${diffDays} ngày trước`;
  }

  /** Lấy chữ cái đầu của tên để hiển thị khi không có avatar */
  getInitials(): string {
    const name = this.user?.username || '';
    if (!name) return '?';
    return name.charAt(0).toUpperCase();
  }

  /** Mở profile sidebar */
  openProfileSidebar(): void {
    this.showProfileSidebar = true;
  }

  /** Đóng profile sidebar */
  closeProfileSidebar(): void {
    this.showProfileSidebar = false;
  }

  /** Đăng xuất */
  logout(): void {
    this.auth.logout();
  }
}
