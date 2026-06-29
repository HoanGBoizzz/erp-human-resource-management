import { Component, OnInit, OnDestroy } from '@angular/core';
import {
    Router,
    NavigationStart,
    NavigationEnd,
    NavigationCancel,
    NavigationError,
} from '@angular/router';
import { Subject } from 'rxjs';
import { takeUntil, filter } from 'rxjs/operators';
import {
    trigger,
    state,
    style,
    transition,
    animate,
} from '@angular/animations';

@Component({
    selector: 'app-route-loading',
    templateUrl: './route-loading.component.html',
    styleUrls: ['./route-loading.component.scss'],
    animations: [
        trigger('fadeInOut', [
            state('void', style({ opacity: 0 })),
            state('*', style({ opacity: 1 })),
            transition('void => *', animate('200ms ease-in')),
            transition('* => void', animate('300ms ease-out')),
        ]),
        trigger('slideIn', [
            state('void', style({ transform: 'translateY(-20px)', opacity: 0 })),
            state('*', style({ transform: 'translateY(0)', opacity: 1 })),
            transition('void => *', animate('300ms 100ms ease-out')),
            transition('* => void', animate('200ms ease-in')),
        ]),
    ],
})
export class RouteLoadingComponent implements OnInit, OnDestroy {
    isLoading = false;
    private destroy$ = new Subject<void>();
    private loadingStartTime = 0;
    private hideTimer?: ReturnType<typeof setTimeout>;
    private pendingNavigations = 0;
    private readonly MIN_LOADING_TIME = 1500; // 1.5 giây
    private readonly MAX_LOADING_TIME = 8000; // auto-hide safety net

    constructor(private router: Router) { }

    ngOnInit(): void {
        this.router.events
            .pipe(
                takeUntil(this.destroy$),
                filter(
                    (event) =>
                        event instanceof NavigationStart ||
                        event instanceof NavigationEnd ||
                        event instanceof NavigationCancel ||
                        event instanceof NavigationError
                )
            )
            .subscribe((event) => {
                if (event instanceof NavigationStart) {
                    // Start loader on first navigation
                    if (this.pendingNavigations === 0) {
                        this.loadingStartTime = Date.now();
                        this.isLoading = true;
                        this.startMaxTimer();
                    }
                    this.pendingNavigations += 1;
                } else {
                    // Đảm bảo loading hiển thị tối thiểu 1.5 giây và không kẹt nếu NavigationEnd không đến
                    if (this.pendingNavigations > 0) {
                        this.pendingNavigations -= 1;
                    }

                    const elapsedTime = Date.now() - this.loadingStartTime;
                    const remainingTime = Math.max(0, this.MIN_LOADING_TIME - elapsedTime);

                    this.clearHideTimer();
                    this.hideTimer = setTimeout(() => {
                        if (this.pendingNavigations === 0) {
                            this.isLoading = false;
                        }
                    }, remainingTime);
                }
            });
    }

    ngOnDestroy(): void {
        this.clearHideTimer();
        this.destroy$.next();
        this.destroy$.complete();
    }

    private startMaxTimer(): void {
        this.clearHideTimer();
        this.hideTimer = setTimeout(() => {
            this.pendingNavigations = 0;
            this.isLoading = false;
        }, this.MAX_LOADING_TIME);
    }

    private clearHideTimer(): void {
        if (this.hideTimer) {
            clearTimeout(this.hideTimer);
            this.hideTimer = undefined;
        }
    }
}
