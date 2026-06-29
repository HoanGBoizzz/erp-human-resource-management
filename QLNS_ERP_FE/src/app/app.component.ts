import { Component, OnInit } from '@angular/core';
import { AuthService } from './core/services/auth.service';
import { MeApiService } from './core/services/api/me-api.service';
import { UserAvatarService } from './core/services/user-avatar.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'QLNS_ERP_FE';

  constructor(
    private auth: AuthService,
    private meApi: MeApiService,
    private avatarService: UserAvatarService
  ) { }

  ngOnInit(): void {
    // Khởi tạo avatar ngay khi app load (cho tất cả roles)
    if (this.auth.isLoggedIn) {
      this.meApi.getMyProfile().subscribe({
        next: (profile) => {
          if (profile?.anhCaNhanUrl) {
            this.avatarService.updateAvatar(profile.anhCaNhanUrl);
          }
        },
        error: () => { } // silent — nếu API lỗi thì giữ localStorage cũ
      });
    }
  }
}
