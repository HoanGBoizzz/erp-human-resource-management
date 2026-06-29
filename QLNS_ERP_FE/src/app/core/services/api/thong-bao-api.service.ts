import { Injectable, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap, interval, Subscription } from 'rxjs';
import { environment } from 'src/environments/environment';
import { SignalrService, ThongBaoDto } from '../signalr.service';

@Injectable({
  providedIn: 'root'
})
export class ThongBaoApiService implements OnDestroy {
  private readonly baseUrl = `${environment.apiBaseUrl}/api/thong-bao`;
  private readonly REFRESH_INTERVAL = 5000; // 5 seconds

  // Local state for notifications
  private notificationsSubject = new BehaviorSubject<ThongBaoDto[]>([]);
  notifications$ = this.notificationsSubject.asObservable();

  // Auto-refresh subscription
  private refreshSub?: Subscription;

  constructor(
    private http: HttpClient,
    private signalrService: SignalrService
  ) {
    // Subscribe to new notifications from SignalR
    this.signalrService.notification$.subscribe((notification: ThongBaoDto) => {
      const current = this.notificationsSubject.value;
      // Add new notification to the beginning
      this.notificationsSubject.next([notification, ...current]);
    });

    // Start auto-refresh interval
    this.startAutoRefresh();
  }

  ngOnDestroy(): void {
    this.stopAutoRefresh();
  }

  /** Start auto-refresh interval (5 seconds) */
  private startAutoRefresh(): void {
    this.refreshSub = interval(this.REFRESH_INTERVAL).subscribe(() => {
      console.log('[ThongBaoApi] Auto-refreshing notifications...');
      this.refresh();
    });
  }

  /** Stop auto-refresh interval */
  private stopAutoRefresh(): void {
    this.refreshSub?.unsubscribe();
  }

  /** Get unread notifications */
  getUnread(limit = 20): Observable<ThongBaoDto[]> {
    return this.http.get<ThongBaoDto[]>(`${this.baseUrl}/unread`, { params: { limit } }).pipe(
      tap(notifications => this.notificationsSubject.next(notifications))
    );
  }

  /** Get all notifications (paginated) */
  getAll(page = 1, pageSize = 20): Observable<ThongBaoDto[]> {
    return this.http.get<ThongBaoDto[]>(this.baseUrl, { params: { page, pageSize } });
  }

  /** Count unread notifications */
  countUnread(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.baseUrl}/count`).pipe(
      tap(res => this.signalrService.updateUnreadCount(res.count))
    );
  }

  /** Mark a notification as read */
  markAsRead(id: number): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.baseUrl}/${id}/read`, {}).pipe(
      tap(() => {
        // Remove from local list
        const current = this.notificationsSubject.value;
        this.notificationsSubject.next(current.filter(n => n.id !== id));
      })
    );
  }

  /** Mark all notifications as read */
  markAllAsRead(): Observable<{ message: string; count: number }> {
    return this.http.post<{ message: string; count: number }>(`${this.baseUrl}/read-all`, {}).pipe(
      tap(() => {
        this.notificationsSubject.next([]);
        this.signalrService.updateUnreadCount(0);
      })
    );
  }

  /** Mark notifications related to entity as read (when user navigates to detail page) */
  markAsReadByEntity(entityType: string, entityId: number): Observable<{ message: string; count: number }> {
    return this.http.post<{ message: string; count: number }>(
      `${this.baseUrl}/mark-read-by-entity`,
      {},
      { params: { entityType, entityId } }
    ).pipe(
      tap(res => {
        // Remove from local list
        const current = this.notificationsSubject.value;
        this.notificationsSubject.next(current.filter(n => 
          !(n.relatedEntity === entityType && n.relatedId === entityId)
        ));
        // Unread count will be updated via SignalR
      })
    );
  }

  /** Refresh notifications */
  refresh(): void {
    this.getUnread().subscribe();
    this.countUnread().subscribe();
  }
}
