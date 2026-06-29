// src/app/shared/services/toast.service.ts

import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export type ToastType = 'success' | 'danger' | 'warning' | 'info';

export interface Toast {
    id: number;
    message: string;
    type: ToastType;
    duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
    private toastSubject = new Subject<Toast>();
    private idCounter = 0;

    toast$ = this.toastSubject.asObservable();

    show(message: string, type: ToastType = 'info', duration: number = 3500): void {
        const toast: Toast = {
            id: ++this.idCounter,
            message,
            type,
            duration,
        };
        this.toastSubject.next(toast);
    }

    success(message: string, duration: number = 3500): void {
        this.show(message, 'success', duration);
    }

    danger(message: string, duration: number = 3500): void {
        this.show(message, 'danger', duration);
    }

    warning(message: string, duration: number = 3500): void {
        this.show(message, 'warning', duration);
    }

    info(message: string, duration: number = 3500): void {
        this.show(message, 'info', duration);
    }
}
