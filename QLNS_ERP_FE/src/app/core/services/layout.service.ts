import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {
  private _sidebarCollapsed = new BehaviorSubject<boolean>(false);
  sidebarCollapsed$ = this._sidebarCollapsed.asObservable();

  constructor() {
    // Load saved state from localStorage
    const saved = localStorage.getItem('sidebarCollapsed');
    if (saved !== null) {
      this._sidebarCollapsed.next(saved === 'true');
    }
  }

  get isCollapsed(): boolean {
    return this._sidebarCollapsed.value;
  }

  toggleSidebar(): void {
    const newState = !this._sidebarCollapsed.value;
    this._sidebarCollapsed.next(newState);
    localStorage.setItem('sidebarCollapsed', String(newState));
  }

  setSidebarCollapsed(collapsed: boolean): void {
    this._sidebarCollapsed.next(collapsed);
    localStorage.setItem('sidebarCollapsed', String(collapsed));
  }
}
