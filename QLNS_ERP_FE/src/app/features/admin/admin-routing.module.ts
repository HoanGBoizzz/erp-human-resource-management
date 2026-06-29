import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuditLogListComponent } from './pages/audit-log-list/audit-log-list.component';
import { MaterialDemoComponent } from './pages/material-demo/material-demo.component';

const routes: Routes = [
    {
        path: '',
        redirectTo: 'audit-log',
        pathMatch: 'full'
    },
    {
        path: 'audit-log',
        component: AuditLogListComponent
    },
    {
        path: 'material-demo',
        component: MaterialDemoComponent
    }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class AdminRoutingModule { }
