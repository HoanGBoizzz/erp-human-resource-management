import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthLayoutComponent } from './core/layout/auth-layout/auth-layout.component';
import { MainLayoutComponent } from './core/layout/main-layout/main-layout.component';
import { AuthGuard } from './core/guards/auth.guard';
import { RoleGuard } from './core/guards/role.guard';
import { RoleCode } from './shared/enums/role.enum';
import { AttendanceKioskComponent } from './features/attendance-kiosk/attendance-kiosk.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auth/login' },

  // Public attendance kiosk (no authentication required)
  { path: 'kiosk', component: AttendanceKioskComponent },

  {
    path: 'auth',
    component: AuthLayoutComponent,
    children: [
      {
        path: '',
        loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule),
      },
    ],
  },

  {
    path: '',
    component: MainLayoutComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'employee',
        canActivate: [RoleGuard],
        data: { roles: [RoleCode.EMPLOYEE] },
        loadChildren: () => import('./features/employee/employee.module').then(m => m.EmployeeModule),
      },
      {
        path: 'hr',
        canActivate: [RoleGuard],
        data: { roles: [RoleCode.HR_ACC] },
        loadChildren: () => import('./features/hr/hr.module').then(m => m.HrModule),
      },
      {
        path: 'gd',
        canActivate: [RoleGuard],
        data: { roles: [RoleCode.GIAM_DOC, RoleCode.SUPER_ADMIN] },
        loadChildren: () => import('./features/gd/gd.module').then(m => m.GdModule),
      },
      {
        path: 'admin',
        canActivate: [RoleGuard],
        data: { roles: [RoleCode.GIAM_DOC, RoleCode.HR_ACC] },
        loadChildren: () => import('./features/admin/admin.module').then(m => m.AdminModule),
      },
    ],
  },

  {
    path: '**',
    loadChildren: () => import('./features/not-found/not-found.module').then(m => m.NotFoundModule),
  },
];

@NgModule({
  imports: [RouterModule.forRoot(routes, { scrollPositionRestoration: 'top' })],
  exports: [RouterModule],
})
export class AppRoutingModule { }
