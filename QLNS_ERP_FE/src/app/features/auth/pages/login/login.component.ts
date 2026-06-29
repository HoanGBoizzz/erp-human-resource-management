import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AuthService } from 'src/app/core/services/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
})
export class LoginComponent {
  loading = false;
  errorMsg = '';

  form = this.fb.group({
    username: ['', [Validators.required]],
    password: ['', [Validators.required]],
    remember: [true],
  });

  showPassword = false;
  imageLoaded = false;

  constructor(private fb: FormBuilder, private auth: AuthService) { }

  showPasswordStart(): void {
    this.showPassword = true;
  }

  showPasswordEnd(): void {
    this.showPassword = false;
  }

  onImageLoad(event: Event): void {
    this.imageLoaded = true;
    const img = event.target as HTMLImageElement;
    img.style.opacity = '1';
  }

  onImageError(event: Event): void {
    this.imageLoaded = false;
    const img = event.target as HTMLImageElement;
    img.style.display = 'none';
  }

  submit(): void {
    this.errorMsg = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;

    const payload = {
      username: this.form.value.username!.trim(),
      password: this.form.value.password!,
    };

    this.auth.login(payload).subscribe({
      next: (user) => {
        this.loading = false;
        // TODO: nếu remember=false thì chuyển sang sessionStorage (tuỳ bạn)
        this.auth.navigateByRole(user.role);
      },
      error: (err) => {
        this.loading = false;
        // Log chi tiết để debug format error response từ BE
        console.log('[LoginComponent] Full error:', err);
        console.log('[LoginComponent] Error status:', err?.status);
        console.log('[LoginComponent] Error body (err.error):', err?.error);
        console.log('[LoginComponent] Type of err.error:', typeof err?.error);

        // Xử lý error response từ BE
        const errorBody = err?.error;

        if (typeof errorBody === 'string') {
          // Nếu BE trả về string trực tiếp
          console.log('[LoginComponent] Error is string:', errorBody);
          this.errorMsg = errorBody || 'Đăng nhập thất bại. Vui lòng kiểm tra lại.';
        } else if (errorBody && typeof errorBody === 'object') {
          // Nếu BE trả về object { message: "..." }
          console.log('[LoginComponent] Error is object, message:', errorBody.message);
          this.errorMsg = errorBody.message || errorBody.detail || errorBody.title || 'Đăng nhập thất bại. Vui lòng kiểm tra lại.';
        } else {
          this.errorMsg = 'Đăng nhập thất bại. Vui lòng kiểm tra lại.';
        }

        console.log('[LoginComponent] Final errorMsg:', this.errorMsg);
      },
    });
  }
}
