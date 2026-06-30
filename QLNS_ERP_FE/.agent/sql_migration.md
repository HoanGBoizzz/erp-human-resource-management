# SQL Migration - Module Nơi Làm Việc

## Tổng quan

Tài liệu này mô tả SQL Migration dùng để tạo các bảng cho module **Nơi Làm Việc** trong hệ thống QLNS ERP.

### Thông tin

| Thuộc tính | Giá trị |
|------------|----------|
| Database | `qlns_erp` |
| Module | Nơi Làm Việc |
| Ngày thực thi | `2026-02-27` |
| Trạng thái | ✅ Đã thực thi thành công |

---

# Các bảng được tạo

Migration tạo 3 bảng phục vụ các chức năng của module.

| Bảng | Chức năng |
|------|-----------|
| `phieu_de_xuat_dung_cu` | Phiếu đề xuất mua dụng cụ, thiết bị |
| `phieu_tam_ung` | Phiếu tạm ứng công tác hoặc chi phí |
| `don_di_muon` | Đơn đi muộn, về sớm hoặc cả hai |

---

# Yêu cầu trước khi chạy

- Đảm bảo đã kết nối đúng database `qlns_erp`
- Kiểm tra quyền tạo bảng
- Chỉ thực thi trên môi trường chưa có các bảng tương ứng
- Nếu triển khai Production, cần được xác nhận trước khi chạy

---

# SQL Migration

## 1. Tạo bảng `phieu_de_xuat_dung_cu`

```sql
CREATE TABLE IF NOT EXISTS `phieu_de_xuat_dung_cu` (
    `id`              INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `nv_ho_so_id`     INT NOT NULL,
    `ten_dung_cu`     VARCHAR(200) NOT NULL,
    `don_vi_tinh`     VARCHAR(50) NOT NULL,
    `so_luong`        INT NOT NULL DEFAULT 1,
    `gia_tien`        DECIMAL(18,2) NOT NULL DEFAULT 0,
    `tong_tien`       DECIMAL(18,2) NOT NULL DEFAULT 0,
    `ly_do`           TEXT NOT NULL,
    `trang_thai`      VARCHAR(20) NOT NULL DEFAULT 'CHO_DUYET',
    `nguoi_duyet_id`  INT NULL,
    `ngay_duyet`      DATETIME NULL,
    `ly_do_tu_choi`   TEXT NULL,
    `created_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT `fk_pdxdc_nv_ho_so`
        FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_pdxdc_nguoi_duyet`
        FOREIGN KEY (`nguoi_duyet_id`) REFERENCES `tai_khoan`(`id`) ON DELETE SET NULL,
    INDEX `idx_pdxdc_nv_ho_so_id` (`nv_ho_so_id`),
    INDEX `idx_pdxdc_trang_thai` (`trang_thai`)
);
```

### Giá trị hợp lệ của `trang_thai`

| Giá trị | Ý nghĩa |
|----------|----------|
| `CHO_DUYET` | Chờ duyệt |
| `DA_DUYET` | Đã duyệt |
| `TU_CHOI` | Từ chối |

---

## 2. Tạo bảng `phieu_tam_ung`

```sql
CREATE TABLE IF NOT EXISTS `phieu_tam_ung` (
    `id`                INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `nv_ho_so_id`       INT NOT NULL,
    `muc_dich`          VARCHAR(200) NOT NULL,
    `so_tien`           DECIMAL(18,2) NOT NULL DEFAULT 0,
    `ngay_can_tam_ung`  DATE NOT NULL,
    `ly_do`             TEXT NOT NULL,
    `trang_thai`        VARCHAR(20) NOT NULL DEFAULT 'CHO_DUYET',
    `nguoi_duyet_id`    INT NULL,
    `ngay_duyet`        DATETIME NULL,
    `ly_do_tu_choi`     TEXT NULL,
    `created_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT `fk_ptu_nv_ho_so`
        FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_ptu_nguoi_duyet`
        FOREIGN KEY (`nguoi_duyet_id`) REFERENCES `tai_khoan`(`id`) ON DELETE SET NULL,
    INDEX `idx_ptu_nv_ho_so_id` (`nv_ho_so_id`),
    INDEX `idx_ptu_trang_thai` (`trang_thai`)
);
```

### Giá trị hợp lệ của `trang_thai`

| Giá trị | Ý nghĩa |
|----------|----------|
| `CHO_DUYET` | Chờ duyệt |
| `DA_DUYET` | Đã duyệt |
| `TU_CHOI` | Từ chối |

---

## 3. Tạo bảng `don_di_muon`

```sql
CREATE TABLE IF NOT EXISTS `don_di_muon` (
    `id`                   INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `nv_ho_so_id`          INT NOT NULL,
    `loai`                 VARCHAR(20) NOT NULL COMMENT 'DI_MUON | VE_SOM | CA_HAI',
    `ngay_ap_dung`         DATE NOT NULL,
    `thoi_gian_bat_dau`    TIME NOT NULL,
    `thoi_gian_ket_thuc`   TIME NOT NULL,
    `ly_do`                TEXT NOT NULL,
    `trang_thai`           VARCHAR(20) NOT NULL DEFAULT 'CHO_DUYET',
    `nguoi_duyet_id`       INT NULL,
    `ngay_duyet`           DATETIME NULL,
    `ly_do_tu_choi`        TEXT NULL,
    `created_at`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`           DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT `fk_ddm_nv_ho_so`
        FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so`(`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_ddm_nguoi_duyet`
        FOREIGN KEY (`nguoi_duyet_id`) REFERENCES `tai_khoan`(`id`) ON DELETE SET NULL,
    INDEX `idx_ddm_nv_ho_so_id` (`nv_ho_so_id`),
    INDEX `idx_ddm_trang_thai` (`trang_thai`),
    INDEX `idx_ddm_ngay_ap_dung` (`ngay_ap_dung`)
);
```

### Giá trị hợp lệ của `loai`

| Giá trị | Ý nghĩa |
|----------|----------|
| `DI_MUON` | Đi muộn |
| `VE_SOM` | Về sớm |
| `CA_HAI` | Đi muộn và về sớm |

### Giá trị hợp lệ của `trang_thai`

| Giá trị | Ý nghĩa |
|----------|----------|
| `CHO_DUYET` | Chờ duyệt |
| `DA_DUYET` | Đã duyệt |
| `TU_CHOI` | Từ chối |

---

# Quan hệ giữa các bảng

```
nv_ho_so
│
├── phieu_de_xuat_dung_cu
├── phieu_tam_ung
└── don_di_muon

tai_khoan
│
├── nguoi_duyet_id → phieu_de_xuat_dung_cu
├── nguoi_duyet_id → phieu_tam_ung
└── nguoi_duyet_id → don_di_muon
```

---

# Rollback Migration

Nếu cần xóa toàn bộ các bảng vừa tạo:

> **Lưu ý:** Thao tác này sẽ xóa toàn bộ dữ liệu trong các bảng.

```sql
SET FOREIGN_KEY_CHECKS = 0;

DROP TABLE IF EXISTS `don_di_muon`;
DROP TABLE IF EXISTS `phieu_tam_ung`;
DROP TABLE IF EXISTS `phieu_de_xuat_dung_cu`;

SET FOREIGN_KEY_CHECKS = 1;
```

---

# Kiểm tra sau khi Migration

Kiểm tra các bảng đã được tạo thành công:

```sql
SELECT TABLE_NAME,
       TABLE_ROWS,
       CREATE_TIME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'qlns_erp'
  AND TABLE_NAME IN (
      'phieu_de_xuat_dung_cu',
      'phieu_tam_ung',
      'don_di_muon'
  );
```

### Kết quả mong đợi

- Có đúng **3 bản ghi** được trả về.
- `CREATE_TIME` khác `NULL`.
- Tên bảng đúng như thiết kế.

---

# Cấu trúc tổng thể

```
qlns_erp
│
├── nv_ho_so
├── tai_khoan
│
├── phieu_de_xuat_dung_cu
├── phieu_tam_ung
└── don_di_muon
```

---

# Các trạng thái sử dụng

## Trạng thái phê duyệt

| Giá trị | Mô tả |
|----------|--------|
| `CHO_DUYET` | Chờ người có thẩm quyền duyệt |
| `DA_DUYET` | Đã được phê duyệt |
| `TU_CHOI` | Bị từ chối |

## Loại đơn đi muộn

| Giá trị | Mô tả |
|----------|--------|
| `DI_MUON` | Xin đi làm muộn |
| `VE_SOM` | Xin về sớm |
| `CA_HAI` | Vừa đi muộn vừa về sớm |

---

# File liên quan

| Thành phần | Đường dẫn |
|------------|-----------|
| SQL Migration | `QLNS_ERP_BE/sql/noi_lam_viec_migration.sql` |
| Backend Service | `QLNS_BE/Services/NoiLamViecService.cs` |
| Backend Controller | `QLNS_BE/Controllers/NoiLamViecController.cs` |
| Frontend API Service | `src/app/core/services/api/noi-lam-viec-api.service.ts` |

---

# Checklist triển khai

- [ ] Kết nối đúng database `qlns_erp`
- [ ] Backup dữ liệu (nếu cần)
- [ ] Chạy toàn bộ script migration
- [ ] Kiểm tra 3 bảng đã được tạo
- [ ] Kiểm tra Foreign Key
- [ ] Kiểm tra Index
- [ ] Kiểm tra quyền truy cập của ứng dụng
- [ ] Khởi động Backend và kiểm tra API hoạt động
- [ ] Kiểm tra Frontend gọi API thành công
