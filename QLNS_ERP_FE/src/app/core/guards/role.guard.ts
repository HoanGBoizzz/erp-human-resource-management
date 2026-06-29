import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { RoleCode } from 'src/app/shared/enums/role.enum';

@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
  constructor(private auth: AuthService, private router: Router) { }

  canActivate(route: ActivatedRouteSnapshot): boolean | UrlTree {
    const allowed = (route.data['roles'] as RoleCode[] | undefined) ?? [];
    const role = this.auth.role;

    if (!role) return this.router.parseUrl('/auth/login');
    if (allowed.length === 0) return true;

    if (allowed.includes(role)) return true;

    // Không đúng role -> đá về dashboard đúng role
    this.auth.navigateByRole(role);
    return false;
  }
}
