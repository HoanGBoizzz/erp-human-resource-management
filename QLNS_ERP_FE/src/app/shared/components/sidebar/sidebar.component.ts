import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { AuthService } from 'src/app/core/services/auth.service';
import { LayoutService } from 'src/app/core/services/layout.service';
import { NotificationService, NotificationCounts } from 'src/app/core/services/notification.service';
import { ThongBaoApiService } from 'src/app/core/services/api/thong-bao-api.service';
import { ThongBaoDto } from 'src/app/core/services/signalr.service';
import { RoleCode } from '../../enums/role.enum';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.scss'],
  animations: [
    trigger('slideDown', [
      state('closed', style({
        height: '0',
        opacity: '0',
        overflow: 'hidden'
      })),
      state('open', style({
        height: '*',
        opacity: '1',
        overflow: 'hidden'
      })),
      transition('closed <=> open', animate('300ms cubic-bezier(0.4, 0, 0.2, 1)'))
    ])
  ]
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Input() role!: RoleCode;

  accountMenuExpanded = false;
  nhanVienMenuExpanded = false;
  chamCongMenuExpanded = false;
  bangLuongMenuExpanded = false;
  noiLamViecMenuExpanded = false;
  deXuatMenuExpanded = false;
  counts: NotificationCounts | null = null;
  notifications: ThongBaoDto[] = [];

  private countsSub?: Subscription;
  private notificationsSub?: Subscription;

  constructor(
    private router: Router,
    private auth: AuthService,
    public layoutService: LayoutService,
    private notificationService: NotificationService,
    private thongBaoApi: ThongBaoApiService
  ) { }

  get isCollapsed(): boolean {
    return this.layoutService.isCollapsed;
  }

  ngOnInit(): void {
    if (this.router.url.includes('/hr/tai-khoan') || this.router.url.includes('/hr/canh-bao-tai-khoan')) {
      this.accountMenuExpanded = true;
    }
    if (this.router.url.includes('/hr/nhan-vien') || this.router.url.includes('/hr/phong-ban')) {
      this.nhanVienMenuExpanded = true;
    }
    if (this.router.url.includes('/hr/cham-cong') || this.router.url.includes('/hr/face-registration')) {
      this.chamCongMenuExpanded = true;
    }
    if (this.router.url.includes('/hr/bang-luong') || this.router.url.includes('/hr/luong-co-ban') ||
      this.router.url.includes('/hr/phu-cap') || this.router.url.includes('/hr/tinh-luong') ||
      this.router.url.includes('/hr/cau-hinh-luong')) {
      this.bangLuongMenuExpanded = true;
    }
    if (this.router.url.includes('/employee/phieu-de-xuat') || this.router.url.includes('/employee/phieu-tam-ung') ||
      this.router.url.includes('/employee/don-di-muon') || this.router.url.includes('/employee/noi-lam-viec-thong-ke')) {
      this.noiLamViecMenuExpanded = true;
    }
    if (this.router.url.includes('/hr/yeu-cau-noi-lam-viec') || this.router.url.includes('/hr/de-xuat-giam-doc')) {
      this.deXuatMenuExpanded = true;
    }

    // Subscribe to old notification counts
    this.countsSub = this.notificationService.counts$.subscribe(c => this.counts = c);

    // Subscribe to new ThongBao notifications
    this.notificationsSub = this.thongBaoApi.notifications$.subscribe(n => this.notifications = n);

    // Initial fetch
    this.notificationService.refresh();
    this.thongBaoApi.refresh();
  }

  ngOnDestroy(): void {
    this.countsSub?.unsubscribe();
    this.notificationsSub?.unsubscribe();
  }

  logout(): void {
    this.notificationService.clear();
    this.auth.logout();
  }

  isActive(url: string): boolean {
    return this.router.url.startsWith(url);
  }

  navigate(url: string): void {
    this.router.navigate([url]);
  }

  toggleAccountMenu(event: Event): void {
    event.stopPropagation();
    this.accountMenuExpanded = !this.accountMenuExpanded;
  }

  toggleNhanVienMenu(event: Event): void {
    event.stopPropagation();
    this.nhanVienMenuExpanded = !this.nhanVienMenuExpanded;
  }

  toggleChamCongMenu(event: Event): void {
    event.stopPropagation();
    this.chamCongMenuExpanded = !this.chamCongMenuExpanded;
  }

  toggleBangLuongMenu(event: Event): void {
    event.stopPropagation();
    this.bangLuongMenuExpanded = !this.bangLuongMenuExpanded;
  }

  toggleNoiLamViecMenu(event: Event): void {
    event.stopPropagation();
    this.noiLamViecMenuExpanded = !this.noiLamViecMenuExpanded;
  }

  toggleDeXuatMenu(event: Event): void {
    event.stopPropagation();
    this.deXuatMenuExpanded = !this.deXuatMenuExpanded;
  }

  toggleSidebar(): void {
    this.layoutService.toggleSidebar();
  }

  /**
   * Get notification count for specific entity type
   * Used for sidebar badges
   */
  getNotificationCount(entityType: string): number {
    return this.notifications.filter(n =>
      n.relatedEntity === entityType && !n.isRead
    ).length;
  }
}
