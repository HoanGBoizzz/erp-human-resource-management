# HỆ THỐNG ĐĂNG NHẬP & PHÂN QUYỀN – TÀI LIỆU CHI TIẾT

> **Mục tiêu:** Mô tả toàn bộ cơ chế xác thực (Authentication) và phân quyền (Authorization) trong hệ thống QLNS ERP, từ lúc người dùng nhập mật khẩu đến khi truy cập được tài nguyên. Dành cho người chưa biết gì về dự án này.

---

## MỤC LỤC

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Các thành phần dữ liệu](#2-các-thành-phần-dữ-liệu)
3. [Hệ thống vai trò (Roles)](#3-hệ-thống-vai-trò-roles)
4. [Bảo mật mật khẩu](#4-bảo-mật-mật-khẩu)
5. [Luồng đăng nhập chi tiết](#5-luồng-đăng-nhập-chi-tiết)
6. [JWT Token – Cơ chế xác thực](#6-jwt-token--cơ-chế-xác-thực)
7. [Cơ chế bảo vệ tài khoản](#7-cơ-chế-bảo-vệ-tài-khoản)
8. [Phân quyền Backend](#8-phân-quyền-backend)
9. [Phân quyền Frontend (Angular)](#9-phân-quyền-frontend-angular)
10. [Quản lý tài khoản (CRUD)](#10-quản-lý-tài-khoản-crud)
11. [Quản lý vai trò (CRUD)](#11-quản-lý-vai-trò-crud)
12. [Hệ thống phân quyền chi tiết (VaiTroChucNang)](#12-hệ-thống-phân-quyền-chi-tiết-vaitrochucnang)
13. [Mật khẩu tạm (MatKhauTam)](#13-mật-khẩu-tạm-matkhautam)
14. [Đổi mật khẩu](#14-đổi-mật-khẩu)
15. [API Endpoints](#15-api-endpoints)
16. [Sơ đồ tổng thể](#16-sơ-đồ-tổng-thể)
17. [Câu hỏi thường gặp (FAQ)](#17-câu-hỏi-thường-gặp-faq)

---

## 1. Tổng quan kiến trúc

Hệ thống dùng mô hình **JWT Bearer Token** kết hợp **Role-Based Access Control (RBAC)**:

```
[Trình duyệt / Angular App]
        │
        │  1. POST /api/auth/login {username, password}
        ▼
[Backend ASP.NET Core]
        │
        │  2. Xác minh mật khẩu (PBKDF2)
        │  3. Tạo JWT Access Token
        │  4. Trả về { accessToken, refreshToken, role, ... }
        │
        ▼
[Frontend lưu token vào localStorage]
        │
        │  5. Mọi request tiếp theo đính kèm header:
        │     Authorization: Bearer <accessToken>
        ▼
[Backend xác thực token, kiểm tra role → cho phép / từ chối]
```

**Nguyên tắc cốt lõi:**
- **Authentication** (Xác thực): Bạn là ai? → JWT Token chứng minh danh tính.
- **Authorization** (Phân quyền): Bạn được làm gì? → Role quyết định quyền truy cập.

---

## 2. Các thành phần dữ liệu

### 2.1 Bảng `TaiKhoan` – Tài khoản người dùng

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `TenDangNhap` | string | Tên đăng nhập (username), duy nhất trong hệ thống |
| `MatKhauHash` | string | Mật khẩu đã băm bằng PBKDF2 (Base64) |
| `MatKhauSalt` | string? | Salt ngẫu nhiên dùng để băm (Base64) |
| `NvHoSoId` | int? | Liên kết hồ sơ nhân viên (có thể null nếu không phải NV) |
| `VaiTroId` | int | Vai trò của tài khoản (FK → VaiTro) |
| `TrangThai` | bool | `true` = đang hoạt động, `false` = bị vô hiệu hóa |
| `LanDangNhapCuoi` | DateTime? | Thời điểm đăng nhập gần nhất (null = chưa đăng nhập lần nào) |
| `SoLanDangNhapSai` | int | Đếm số lần nhập sai liên tiếp (reset khi đăng nhập đúng) |
| `ThoiGianKhoa` | DateTime? | Thời điểm tài khoản bị khóa tạm thời (do nhập sai nhiều lần) |
| `TrangThaiCanhBao` | string | `BINH_THUONG` / `CANH_BAO` / `CAM` |
| `LyDoCanhBao` | string? | Lý do bị cảnh báo/cấm |
| `TaiKhoanCanhBaoBoiId` | int? | Tài khoản HR đã thực hiện cảnh báo |
| `NgayCanhBao` | DateTime? | Ngày bị cảnh báo |
| `MatKhauTam` | string? | Mật khẩu tạm thời dạng plaintext, bị xóa sau lần đăng nhập đầu tiên |
| `CreatedAt` | DateTime | Ngày tạo tài khoản |
| `UpdatedAt` | DateTime | Ngày cập nhật gần nhất |

---

### 2.2 Bảng `VaiTro` – Vai trò (Role)

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `MaVaiTro` | string | Mã vai trò: `EMPLOYEE`, `HR_ACC`, `GIAM_DOC`, `SUPER_ADMIN` |
| `TenVaiTro` | string | Tên hiển thị (VD: "Nhân viên", "Kế toán lương") |
| `MoTa` | string? | Mô tả vai trò |
| `MucDoUuTien` | int | Cấp độ ưu tiên (số càng cao = càng có quyền) |
| `TrangThai` | bool | Có đang sử dụng không |

---

### 2.3 Bảng `ChucNang` – Chức năng hệ thống

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `MaChucNang` | string | Mã chức năng (VD: `QUAN_LY_LUONG`) |
| `TenChucNang` | string | Tên chức năng |
| `DuongDan` | string? | Đường dẫn URL tương ứng |
| `Nhom` | string? | Nhóm chức năng (VD: HR, ADMIN...) |
| `ThuTuHienThi` | int | Thứ tự hiển thị trong menu |

---

### 2.4 Bảng `VaiTroChucNang` – Bảng phân quyền chi tiết (junction table)

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `VaiTroId` | int | FK → VaiTro |
| `ChucNangId` | int | FK → ChucNang |
| `QuyenXem` | bool | Được xem |
| `QuyenThem` | bool | Được thêm mới |
| `QuyenSua` | bool | Được sửa |
| `QuyenXoa` | bool | Được xóa |
| `QuyenDuyet` | bool | Được duyệt |

---

## 3. Hệ thống vai trò (Roles)

Hệ thống có **4 vai trò** được định nghĩa cứng:

| Mã vai trò | Tên | Mô tả quyền hạn |
|---|---|---|
| `EMPLOYEE` | Nhân viên | Chỉ xem dữ liệu của bản thân: chấm công, lương, thông tin cá nhân |
| `HR_ACC` | Kế toán / HR | Quản lý nhân sự, chấm công, tính lương, quản lý tài khoản |
| `GIAM_DOC` | Giám đốc | Xem báo cáo tổng quan, duyệt lương, duyệt đề xuất, xem toàn bộ |
| `SUPER_ADMIN` | Quản trị hệ thống | Toàn quyền, bao gồm cả cấu hình hệ thống |

### Phân quyền theo vai trò (tổng hợp)

| Chức năng | EMPLOYEE | HR_ACC | GIAM_DOC | SUPER_ADMIN |
|---|:---:|:---:|:---:|:---:|
| Xem lương bản thân | ✅ | ✅ | ✅ | ✅ |
| Xem chấm công bản thân | ✅ | ✅ | ✅ | ✅ |
| Tính lương cho NV | ❌ | ✅ | ❌ | ✅ |
| Gửi duyệt lương | ❌ | ✅ | ❌ | ✅ |
| Duyệt lương | ❌ | ❌ | ✅ | ✅ |
| Quản lý chấm công | ❌ | ✅ | ❌ | ✅ |
| Quản lý tài khoản | ❌ | ✅ | ✅ | ✅ |
| Quản lý vai trò | ❌ | ✅ | ✅ | ✅ |
| Xem báo cáo, dashboard | ❌ | ✅ | ✅ | ✅ |

---

## 4. Bảo mật mật khẩu

### Thuật toán: PBKDF2-HMACSHA256

Hệ thống **KHÔNG** lưu mật khẩu dạng plaintext hay MD5/SHA1 đơn giản. Thay vào đó dùng PBKDF2 (Password-Based Key Derivation Function 2):

```
Đầu vào:  password (plaintext)
          salt      (32 bytes ngẫu nhiên)

Thuật toán: PBKDF2-HMACSHA256
Iterations: 100,000 vòng
Output:     32 bytes key

Lưu DB:   MatKhauHash = Base64(output_32_bytes)
          MatKhauSalt = Base64(salt_32_bytes)
```

### Tại sao an toàn?

| Yếu tố | Giải thích |
|---|---|
| **Salt ngẫu nhiên** | Mỗi tài khoản có salt riêng → 2 người cùng mật khẩu có hash khác nhau → chống rainbow table attack |
| **100,000 iterations** | Tăng thời gian brute-force lên hàng nghìn lần so với hash đơn giản |
| **FixedTimeEquals** | So sánh hash dùng `CryptographicOperations.FixedTimeEquals` → chống timing attack (kẻ tấn công không đo được thời gian so sánh) |

### Quá trình tạo & xác minh mật khẩu

```
── TẠO MẬT KHẨU ──────────────────────────────────────────
1. RandomNumberGenerator.GetBytes(32) → salt
2. Rfc2898DeriveBytes(password, salt, 100_000, SHA256) → hash
3. Lưu: MatKhauHash = Base64(hash), MatKhauSalt = Base64(salt)

── XÁC MINH MẬT KHẨU ─────────────────────────────────────
1. Lấy salt từ DB: salt = Base64Decode(MatKhauSalt)
2. Tính lại:       computed = PBKDF2(input_password, salt, 100_000, SHA256)
3. So sánh:        FixedTimeEquals(computed, Base64Decode(MatKhauHash))
4. Bằng nhau → đúng, khác nhau → sai
```

---

## 5. Luồng đăng nhập chi tiết

### Request:

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "nguyenvana",
  "password": "MyPassword123"
}
```

### Xử lý trong `AuthService.LoginAsync()`:

```
Bước 1: Tìm tài khoản
  SELECT * FROM TaiKhoans WHERE TenDangNhap = 'nguyenvana' AND TrangThai = true
  → Không tìm thấy: ném UnauthorizedAccessException("Tài khoản không tồn tại hoặc đã bị vô hiệu hóa")

Bước 2: Kiểm tra trạng thái cảnh báo
  IF TrangThaiCanhBao == "CAM"
    → lỗi: "Tài khoản của bạn đã bị cấm: {LyDoCanhBao}"

Bước 3: Kiểm tra khóa tạm thời (lockout)
  IF ThoiGianKhoa != null AND ThoiGianKhoa > UtcNow
    → lỗi: "Tài khoản tạm thời bị khóa, vui lòng thử lại sau {phút} phút"

Bước 4: Xác minh mật khẩu
  IF NOT PasswordHasher.Verify(password, MatKhauHash, MatKhauSalt)
    SoLanDangNhapSai++
    IF SoLanDangNhapSai >= 3
      ThoiGianKhoa = UtcNow + 15 phút
    SaveChanges()
    → lỗi: "Sai mật khẩu. Còn {3 - SoLanDangNhapSai} lần thử trước khi bị khóa"

Bước 5: Đăng nhập thành công
  SoLanDangNhapSai = 0
  ThoiGianKhoa = null
  LanDangNhapCuoi = UtcNow
  MatKhauTam = null  (xóa mật khẩu tạm nếu có)
  SaveChanges()

Bước 6: Tạo JWT Access Token
  Claims: sub=TenDangNhap, jti=GUID, userid=Id, role=MaVaiTro, EmployeeId=NvHoSoId
  Ký bằng HMACSHA256 với SecretKey từ cấu hình

Bước 7: Tạo Refresh Token (JWT, 7 ngày)

Bước 8: Trả về response
  {
    "username": "nguyenvana",
    "role": "HR_ACC",
    "accessToken": "eyJ...",
    "refreshToken": "eyJ...",
    "expiresAt": "2026-03-08T09:00:00Z",
    "employeeId": 5
  }
```

### Response thành công:

```json
{
  "username": "nguyenvana",
  "role": "HR_ACC",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-03-08T10:00:00Z",
  "employeeId": 5
}
```

---

## 6. JWT Token – Cơ chế xác thực

### Cấu trúc JWT

JWT gồm 3 phần phân cách bằng dấu `.`:
```
Header.Payload.Signature

Header:    { "alg": "HS256", "typ": "JWT" }
Payload:   { claims... }
Signature: HMACSHA256(base64(header) + "." + base64(payload), secretKey)
```

### Claims trong Access Token

| Claim | Ý nghĩa | Ví dụ |
|---|---|---|
| `sub` | Username | `"nguyenvana"` |
| `jti` | JWT ID (GUID duy nhất, chống replay) | `"a1b2c3..."` |
| `userid` | ID tài khoản trong DB | `"5"` |
| `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` | Mã vai trò | `"HR_ACC"` |
| `EmployeeId` | ID hồ sơ nhân viên (nếu có) | `"12"` |
| `exp` | Thời điểm hết hạn (Unix timestamp) | `1741426800` |

### Cấu hình JWT (`appsettings.json`)

```json
"Jwt": {
  "Issuer": "QLNS_ERP",
  "Audience": "QLNS_ERP_Client",
  "SecretKey": "...(tối thiểu 32 ký tự)...",
  "AccessTokenMinutes": 60,
  "RefreshTokenDays": 7
}
```

### Tham số xác thực token (Backend)

```csharp
ValidateIssuer            = true   // Kiểm tra Issuer
ValidateAudience          = true   // Kiểm tra Audience
ValidateIssuerSigningKey  = true   // Kiểm tra chữ ký
ValidateLifetime          = true   // Kiểm tra hết hạn
ClockSkew                 = Zero   // Không có thời gian grace (token hết hạn = từ chối ngay)
```

### SignalR & WebSocket

Với WebSocket (SignalR), token không thể đặt trong header thông thường. Hệ thống đọc token từ query string:

```
GET /hubs/notification?access_token=eyJ...
```

---

## 7. Cơ chế bảo vệ tài khoản

### 7.1 Khóa tạm thời (Lockout sau đăng nhập sai)

```
Nhập sai lần 1: SoLanDangNhapSai = 1
Nhập sai lần 2: SoLanDangNhapSai = 2
Nhập sai lần 3: SoLanDangNhapSai = 3
               ThoiGianKhoa = UtcNow + 15 phút
               → Tất cả lần thử tiếp theo bị từ chối trong 15 phút

Đăng nhập đúng: SoLanDangNhapSai = 0, ThoiGianKhoa = null (reset)
```

> **Lưu ý:** Lockout là **tự động** và tạm thời. Sau 15 phút tự hết. Nếu cần mở khóa ngay, HR có thể reset mật khẩu.

---

### 7.2 Cảnh báo & Cấm tài khoản (TrangThaiCanhBao)

HR có thể đánh dấu tài khoản ở 3 mức:

| Trạng thái | Ý nghĩa | Ảnh hưởng đến đăng nhập |
|---|---|---|
| `BINH_THUONG` | Bình thường | Không ảnh hưởng |
| `CANH_BAO` | Đã bị cảnh báo vi phạm | Vẫn đăng nhập được (chỉ là nhãn) |
| `CAM` | Bị cấm hoàn toàn | **Không thể đăng nhập** – nhận thông báo lý do |

---

### 7.3 Vô hiệu hóa tài khoản (TrangThai = false)

HR có thể tắt (`TrangThai = false`) một tài khoản → khi đăng nhập nhận lỗi "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa".

---

## 8. Phân quyền Backend

### 8.1 Cơ chế `[Authorize]`

Mọi endpoint (trừ `[AllowAnonymous]`) đều yêu cầu token hợp lệ.

```csharp
// Chỉ cần đăng nhập, không phân biệt role
[Authorize]
public IActionResult SomeEndpoint() { ... }

// Chỉ HR mới truy cập được
[Authorize(Roles = "HR_ACC")]
public IActionResult HrOnlyEndpoint() { ... }

// HR hoặc Giám đốc đều được
[Authorize(Roles = "GIAM_DOC,HR_ACC")]
public IActionResult HrOrDirectorEndpoint() { ... }

// Không cần đăng nhập (endpoint public)
[AllowAnonymous]
[HttpPost("login")]
public IActionResult Login() { ... }
```

### 8.2 Trích xuất thông tin người dùng từ Token trong Controller

```csharp
// Lấy ID tài khoản từ claim "userid"
private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);

// Lấy role từ claim chuẩn
var role = User.FindFirstValue(ClaimTypes.Role);

// Lấy employeeId
var employeeId = User.FindFirstValue("EmployeeId");
```

### 8.3 Phân quyền theo từng Controller

| Controller | Route | Role yêu cầu |
|---|---|---|
| `AuthController` | `/api/auth/login` | Public (AllowAnonymous) |
| `AuthController` | `/api/auth/change-password` | Bất kỳ (Authorize) |
| `AuthController` | `/api/auth/view-password/{id}` | HR_ACC |
| `AccountsController` | `/api/admin/accounts/*` | GIAM_DOC, HR_ACC |
| `RolesController` | `/api/admin/roles/*` | GIAM_DOC, HR_ACC |
| `LuongController` | `/api/luong/tinh` | HR_ACC |
| `LuongController` | `/api/luong/*/duyet` | GIAM_DOC |
| `LuongController` | `/api/luong/me` | EMPLOYEE, HR_ACC, GIAM_DOC |
| `MeController` | `/api/me/*` | EMPLOYEE |

### 8.4 Middleware pipeline

Thứ tự middleware rất quan trọng:

```
Request đến
    │
    ▼
app.UseCors("AllowAngular")     // Kiểm tra CORS trước
    │
    ▼
app.UseStaticFiles()            // Trả về file tĩnh nếu có
    │
    ▼
app.UseAuthentication()         // Đọc & xác thực JWT token
    │
    ▼
app.UseAuthorization()          // Kiểm tra quyền truy cập endpoint
    │
    ▼
app.MapControllers()            // Điều hướng đến Controller
```

> **Quan trọng:** `UseAuthentication()` phải đứng **trước** `UseAuthorization()`. Nếu đổi thứ tự → toàn bộ phân quyền sẽ không hoạt động.

---

## 9. Phân quyền Frontend (Angular)

### 9.1 Lưu trữ thông tin đăng nhập

Sau khi đăng nhập thành công, frontend lưu toàn bộ thông tin vào `localStorage`:

```
localStorage['qlns_auth'] = JSON.stringify({
  username: "nguyenvana",
  role: "HR_ACC",
  accessToken: "eyJ...",
  refreshToken: "eyJ...",
  expiresAt: "2026-03-08T10:00:00Z",
  employeeId: 5
})
```

Khi ứng dụng khởi động, dữ liệu này được đọc lại và phục hồi trạng thái đăng nhập.

---

### 9.2 AuthInterceptor – Tự động gắn token

Mọi HTTP request đều tự động được gắn `Authorization` header:

```typescript
// app/core/interceptors/auth.interceptor.ts
intercept(req, next) {
  const user = this.auth.currentUser;
  if (!user?.accessToken) return next.handle(req);

  const cloned = req.clone({
    setHeaders: { Authorization: `Bearer ${user.accessToken}` }
  });
  return next.handle(cloned);
}
```

Người lập trình **không cần tự gắn token thủ công** trong từng service, interceptor xử lý hết.

---

### 9.3 AuthGuard – Bảo vệ route khi chưa đăng nhập

```typescript
// Áp dụng trong routing:
{ path: 'hr', canActivate: [AuthGuard], component: HrLayoutComponent }

// Logic:
canActivate(): boolean | UrlTree {
  if (this.auth.isLoggedIn) return true;
  return this.router.parseUrl('/auth/login');  // Redirect về login
}
```

Nếu người dùng chưa đăng nhập mà truy cập URL nội bộ → tự động chuyển về `/auth/login`.

---

### 9.4 RoleGuard – Bảo vệ route theo vai trò

```typescript
// Áp dụng trong routing:
{
  path: 'hr/dashboard',
  canActivate: [RoleGuard],
  data: { roles: ['HR_ACC', 'GIAM_DOC'] },
  component: HrDashboardComponent
}

// Logic:
canActivate(route): boolean | UrlTree {
  const allowed = route.data['roles'] ?? [];
  const role = this.auth.role;

  if (!role) return this.router.parseUrl('/auth/login');
  if (allowed.length === 0) return true;         // Mọi role đều được vào
  if (allowed.includes(role)) return true;       // Role hợp lệ

  // Sai role → điều hướng về dashboard của role đó
  this.auth.navigateByRole(role);
  return false;
}
```

---

### 9.5 Điều hướng sau đăng nhập theo Role

Sau khi đăng nhập thành công, Angular tự động điều hướng người dùng đến dashboard tương ứng:

| Role | Đường dẫn |
|---|---|
| `EMPLOYEE` | `/employee/dashboard` |
| `HR_ACC` | `/hr/dashboard` |
| `GIAM_DOC` | `/gd/dashboard` |
| `SUPER_ADMIN` | `/gd/dashboard` |

---

### 9.6 RoleCode Enum (Frontend)

```typescript
export enum RoleCode {
  EMPLOYEE    = 'EMPLOYEE',
  HR_ACC      = 'HR_ACC',
  GIAM_DOC    = 'GIAM_DOC',
  SUPER_ADMIN = 'SUPER_ADMIN',
}
```

---

## 10. Quản lý tài khoản (CRUD)

Route: `GET|POST|PUT|DELETE /api/admin/accounts`  
Phân quyền: **HR_ACC, GIAM_DOC**

### Tạo tài khoản mới

```http
POST /api/admin/accounts
{
  "tenDangNhap": "tranthib",
  "matKhau": "Password@123",
  "vaiTroId": 2,
  "nvHoSoId": 15,
  "trangThai": true
}
```

Quy trình:
1. Kiểm tra `TenDangNhap` đã tồn tại chưa
2. Băm mật khẩu bằng PBKDF2
3. Lưu hash + salt vào DB
4. **Không lưu plaintext vào `MatKhauTam`** → HR phải dùng endpoint `GET /{id}/temp-password` để xem

### Bảng tóm tắt API quản lý tài khoản

| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/admin/accounts` | Danh sách tất cả tài khoản |
| `GET` | `/api/admin/accounts/{id}` | Chi tiết 1 tài khoản |
| `POST` | `/api/admin/accounts` | Tạo tài khoản mới |
| `PUT` | `/api/admin/accounts/{id}` | Cập nhật tài khoản (role, NV, trạng thái) |
| `DELETE` | `/api/admin/accounts/{id}` | Xóa tài khoản |
| `GET` | `/api/admin/accounts/employees-for-dropdown` | Danh sách NV chọn khi tạo TK |
| `POST` | `/api/admin/accounts/quick-employee` | Tạo nhanh NV và TK cùng lúc |
| `GET` | `/api/admin/accounts/{id}/temp-password` | Xem mật khẩu tạm thời |
| `PUT` | `/api/admin/accounts/{id}/reset-password` | Reset mật khẩu về mật khẩu mới |

---

## 11. Quản lý vai trò (CRUD)

Route: `/api/admin/roles`  
Phân quyền: **HR_ACC, GIAM_DOC**

| Method | Endpoint | Mô tả |
|---|---|---|
| `GET` | `/api/admin/roles` | Danh sách vai trò |
| `GET` | `/api/admin/roles/{id}` | Chi tiết 1 vai trò |
| `POST` | `/api/admin/roles` | Tạo vai trò mới |
| `PUT` | `/api/admin/roles/{id}` | Cập nhật vai trò |
| `DELETE` | `/api/admin/roles/{id}` | Xóa vai trò |

**Ràng buộc khi xóa vai trò:** Nếu có tài khoản nào đang dùng vai trò đó → hệ thống từ chối xóa và trả về lỗi.

---

## 12. Hệ thống phân quyền chi tiết (VaiTroChucNang)

### Mô hình dữ liệu

```
VaiTro (1) ──────────────────── (N) VaiTroChucNang (N) ──── (1) ChucNang
                                      │
                                      ├── QuyenXem   (bool)
                                      ├── QuyenThem  (bool)
                                      ├── QuyenSua   (bool)
                                      ├── QuyenXoa   (bool)
                                      └── QuyenDuyet (bool)
```

### Ví dụ phân quyền

| Vai trò | Chức năng | Xem | Thêm | Sửa | Xóa | Duyệt |
|---|---|:---:|:---:|:---:|:---:|:---:|
| HR_ACC | Quản lý lương | ✅ | ✅ | ✅ | ❌ | ❌ |
| GIAM_DOC | Quản lý lương | ✅ | ❌ | ❌ | ❌ | ✅ |
| EMPLOYEE | Xem lương bản thân | ✅ | ❌ | ❌ | ❌ | ❌ |

> **Lưu ý triển khai:** Bảng `VaiTroChucNang` có đầy đủ trong DB, nhưng `PermissionsController` và guard `permissionGuard` ở frontend hiện là **stub chưa cài đặt** (luôn trả về `true`). Việc kiểm tra chi tiết quyền CRUD hiện được thực hiện qua `[Authorize(Roles = "...")]` attribute ở cấp Controller/Action.

---

## 13. Mật khẩu tạm (MatKhauTam)

### Mục đích

Khi HR tạo tài khoản mới cho nhân viên, hệ thống có thể lưu **mật khẩu tạm thời** (plaintext) để HR thông báo cho nhân viên đăng nhập lần đầu.

### Quy trình

```
1. HR tạo tài khoản → hệ thống sinh mật khẩu tạm, hash rồi lưu
   → MatKhauTam = plaintext (chỉ lưu tạm để HR tra cứu)
   → MatKhauHash, MatKhauSalt = đã băm (dùng để xác thực)

2. HR tra cứu: GET /api/auth/view-password/{taiKhoanId}
   → Trả về: { canViewPassword: true/false }
   → Chỉ được xem nếu LanDangNhapCuoi == null (NV chưa đăng nhập lần nào)

3. Nhân viên đăng nhập lần đầu:
   → MatKhauTam = null (bị xóa ngay lập tức trong LoginAsync)
   → HR không thể xem lại nữa
```

### Bảo mật

- `MatKhauTam` chỉ khả dụng **trước lần đăng nhập đầu tiên**.
- Chỉ **HR_ACC** mới có quyền xem (`[Authorize(Roles = "HR_ACC")]`).
- Sau khi nhân viên đổi mật khẩu hoặc đăng nhập, `MatKhauTam` bị xóa vĩnh viễn.

---

## 14. Đổi mật khẩu

### Người dùng tự đổi

```http
PUT /api/auth/change-password
Authorization: Bearer <token>
{
  "oldPassword": "Password@123",
  "newPassword": "NewPassword@456"
}
```

Quy trình:
1. Xác minh `oldPassword` với hash trong DB
2. Nếu sai → lỗi "Mật khẩu cũ không đúng"
3. Nếu đúng → băm `newPassword`, lưu hash + salt mới vào DB

### HR reset mật khẩu cho nhân viên

```http
PUT /api/admin/accounts/{id}/reset-password
Authorization: Bearer <token_HR>
{
  "newPassword": "TempPass@789"
}
```

HR có thể reset mật khẩu bất kỳ tài khoản nào, không cần biết mật khẩu cũ.

---

## 15. API Endpoints

### AuthController (`/api/auth`)

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| `POST` | `/api/auth/login` | Public | Đăng nhập, nhận JWT |
| `PUT` | `/api/auth/change-password` | Bất kỳ | Tự đổi mật khẩu |
| `GET` | `/api/auth/view-password/{id}` | HR_ACC | Xem mật khẩu tạm |

### AccountsController (`/api/admin/accounts`)

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| `GET` | `/api/admin/accounts` | HR, GĐ | Danh sách tài khoản |
| `GET` | `/api/admin/accounts/{id}` | HR, GĐ | Chi tiết tài khoản |
| `POST` | `/api/admin/accounts` | HR, GĐ | Tạo tài khoản |
| `PUT` | `/api/admin/accounts/{id}` | HR, GĐ | Cập nhật tài khoản |
| `DELETE` | `/api/admin/accounts/{id}` | HR, GĐ | Xóa tài khoản |
| `GET` | `/api/admin/accounts/employees-for-dropdown` | HR, GĐ | Danh sách NV để chọn |
| `POST` | `/api/admin/accounts/quick-employee` | HR, GĐ | Tạo nhanh NV + TK |
| `GET` | `/api/admin/accounts/{id}/temp-password` | HR, GĐ | Xem mật khẩu tạm |
| `PUT` | `/api/admin/accounts/{id}/reset-password` | HR, GĐ | Reset mật khẩu |

### RolesController (`/api/admin/roles`)

| Method | Endpoint | Auth | Mô tả |
|---|---|---|---|
| `GET` | `/api/admin/roles` | HR, GĐ | Danh sách vai trò |
| `GET` | `/api/admin/roles/{id}` | HR, GĐ | Chi tiết vai trò |
| `POST` | `/api/admin/roles` | HR, GĐ | Tạo vai trò mới |
| `PUT` | `/api/admin/roles/{id}` | HR, GĐ | Cập nhật vai trò |
| `DELETE` | `/api/admin/roles/{id}` | HR, GĐ | Xóa vai trò |

---

## 16. Sơ đồ tổng thể

```
┌────────────────────────────────────────────────────────────────────┐
│                  LUỒNG ĐỂ ĐĂNG NHẬP & PHÂN QUYỀN                  │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  [NGƯỜI DÙNG]                                                      │
│       │                                                            │
│       │  1. Nhập username + password                               │
│       ▼                                                            │
│  [Angular AuthService]                                             │
│       │  POST /api/auth/login                                      │
│       ▼                                                            │
│  [AuthController → AuthService]                                    │
│       │                                                            │
│       ├─ Kiểm tra TrangThai = true?         ──► KHÔNG → lỗi 401   │
│       ├─ Kiểm tra TrangThaiCanhBao = "CAM"? ──► CÓ  → lỗi 401    │
│       ├─ Kiểm tra Lockout (ThoiGianKhoa)?   ──► Còn → lỗi 401    │
│       ├─ Xác minh PBKDF2(password, salt)    ──► Sai → đếm sai    │
│       │                                          (≥3 → khóa 15p)  │
│       └─ Đúng → Tạo JWT  ──────────────────────────────────────── │
│                                │                                   │
│                         [JWT Access Token]                         │
│                         Claims: userid, role, EmployeeId, exp      │
│                                │                                   │
│                                ▼                                   │
│  [Angular] ─── localStorage['qlns_auth'] ◄── lưu token + role     │
│       │                                                            │
│       │  2. Điều hướng theo role                                   │
│       │  EMPLOYEE → /employee/dashboard                            │
│       │  HR_ACC   → /hr/dashboard                                  │
│       │  GIAM_DOC → /gd/dashboard                                  │
│       │                                                            │
│       │  3. Mọi request tiếp theo                                  │
│       ▼                                                            │
│  [AuthInterceptor] → tự gắn Authorization: Bearer <token>         │
│       │                                                            │
│       ▼                                                            │
│  [RoleGuard / AuthGuard] → kiểm tra route permission              │
│       │                                                            │
│       ▼                                                            │
│  [Backend: UseAuthentication → UseAuthorization]                   │
│       │                                                            │
│       ├─ Token hợp lệ? Chưa hết hạn?  ──► KHÔNG → 401            │
│       └─ Role trong [Authorize]?        ──► KHÔNG → 403            │
│                         │                                          │
│                         ▼                                          │
│                  [Controller Action]                               │
│                  Xử lý business logic                              │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## 17. Câu hỏi thường gặp (FAQ)

### Q: Tại sao không dùng cookie thay vì localStorage?
**A:** JWT lưu trong `localStorage` dễ quản lý hơn với SPA (Single Page Application) và đơn giản hóa việc gửi token đến nhiều domain khác nhau. Nhược điểm là dễ bị XSS nếu không vệ sinh input đúng cách.

---

### Q: Token hết hạn thì làm sao?
**A:** Hiện tại hệ thống có `refreshToken` trong response nhưng **chưa cài đặt endpoint refresh**. Khi `accessToken` hết hạn (mặc định 60 phút), người dùng cần đăng nhập lại.

---

### Q: Làm sao biết request của tôi đang dùng role nào?
**A:** Trong Backend, giải mã token và đọc claim `ClaimTypes.Role`. Trong Frontend, đọc `authService.role` — giá trị này được lưu trong `localStorage` khi đăng nhập.

---

### Q: Tại sao đăng nhập đúng mật khẩu vẫn bị từ chối?
**A:** Có thể do một trong các nguyên nhân:
1. `TrangThai = false` (tài khoản bị vô hiệu hóa)
2. `TrangThaiCanhBao = "CAM"` (bị cấm)
3. `ThoiGianKhoa` chưa hết (vừa nhập sai 3 lần)

---

### Q: HR tạo tài khoản xong, nhân viên lấy mật khẩu ở đâu?
**A:** HR gọi `GET /api/admin/accounts/{id}/temp-password` để lấy mật khẩu tạm, rồi thông báo cho nhân viên. Sau khi nhân viên đăng nhập lần đầu, mật khẩu tạm bị xóa vĩnh viễn.

---

### Q: Có thể đăng nhập từ nhiều thiết bị cùng lúc không?
**A:** Có. Hệ thống không giới hạn số session. JWT là stateless — server không lưu trạng thái session, mọi token hợp lệ đều được chấp nhận.

---

### Q: Xóa tài khoản có ảnh hưởng dữ liệu lịch sử không?
**A:** Nên dùng **vô hiệu hóa** (`TrangThai = false`) thay vì xóa hẳn, để bảo toàn audit log và các bản ghi lịch sử liên quan đến tài khoản đó.

---

### Q: `VaiTroChucNang` có thực sự được kiểm tra khi gọi API không?
**A:** **Hiện tại chưa.** Bảng dữ liệu đã có nhưng logic kiểm tra chi tiết (`PermissionsController`, `permissionGuard`) vẫn là stub rỗng. Phân quyền hiện thực qua `[Authorize(Roles = "...")]` trực tiếp trong code.

---

*Tài liệu được tạo ngày 08/03/2026 – Phiên bản 1.0*
