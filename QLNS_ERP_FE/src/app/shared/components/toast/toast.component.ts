// src/app/shared/components/toast/toast.component.ts

import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription } from 'rxjs';
import { Toast, ToastService, ToastType } from '../../services/toast.service';

@Component({
    selector: 'app-toast',
    templateUrl: './toast.component.html',
    styleUrls: ['./toast.component.scss'],
})
export class ToastComponent implements OnInit, OnDestroy {
    toasts: Toast[] = [];
    private sub: Subscription | null = null;

    constructor(private toastService: ToastService) { }

    ngOnInit(): void {
        this.sub = this.toastService.toast$.subscribe((toast) => {
            this.toasts.push(toast);

            // Auto remove after duration
            setTimeout(() => {
                this.remove(toast.id);
            }, toast.duration);
        });
    }

    ngOnDestroy(): void {
        if (this.sub) {
            this.sub.unsubscribe();
        }
    }

    remove(id: number): void {
        this.toasts = this.toasts.filter((t) => t.id !== id);
    }

    getIcon(type: ToastType): string {
        switch (type) {
            case 'success':
                return 'bi-check-circle-fill';
            case 'danger':
                return 'bi-x-circle-fill';
            case 'warning':
                return 'bi-exclamation-triangle-fill';
            case 'info':
                return 'bi-info-circle-fill';
            default:
                return 'bi-info-circle-fill';
        }
    }

    getBgClass(type: ToastType): string {
        switch (type) {
            case 'success':
                return 'text-bg-success';
            case 'danger':
                return 'text-bg-danger';
            case 'warning':
                return 'text-bg-warning';
            case 'info':
                return 'text-bg-info';
            default:
                return 'text-bg-secondary';
        }
    }

    trackById(index: number, toast: Toast): number {
        return toast.id;
    }
}
