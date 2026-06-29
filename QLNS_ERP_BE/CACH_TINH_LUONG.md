# Công Thức Tính Lương (Cập nhật từ Source Code)

Tài liệu này mô tả chính xác logic tính toán đang được cài đặt trong `LuongService.cs`.

## 1. Định nghĩa biến

| Biến Code (Variable) | Kiểu dữ liệu | Nguồn dữ liệu | Mô tả |
| :--- | :--- | :--- | :--- |
| `nvLuong.LuongCoBan` | `decimal` | Bảng `NvLuongHienTai` | Lương cơ bản hiện tại của nhân viên. |
| `nvLuong.PhuCapCoDinh` | `decimal` | Bảng `NvLuongHienTai` | Phụ cấp cố định hàng tháng. |
| `tongCong` | `int` | Bảng `ChamCong` (Count) | Tổng số ngày có trạng thái `TrangThai == "DI_LAM"`. |
| `tongOt` | `decimal` | Bảng `ChamCong` (Sum) | Tổng số giờ OT (`SoGioOt`) trong tháng. |

## 2. Hằng số (Hardcoded Constants)

Các giá trị sau được gắn cứng trong code:

*   **26**: Số ngày công chuẩn một tháng.
*   **8**: Số giờ làm việc chuẩn một ngày.
*   **1.5**: Hệ số nhân lương làm thêm giờ (OT).

## 3. Quy trình tính toán (Step-by-step)

Hệ thống thực hiện tính toán qua 3 dòng code chính (dòng 44-47 trong `LuongService.cs`):

### Bước 1: Tính lương ngày
Lương cơ bản chia cho 26.

```csharp
decimal luongNgay = nvLuong.LuongCoBan / 26;
```
> **Lưu ý**: Kết quả là số thập phân do `LuongCoBan` là `decimal`.

### Bước 2: Tính lương OT (Làm thêm giờ)
Công thức: (Tổng giờ OT) * (Lương 1 giờ) * 1.5.
Lương 1 giờ được tính là `luongNgay / 8`.

```csharp
decimal luongOt = tongOt * (luongNgay / 8) * 1.5m;
```

### Bước 3: Tính tổng lương (Gross)
Tổng lương = (Lương ngày * Số ngày công đi làm) + Phụ cấp cố định + Lương OT.

```csharp
decimal tongLuong = (luongNgay * tongCong) + nvLuong.PhuCapCoDinh + luongOt;
```

## 4. Công thức toán học tổng hợp

**Tổng Lương** = ((Lương Cơ Bản / 26) * Số Ngày Đi Làm) + Phụ Cấp + (Tổng Giờ OT * (Lương Cơ Bản / (26 * 8)) * 1.5)

## 5. Các giá trị mặc định khác

Trong phiên bản hiện tại, các trường sau được gán cứng bằng 0:

```csharp
entity.KhauTru = 0;
entity.Thuong = 0;
```
