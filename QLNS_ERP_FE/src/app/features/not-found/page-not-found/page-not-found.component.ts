import { Component } from '@angular/core';
import { AuthService } from 'src/app/core/services/auth.service';
import { RoleCode } from 'src/app/shared/enums/role.enum';

@Component({
  selector: 'app-page-not-found',
  templateUrl: './page-not-found.component.html',
  styleUrls: ['./page-not-found.component.scss']
})
export class PageNotFoundComponent {
  constructor(private authService: AuthService) { }

  goHome(): void {
    const role = this.authService.role;
    if (role) {
      this.authService.navigateByRole(role);
    } else {
      this.authService.logout();
    }
  }
}
