# HỆ THỐNG TÍNH LƯƠNG – TÀI LIỆU CHI TIẾT

> **Mục tiêu:** Tài liệu này mô tả toàn bộ quy trình tính lương trong hệ thống QLNS ERP, từ dữ liệu đầu vào, công thức tính, vòng đời trạng thái, cho đến các API endpoint. Người đọc không cần biết trước về dự án vẫn có thể hiểu được.

---

## MỤC LỤC

1. [Tổng quan quy trình](#1-tổng-quan-quy-trình)
2. [Các thành phần dữ liệu tham gia](#2-các-thành-phần-dữ-liệu-tham-gia)
3. [Tham số hệ thống (ThamSoHeThong)](#3-tham-số-hệ-thống-thamsoheThong)
4. [Công thức tính lương chi tiết](#4-công-thức-tính-lương-chi-tiết)
5. [Vòng đời trạng thái bảng lương](#5-vòng-đời-trạng-thái-bảng-lương)
6. [Luồng xử lý từng bước](#6-luồng-xử-lý-từng-bước)
7. [Quản lý thưởng & khấu trừ (BangLuongItem)](#7-quản-lý-thưởng--khấu-trừ-bangluongitem)
8. [Quản lý lương cơ bản (NvLuongHienTai)](#8-quản-lý-lương-cơ-bản-nvluonghientai)
9. [Phụ cấp (NvPhuCap)](#9-phụ-cấp-nvphucap)
10. [API Endpoints](#10-api-endpoints)
11. [Vai trò & phân quyền](#11-vai-trò--phân-quyền)
12. [Sơ đồ tổng thể](#12-sơ-đồ-tổng-thể)
13. [Câu hỏi thường gặp (FAQ)](#13-câu-hỏi-thường-gặp-faq)

---

## 1. Tổng quan quy trình

Hệ thống tính lương hoạt động theo **quy trình 4 bước tuần tự**:

```
[CHỐT CÔNG] ──► [TÍNH LƯƠNG] ──► [GỬI DUYỆT] ──► [GIÁM ĐỐC DUYỆT]
     ▲                                                      │
     │                                                      ▼
  (Bảng công                                          DA_DUYET / TU_CHOI
  phải "DA_CHOT_CONG"                                    / DA_KHOA
  trước khi tính)
```

**Tóm lược:**
- **HR (Kế toán lương)** chịu trách nhiệm tính lương cho từng nhân viên mỗi tháng.
- Trước khi tính lương, **bảng công tháng phải được chốt** (`DA_CHOT_CONG`).
- Sau khi tính xong, HR **gửi lên Giám đốc** để xét duyệt.
- **Giám đốc** đồng ý hoặc từ chối.
- Nhân viên có thể **tự xem lương** của mình sau khi bảng lương được phê duyệt.

---

## 2. Các thành phần dữ liệu tham gia

### 2.1 Bảng `BangLuongThang` – Bảng lương tháng

Mỗi bản ghi đại diện cho **bảng lương của 1 nhân viên trong 1 tháng/năm**.

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `NvHoSoId` | int | ID nhân viên |
| `BangCongThangId` | int | Liên kết bảng công tháng tương ứng |
| `Thang` | int | Tháng tính lương (1–12) |
| `Nam` | int | Năm tính lương |
| `LuongCoBanTinh` | decimal | Lương cơ bản **tại thời điểm tính** (snapshot) |
| `TongCong` | decimal | Tổng số ngày công thực tế trong tháng |
| `TongOt` | decimal | Tổng số giờ làm thêm (OT) trong tháng |
| `PhuCapTinh` | decimal | Tổng phụ cấp (snapshot tại thời điểm tính) |
| `Thuong` | decimal | Tổng thưởng từ `BangLuongItem` loại THUONG |
| `KhauTru` | decimal | Tổng khấu trừ (bao gồm phạt đi muộn + khấu trừ thủ công) |
| `TongLuong` | decimal | **Lương thực nhận cuối cùng** |
| `TrangThai` | string | Trạng thái hiện tại (xem mục 5) |
| `NgayTinhLuong` | DateTime? | Thời điểm HR bấm "tính lương" |
| `NgayGuiDuyet` | DateTime? | Thời điểm HR gửi lên GĐ |
| `NgayDuyetGiamDoc` | DateTime? | Thời điểm GĐ ký duyệt |
| `NgayKhoaLuong` | DateTime? | Thời điểm bảng lương bị khóa |
| `TaiKhoanTinhId` | int? | Tài khoản HR thực hiện tính |
| `TaiKhoanGuiDuyetId` | int? | Tài khoản HR gửi duyệt |
| `TaiKhoanDuyetId` | int? | Tài khoản GĐ duyệt |
| `TaiKhoanKhoaId` | int? | Tài khoản thực hiện khóa |
| `IsDirty` | bool | Cờ đánh dấu cần tính lại (khi công/phụ cấp thay đổi) |

> **Lưu ý:** `LuongCoBanTinh` và `PhuCapTinh` là **bản sao snapshot** tại thời điểm tính lương — dù sau này HR thay đổi lương cơ bản của nhân viên, bảng lương cũ vẫn giữ nguyên giá trị cũ.

---

### 2.2 Bảng `BangLuongItem` – Thưởng / Khấu trừ thủ công

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `BangLuongThangId` | int | Thuộc bảng lương nào |
| `Loai` | string | `"THUONG"` hoặc `"KHAU_TRU"` |
| `LyDo` | string | Mô tả lý do thưởng/phạt |
| `SoTien` | decimal | Số tiền (luôn dương, `Loai` quyết định cộng/trừ) |
| `TaiKhoanTaoId` | int? | Người tạo item |
| `CreatedAt` | DateTime | Thời điểm tạo |

---

### 2.3 Bảng `NvLuongHienTai` – Lương hiện tại của nhân viên

Lưu **lương cơ bản và phụ cấp cố định** đang được áp dụng cho nhân viên. Có thể có nhiều bản ghi per nhân viên (lịch sử), nhưng chỉ 1 bản có `DangApDung = true`.

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `NvHoSoId` | int | Nhân viên |
| `LuongCoBan` | decimal | Lương cơ bản (VNĐ/tháng) |
| `PhuCapCoDinh` | decimal | Phụ cấp cố định (fallback nếu không có NvPhuCap) |
| `SoTaiKhoanNganHang` | string? | Số tài khoản ngân hàng |
| `TenNganHang` | string? | Tên ngân hàng |
| `ChiNhanhNganHang` | string? | Chi nhánh ngân hàng |
| `NgayBatDauHieuLuc` | DateTime | Ngày bắt đầu áp dụng |
| `NgayKetThucHieuLuc` | DateTime? | Ngày hết hạn (null = đang dùng) |
| `DangApDung` | bool | `true` = đang áp dụng |

---

### 2.4 Bảng `ChamCong` – Chấm công từng ngày

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `NvHoSoId` | int | Nhân viên |
| `BangCongThangId` | int | Bảng công tháng |
| `Ngay` | DateTime | Ngày chấm công |
| `GioVao` | DateTime? | Giờ vào làm |
| `GioRa` | DateTime? | Giờ ra về |
| `SoGioOt` | decimal | Số giờ OT hôm đó |
| `TrangThai` | string | `DI_LAM` / `TRE` / `VANG_MAT` / `NGHI_PHEP` / v.v. |
| `PhuongThuc` | string? | `MANUAL` (nhập tay) hoặc `FACE_RECOGNITION` (nhận diện mặt) |

---

### 2.5 Bảng `NvPhuCap` – Phụ cấp chi tiết từng loại

Cho phép gắn nhiều loại phụ cấp khác nhau cho 1 nhân viên (phụ cấp ăn trưa, xăng xe, điện thoại...).

---

### 2.6 Bảng `BangCongThang` – Bảng công tháng

| Trường | Ý nghĩa |
|---|---|
| `Thang`, `Nam` | Tháng/năm |
| `TrangThaiCong` | Phải là `"DA_CHOT_CONG"` để tính lương |

---

## 3. Tham số hệ thống (ThamSoHeThong)

Các hằng số dùng trong công thức tính lương được lưu trong bảng `ThamSoHeThong` và có thể thay đổi không cần triển khai lại code.

| Mã tham số | Giá trị mặc định | Ý nghĩa |
|---|---|---|
| `LUONG_NGAY_CONG_CHUAN` | `26` | Số ngày công chuẩn 1 tháng |
| `LUONG_GIO_LAM_CHUAN` | `8` | Số giờ làm chuẩn 1 ngày |
| `LUONG_HE_SO_OT` | `1.5` | Hệ số nhân lương OT (150%) |
| `LUONG_PHAT_DI_MUON` | `30,000` | Tiền phạt mỗi lần đi muộn (VNĐ) |
| `LUONG_CO_TINH_PHU_CAP` | `1` | `1` = có tính phụ cấp, `0` = bỏ qua |
| `LUONG_CO_TINH_OT` | `1` | `1` = có tính lương OT, `0` = bỏ qua |
| `LUONG_CO_TINH_THUONG` | `1` | `1` = có tính thưởng, `0` = bỏ qua |
| `LUONG_CO_TINH_KHAU_TRU` | `1` = | `1` = có tính khấu trừ, `0` = bỏ qua |
| `CHAM_CONG_GIO_VAO` | `"08:00"` | Giờ vào chuẩn (HH:mm) |
| `CHAM_CONG_GIO_GIA_CU` | `1` | Thời gian gia cú (phút) – sau đó là đi muộn |

> **Ý nghĩa "gia cú":** Nếu `CHAM_CONG_GIO_VAO = 08:00` và `CHAM_CONG_GIO_GIA_CU = 5`, thì nhân viên vào trước `08:05` vẫn không bị phạt. Từ `08:06` trở đi mới tính là đi muộn.

---

## 4. Công thức tính lương chi tiết

### 4.1 Công thức tổng quát

```
TongLuong = LuongCong + PhuCap + LuongOT + Thuong - KhauTru
```

Trong đó mỗi thành phần có thể **bật/tắt** qua tham số hệ thống.

---

### 4.2 Tính từng thành phần

#### A. Lương công (bắt buộc, không thể tắt)

```
LuongNgay  = LuongCoBan / NgayCongChuan
           = LuongCoBan / 26

LuongCong  = LuongNgay × TongCong
```

**`TongCong`** = số ngày chấm công có trạng thái `DI_LAM` **hoặc** `TRE` trong tháng.

> Nhân viên đi muộn (`TRE`) **vẫn được tính công** — họ chỉ bị *phạt tiền* chứ không mất ngày công.

---

#### B. Lương OT (tắt được bằng `LUONG_CO_TINH_OT = 0`)

```
LuongOT = TongGioOT × (LuongNgay / GioLamChuan) × HeSoOT
        = TongGioOT × (LuongNgay / 8)            × 1.5
```

**`TongGioOT`** = tổng `SoGioOt` của mọi bản ghi chấm công trong tháng.

---

#### C. Phụ cấp (tắt được bằng `LUONG_CO_TINH_PHU_CAP = 0`)

Hệ thống ưu tiên lấy phụ cấp theo thứ tự:

1. **Ưu tiên:** Lấy từ bảng `NvPhuCap` — danh sách các khoản phụ cấp chi tiết theo từng loại (ăn trưa, xăng xe...) đang còn hiệu lực (`DangApDung = true`, trong khoảng `NgayBatDau` – `NgayKetThuc`).
2. **Fallback:** Nếu không có `NvPhuCap` nào → lấy `PhuCapCoDinh` từ `NvLuongHienTai`.

```
TongPhuCap = SUM(NvPhuCap.SoTien)         -- nếu có NvPhuCap
           = NvLuongHienTai.PhuCapCoDinh  -- nếu không có NvPhuCap
```

---

#### D. Thưởng (tắt được bằng `LUONG_CO_TINH_THUONG = 0`)

```
TongThuong = SUM(BangLuongItem.SoTien WHERE Loai = 'THUONG')
```

Đây là thưởng **thủ công** do HR nhập, ví dụ: thưởng KPI, thưởng dự án.

---

#### E. Khấu trừ (tắt được bằng `LUONG_CO_TINH_KHAU_TRU = 0`)

Khấu trừ gồm **2 phần cộng lại**:

```
KhauTruThucTe = KhauTruThuongPhat + KhauTruDiMuon
```

**E.1 – Khấu trừ thủ công (HR nhập):**
```
KhauTruThuongPhat = SUM(BangLuongItem.SoTien WHERE Loai = 'KHAU_TRU')
```

**E.2 – Phạt đi muộn (tự động):**
```
SoLanDiMuon = số bản ghi ChamCong thỏa 1 trong 2 điều kiện:
  - TrangThai = 'TRE'                              (face recognition xác định muộn)
  - TrangThai = 'DI_LAM' VÀ GioVao > GioChuanVao + GiaCuPhut   (nhập tay muộn)

KhauTruDiMuon = SoLanDiMuon × PhatDiMuon
              = SoLanDiMuon × 30,000
```

> **Ví dụ:** Nhân viên đi muộn 3 lần trong tháng → bị trừ `3 × 30,000 = 90,000 VNĐ`.

---

#### F. Tổng hợp cuối

```
TongLuong = LuongCong
          + (coTinhPhuCap  ? TongPhuCap    : 0)
          + (coTinhOT      ? LuongOT       : 0)
          + (coTinhThuong  ? TongThuong    : 0)
          - (coTinhKhauTru ? KhauTruThucTe : 0)

TongLuong = MAX(TongLuong, 0)   -- không bao giờ âm
```

---

### 4.3 Ví dụ minh họa số liệu cụ thể

**Giả sử:**
- Lương cơ bản: **10,000,000 VNĐ**
- Số ngày công chuẩn: **26**
- Số ngày công thực tế: **22**
- Số giờ OT: **8 giờ**
- Phụ cấp: **1,500,000 VNĐ** (ăn trưa 1,000,000 + xăng xe 500,000)
- Thưởng KPI: **2,000,000 VNĐ**
- Đi muộn 2 lần, không có khấu trừ thủ công

**Tính toán:**

| Thành phần | Công thức | Kết quả |
|---|---|---|
| LuongNgay | 10,000,000 / 26 | **384,615 VNĐ** |
| LuongCong | 384,615 × 22 | **8,461,538 VNĐ** |
| LuongOT | 8 × (384,615 / 8) × 1.5 | **576,923 VNĐ** |
| PhuCap | 1,000,000 + 500,000 | **1,500,000 VNĐ** |
| Thuong | — | **2,000,000 VNĐ** |
| KhauTruDiMuon | 2 × 30,000 | **60,000 VNĐ** |
| **TongLuong** | 8,461,538 + 576,923 + 1,500,000 + 2,000,000 − 60,000 | **≈ 12,478,461 VNĐ** |

---

## 5. Vòng đời trạng thái bảng lương

```
                     ┌──────────────────────────────────────────┐
                     │                                          │
                     ▼                                          │
              [TAM_TINH]  ◄──── HR có thể tính lại             │
                  │                                             │
                  │ HR: Gửi duyệt                               │ HR: Thu hồi
                  ▼                                             │
         [CHO_DUYET_GIAM_DOC] ──────────────────────────────────┘
                  │
         ┌────────┴────────┐
         │ GĐ: Đồng ý      │ GĐ: Từ chối
         ▼                 ▼
      [DA_DUYET]       [TU_CHOI]
         │
         │ (Hệ thống hoặc HR: Khóa)
         ▼
      [DA_KHOA]
```

### Giải thích từng trạng thái

| Trạng thái | Ý nghĩa | Ai có thể thao tác tiếp |
|---|---|---|
| `TAM_TINH` | Vừa được HR tính, chưa gửi duyệt. HR có thể tính lại. | HR: gửi duyệt, tính lại |
| `CHO_DUYET_GIAM_DOC` | Đã gửi lên Giám đốc, đang chờ phê duyệt. **Không tính lại được.** | GĐ: duyệt/từ chối; HR: thu hồi |
| `DA_DUYET` | Giám đốc đã ký duyệt. Lương chính thức. | Hệ thống: khóa |
| `TU_CHOI` | Giám đốc từ chối. HR cần xem lại và tính lại từ đầu. | HR: tính lại (sẽ tạo bản mới) |
| `DA_KHOA` | Bảng lương đã bị khóa vĩnh viễn. Không thao tác được nữa. | — |

### Các ràng buộc quan trọng

- **Không thể tính lại** khi trạng thái là `DA_DUYET`, `DA_KHOA`, hoặc `CHO_DUYET_GIAM_DOC`.
- **Thu hồi** chỉ được thực hiện khi đang ở `CHO_DUYET_GIAM_DOC` → trở về `TAM_TINH`.
- **Tính lần đầu:** Tạo bản ghi mới với trạng thái `TAM_TINH`.
- **Tính lại** (khi chưa gửi duyệt): Cập nhật bản ghi hiện có, giữ nguyên `Id`.

---

## 6. Luồng xử lý từng bước

### Bước 1: Điều kiện tiên quyết – Chốt công

Trước khi tính lương, HR phải **chốt bảng công tháng** qua module Chấm công.

```
BangCongThang.TrangThaiCong phải = "DA_CHOT_CONG"
```

Nếu chưa chốt → API trả lỗi: `"Vui lòng chốt công tháng X/Y trước khi tính lương"`

---

### Bước 2: HR tính lương (`POST /api/luong/tinh`)

**Request body:**
```json
{
  "nvHoSoId": 5,
  "thang": 3,
  "nam": 2026
}
```

**Hệ thống thực hiện theo thứ tự:**

```
1. Tìm NvLuongHienTai (DangApDung=true) của nhân viên
   → Nếu không có: lỗi "Nhân viên chưa có thông tin lương hiện tại"

2. Tìm BangCongThang (Thang=3, Nam=2026)
   → Nếu không có: lỗi "Bảng công tháng chưa tồn tại"
   → Nếu chưa chốt: lỗi "Vui lòng chốt công tháng..."

3. Đếm TongCong:
   SELECT COUNT(*) FROM ChamCong
   WHERE NvHoSoId = 5
     AND BangCongThangId = bangCong.Id
     AND TrangThai IN ('DI_LAM', 'TRE')

4. Tính TongOt:
   SELECT SUM(SoGioOt) FROM ChamCong
   WHERE NvHoSoId = 5
     AND BangCongThangId = bangCong.Id

5. Lấy phụ cấp (NvPhuCap còn hiệu lực ngày hôm nay)
   → Nếu không có → dùng NvLuongHienTai.PhuCapCoDinh

6. Đọc tham số hệ thống từ ThamSoHeThong

7. Kiểm tra BangLuongThang đã tồn tại chưa:
   - Nếu TrangThai = 'DA_DUYET' hoặc 'DA_KHOA'    → từ chối tính lại
   - Nếu TrangThai = 'CHO_DUYET_GIAM_DOC'          → từ chối tính lại
   - Nếu không tồn tại                              → tạo mới với TrangThai='TAM_TINH'

8. Tổng hợp Thuong và KhauTruThuongPhat từ BangLuongItem

9. Tính KhauTruDiMuon (đếm SoLanDiMuon)

10. Áp dụng công thức → lưu TongLuong vào DB

11. Trả về BangLuongThangDto chi tiết
```

---

### Bước 3: HR gửi duyệt (`POST /api/luong/{id}/gui-duyet`)

```json
{ "ghiChu": "Lương tháng 3/2026 đề nghị GĐ phê duyệt" }
```

- Chỉ được gửi nếu `TrangThai = "TAM_TINH"`
- Sau khi gửi: `TrangThai → "CHO_DUYET_GIAM_DOC"`, ghi lại `NgayGuiDuyet` và `TaiKhoanGuiDuyetId`
- Hệ thống gửi **thông báo** tới tất cả tài khoản có role `GIAM_DOC`

---

### Bước 3.5 (tùy chọn): HR thu hồi (`POST /api/luong/{id}/thu-hoi`)

- Chỉ được thu hồi nếu `TrangThai = "CHO_DUYET_GIAM_DOC"`
- Sau khi thu hồi: `TrangThai → "TAM_TINH"`, xóa `TaiKhoanGuiDuyetId` và `NgayGuiDuyet`
- HR có thể chỉnh sửa thưởng/khấu trừ rồi gửi lại

---

### Bước 4: Giám đốc duyệt (`POST /api/luong/{id}/duyet`)

```json
{ "dongY": true }
// hoặc
{ "dongY": false, "lyDoTuChoi": "Sai số ngày công" }
```

- Chỉ được duyệt nếu `TrangThai = "CHO_DUYET_GIAM_DOC"`
- **Đồng ý:** `TrangThai → "DA_DUYET"`, ghi lại `NgayDuyetGiamDoc`
- **Từ chối:** `TrangThai → "TU_CHOI"`
- Hệ thống gửi **thông báo** lại cho HR biết kết quả

---

### Bước 5 (tự động): Nhân viên xem lương (`GET /api/luong/me`)

- Nhân viên đăng nhập → xem danh sách lương các tháng của mình
- Hiển thị tất cả trạng thái (kể cả `TAM_TINH`, `DA_DUYET`...)
- Thông tin bao gồm: lương cơ bản, phụ cấp, tổng công, OT, thưởng, khấu trừ, tổng nhận

---

## 7. Quản lý thưởng & khấu trừ (BangLuongItem)

### Khi nào thêm item?

HR có thể thêm/xóa thưởng và khấu trừ **bất kỳ lúc nào miễn bảng lương chưa bị khóa**.

### API quản lý items

| Endpoint | Mô tả |
|---|---|
| `GET /api/luong/{bangLuongId}/items` | Xem danh sách thưởng/khấu trừ của bảng lương |
| `POST /api/luong/{bangLuongId}/items` | Thêm 1 khoản thưởng/khấu trừ |
| `DELETE /api/luong/{bangLuongId}/items/{itemId}` | Xóa 1 khoản |

### Request body khi thêm:
```json
{
  "loai": "THUONG",
  "lyDo": "Thưởng KPI tháng 3",
  "soTien": 2000000
}
```
hoặc:
```json
{
  "loai": "KHAU_TRU",
  "lyDo": "Thiếu báo cáo tuần",
  "soTien": 100000
}
```

### Cập nhật tổng lương sau khi thêm/xóa item

Sau mỗi thao tác thêm/xóa item, hệ thống **tự động tính lại** `TongLuong` theo công thức:

```
luongNgay  = LuongCoBanTinh / 26
luongOT    = TongOt × (luongNgay / 8) × 1.5
TongLuong  = (luongNgay × TongCong) + PhuCapTinh + luongOT + Thuong - KhauTru
```

---

## 8. Quản lý lương cơ bản (NvLuongHienTai)

### Cập nhật lương cơ bản cho nhân viên

Route: `POST /api/luong-co-ban/nhan-vien/{nvId}`

Khi HR cập nhật lương mới:
1. Bản ghi cũ (`DangApDung = true`) được **deactivate**: `DangApDung = false`, `NgayKetThucHieuLuc = hôm nay`
2. Bản ghi mới được **tạo**: `DangApDung = true`, `NgayBatDauHieuLuc = hôm nay`

> **Quan trọng:** Thay đổi lương cơ bản **chỉ ảnh hưởng** đến các bảng lương tính **sau** thời điểm thay đổi. Bảng lương cũ đã tính không bị ảnh hưởng vì dùng `LuongCoBanTinh` (snapshot).

---

## 9. Phụ cấp (NvPhuCap)

### Cấu trúc phụ cấp chi tiết

Mỗi nhân viên có thể có nhiều loại phụ cấp (ví dụ: ăn trưa, xăng xe, điện thoại...). Mỗi loại được định nghĩa trong bảng `PhuCapLoai`.

### Điều kiện áp dụng

Khi tính lương, hệ thống chỉ lấy các `NvPhuCap` thỏa:
- `DangApDung = true`
- `NgayBatDau <= ngày hôm nay`
- `NgayKetThuc IS NULL` hoặc `NgayKetThuc >= ngày hôm nay`

### Fallback về `PhuCapCoDinh`

Nếu không tìm thấy bất kỳ `NvPhuCap` nào thỏa điều kiện, hệ thống dùng `NvLuongHienTai.PhuCapCoDinh`.

---

## 10. API Endpoints

### LuongController (`/api/luong`)

| Method | Endpoint | Phân quyền | Mô tả |
|---|---|---|---|
| `GET` | `/api/luong` | HR, GIAM_DOC | Danh sách tất cả bảng lương |
| `GET` | `/api/luong/{id}` | HR, GIAM_DOC | Chi tiết 1 bảng lương |
| `GET` | `/api/luong/me` | EMPLOYEE, HR, GIAM_DOC | Lương của tôi |
| `GET` | `/api/luong/tong-luong-thang?thang=3&nam=2026` | HR, GIAM_DOC | Tổng lương theo tháng |
| `GET` | `/api/luong/thong-ke-trang-thai?thang=3&nam=2026` | HR, GIAM_DOC | Thống kê trạng thái |
| `POST` | `/api/luong/tinh` | HR | Tính lương 1 nhân viên |
| `POST` | `/api/luong/{id}/gui-duyet` | HR | Gửi lên Giám đốc |
| `POST` | `/api/luong/{id}/thu-hoi` | HR | Thu hồi bảng lương |
| `POST` | `/api/luong/{id}/duyet` | GIAM_DOC | Duyệt hoặc từ chối |

### BangLuongItemController (`/api/luong/{bangLuongId}/items`)

| Method | Endpoint | Phân quyền | Mô tả |
|---|---|---|---|
| `GET` | `/api/luong/{bangLuongId}/items` | HR, GIAM_DOC | Lấy items |
| `POST` | `/api/luong/{bangLuongId}/items` | HR | Thêm thưởng/khấu trừ |
| `DELETE` | `/api/luong/{bangLuongId}/items/{itemId}` | HR | Xóa item |

### LuongCoBanController (`/api/luong-co-ban`)

| Method | Endpoint | Phân quyền | Mô tả |
|---|---|---|---|
| `GET` | `/api/luong-co-ban` | HR | Tất cả NV đang áp dụng |
| `GET` | `/api/luong-co-ban/nhan-vien/{nvId}` | HR | Lịch sử lương 1 NV |
| `POST` | `/api/luong-co-ban/nhan-vien/{nvId}` | HR | Cập nhật lương cơ bản |

---

## 11. Vai trò & phân quyền

| Vai trò | Mã | Quyền hạn trong hệ thống lương |
|---|---|---|
| Kế toán lương / HR | `HR_ACC` | Tính lương, thêm/xóa items, gửi duyệt, thu hồi, xem danh sách |
| Giám đốc | `GIAM_DOC` | Duyệt hoặc từ chối bảng lương, xem danh sách |
| Nhân viên | `EMPLOYEE` | Chỉ xem lương của bản thân (`/me`) |

---

## 12. Sơ đồ tổng thể

```
┌─────────────────────────────────────────────────────────────────────┐
│                        HỆ THỐNG TÍNH LƯƠNG                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  [ThamSoHeThong]          [NvLuongHienTai]        [NvPhuCap]        │
│  ─ NgayCongChuan=26       ─ LuongCoBan            ─ PhuCapLoai      │
│  ─ GioLamChuan=8          ─ PhuCapCoDinh           ─ SoTien         │
│  ─ HeSoOT=1.5             ─ DangApDung=true        ─ DangApDung     │
│  ─ PhatDiMuon=30,000                                                │
│  ─ CờBật/Tắt thành phần                                             │
│           │                        │                    │           │
│           └────────────────────────┴────────────────────┘           │
│                                    │                                │
│                                    ▼                                │
│  [BangCongThang]       ──►  LuongService.TinhLuongAsync()           │
│  TrangThaiCong                     │                                │
│  = "DA_CHOT_CONG"                  │                                │
│                                    │                                │
│  [ChamCong]            ──►  TongCong, TongOT, SoLanDiMuon           │
│  TrangThai:                        │                                │
│  DI_LAM / TRE / ...                │                                │
│                                    │                                │
│  [BangLuongItem]       ──►  Thuong, KhauTruThuongPhat               │
│  Loai: THUONG / KHAU_TRU           │                                │
│                                    ▼                                │
│                          ┌─────────────────┐                        │
│                          │  BangLuongThang  │                        │
│                          │  ─ LuongCong     │                        │
│                          │  ─ + PhuCap      │                        │
│                          │  ─ + LuongOT     │                        │
│                          │  ─ + Thuong      │                        │
│                          │  ─ − KhauTru     │                        │
│                          │  ═══════════════ │                        │
│                          │  = TongLuong     │                        │
│                          └─────────────────┘                        │
│                                    │                                │
│                     TAM_TINH → CHO_DUYET → DA_DUYET                 │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 13. Câu hỏi thường gặp (FAQ)

### Q: Tại sao nhân viên đi muộn vẫn được tính công?
**A:** Vì đi muộn (`TRE`) vẫn là đến làm, chỉ là không đúng giờ. Hệ thống tính đủ ngày công nhưng trừ tiền phạt theo số lần đi muộn. Nhân viên vắng mặt (`VANG_MAT`) mới không được tính công.

---

### Q: Khác biệt giữa `TRE` và `DI_LAM` muộn là gì?
**A:**
- `TRE`: Trạng thái được xác định bởi **hệ thống nhận diện khuôn mặt** (face recognition) khi ghi nhận giờ vào sau giờ chuẩn.
- `DI_LAM` + `GioVao > GioChuanCoGiaCu`: Được nhập tay bởi HR nhưng giờ vào thực tế sau giờ chuẩn + gia cú.
Cả 2 trường hợp đều bị tính phạt đi muộn như nhau.

---

### Q: Có thể tính lại lương sau khi đã gửi Giám đốc không?
**A:** Không thể tính lại trực tiếp. HR phải **thu hồi** (`/thu-hoi`) trước để kéo về `TAM_TINH`, sau đó mới tính lại.

---

### Q: Nếu Giám đốc từ chối thì sao?
**A:** Bảng lương chuyển sang `TU_CHOI`. HR cần xem lý do, điều chỉnh (thêm/xóa items hoặc kiểm tra công), rồi **tính lại** (hệ thống sẽ cập nhật bản ghi hiện có và reset về `TAM_TINH`).

---

### Q: Lương cơ bản thay đổi giữa chừng có ảnh hưởng bảng lương cũ không?
**A:** Không. Mỗi bảng lương lưu `LuongCoBanTinh` là **snapshot** tại thời điểm tính. Các bảng lương cũ luôn giữ nguyên giá trị lương cơ bản lúc tính.

---

### Q: OT được tính từ đâu?
**A:** Từ trường `SoGioOt` trong bảng `ChamCong`. HR (hoặc hệ thống) nhập số giờ OT cho từng ngày khi chấm công. Tổng OT trong tháng = tổng tất cả `SoGioOt` của nhân viên đó trong tháng đó.

---

### Q: Phụ cấp itemized và phụ cấp cố định khác nhau thế nào?
**A:** 
- **Phụ cấp itemized** (`NvPhuCap`): Chi tiết từng loại (ăn trưa 1tr, xăng xe 500k...), linh hoạt hơn, được hiển thị từng khoản trong phiếu lương.
- **Phụ cấp cố định** (`PhuCapCoDinh`): 1 số duy nhất trong `NvLuongHienTai`, dùng làm fallback khi không có itemized.

---

*Tài liệu được tạo ngày 08/03/2026 – Phiên bản 1.0*
