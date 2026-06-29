import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subject, Subscription, interval } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

export interface ThongBaoDto {
  id: number;
  title: string;
  message: string | null;
  type: string;
  relatedEntity: string | null;
  relatedId: number | null;
  link: string | null;
  isRead: boolean;
  createdAt: string;
  senderName: string | null;
}

export interface EntityUpdateDto {
  entityType: string;
  entityId: number;
  action: string;
  data: any;
  timestamp: string;
}

@Injectable({
  providedIn: 'root'
})
export class SignalrService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private readonly hubUrl = 'http://localhost:5042/hubs/notification';
  private checkSub?: Subscription;

  // Observables
  private notificationSubject = new Subject<ThongBaoDto>();
  private unreadCountSubject = new BehaviorSubject<number>(0);
  private connectionStateSubject = new BehaviorSubject<boolean>(false);
  private entityUpdateSubject = new Subject<EntityUpdateDto>();

  notification$ = this.notificationSubject.asObservable();
  unreadCount$ = this.unreadCountSubject.asObservable();
  isConnected$ = this.connectionStateSubject.asObservable();
  entityUpdate$ = this.entityUpdateSubject.asObservable();

  constructor(private authService: AuthService) {
    // Check every 5 seconds if we should connect
    this.checkSub = interval(5000).subscribe(() => {
      const user = this.authService.currentUser;
      if (user && user.accessToken) {
        if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
          this.startConnection();
        }
      } else {
        this.stopConnection();
      }
    });

    // Initial connection attempt after 1 second
    setTimeout(() => this.startConnection(), 1000);
  }

  ngOnDestroy(): void {
    this.checkSub?.unsubscribe();
    this.stopConnection();
  }

  /** Get token from AuthService */
  private getToken(): string | null {
    return this.authService.currentUser?.accessToken ?? null;
  }

  /** Start SignalR connection */
  async startConnection(): Promise<void> {
    if (this.hubConnection?.state === signalR.HubConnectionState.Connected) {
      return;
    }

    const token = this.getToken();
    if (!token) {
      return;
    }

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.registerHandlers();

    try {
      await this.hubConnection.start();
      console.log('[SignalR] Connected successfully');
      this.connectionStateSubject.next(true);
    } catch (err) {
      console.error('[SignalR] Connection failed:', err);
      this.connectionStateSubject.next(false);
    }
  }

  /** Stop SignalR connection */
  async stopConnection(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
        console.log('[SignalR] Disconnected');
      } catch (err) {
        console.error('[SignalR] Disconnect error:', err);
      }
      this.hubConnection = null;
      this.connectionStateSubject.next(false);
    }
  }

  /** Register event handlers from server */
  private registerHandlers(): void {
    if (!this.hubConnection) return;

    // Receive new notification
    this.hubConnection.on('ReceiveNotification', (notification: ThongBaoDto) => {
      console.log('[SignalR] New notification:', notification);
      this.notificationSubject.next(notification);
    });

    // Update unread count
    this.hubConnection.on('UpdateUnreadCount', (count: number) => {
      console.log('[SignalR] Unread count updated:', count);
      this.unreadCountSubject.next(count);
    });

    // Entity updated (for realtime list refresh)
    this.hubConnection.on('EntityUpdated', (update: EntityUpdateDto) => {
      console.log('[SignalR] Entity updated:', update);
      this.entityUpdateSubject.next(update);
    });

    // Connection events
    this.hubConnection.onreconnecting(() => {
      console.log('[SignalR] Reconnecting...');
      this.connectionStateSubject.next(false);
    });

    this.hubConnection.onreconnected(() => {
      console.log('[SignalR] Reconnected');
      this.connectionStateSubject.next(true);
    });

    this.hubConnection.onclose(() => {
      console.log('[SignalR] Connection closed');
      this.connectionStateSubject.next(false);
    });
  }

  /** Manually update unread count */
  updateUnreadCount(count: number): void {
    this.unreadCountSubject.next(count);
  }
}

