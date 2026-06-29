import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface NotificationCounts {
    // GIAM_DOC
    bangLuongChoDuyet: number;
    deXuatChoDuyet: number;
    duAnChoDuyet: number;
    dieuChuyenChoDuyet: number;

    // HR_ACC
    donPhepMoi: number;
    duAnDaDuyet: number;
    dieuChuyenDaXuLy: number;
    bangLuongDaDuyet: number;
    taiKhoanCanhBao: number;
    yeuCauChoDuyet: number;

    // EMPLOYEE
    donPhepDaXuLy: number;
    duAnMoiGan: number;
    taskMoi: number;
    bangCongDaChot: number;
    bangLuongDaTinh: number;
}

@Injectable({
    providedIn: 'root'
})
export class NotificationService {
    private apiUrl = `${environment.apiBaseUrl}/api/notification`;

    private _counts = new BehaviorSubject<NotificationCounts | null>(null);
    counts$ = this._counts.asObservable();

    constructor(private http: HttpClient) {
        // Auto refresh every 12 seconds
        interval(12000).subscribe(() => this.refresh());
    }

    get counts(): NotificationCounts | null {
        return this._counts.value;
    }

    /**
     * Fetch notification counts from API
     */
    refresh(): void {
        this.http.get<NotificationCounts>(`${this.apiUrl}/counts`)
            .pipe(
                tap(counts => this._counts.next(counts)),
                catchError(err => {
                    console.error('Failed to fetch notification counts', err);
                    return [];
                })
            )
            .subscribe();
    }

    /**
     * Clear counts (on logout)
     */
    clear(): void {
        this._counts.next(null);
    }
}
