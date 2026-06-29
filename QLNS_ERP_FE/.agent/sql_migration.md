# SQL Migration — Module Nơi Làm Việc

## Mô tả

Migration tạo 3 bảng mới phục vụ module **Nơi Làm Việc** (phiếu đề xuất dụng cụ, phiếu tạm ứng, đơn đi muộn/về sớm).

- **Database**: `qlns_erp`
- **Ngày thực thi**: 2026-02-27
- **Trạng thái**: ✅ Đã thực thi thành công

---

## Các bảng được tạo

| Tên bảng | Mô tả |
|---|---|
| `phieu_de_xuat_dung_cu` | Phiếu đề xuất mua dụng cụ/thiết bị |
| `phieu_tam_ung` | Phiếu tạm ứng tiền công tác / chi phí |
| `don_di_muon` | Đơn xin đi muộn / về sớm / ca hai |

---

## Script SQL

> ⚠️ **Yêu cầu**: Script dưới đây cần được **ACCEPT / xác nhận** trước khi thực thi nếu chạy lại trên môi trường mới.
> Chỉ chạy khi đã kết nối đúng database `qlns_erp`.

### 1. Bảng `phieu_de_xuat_dung_cu`

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

**Giá trị `trang_thai` hợp lệ**: `CHO_DUYET` | `DA_DUYET` | `TU_CHOI`

---

### 2. Bảng `phieu_tam_ung`

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

---

### 3. Bảng `don_di_muon`

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

**Giá trị `loai` hợp lệ**: `DI_MUON` | `VE_SOM` | `CA_HAI`

---

## Rollback

Nếu cần xóa các bảng đã tạo:

```sql
-- ⚠️ NGUY HIỂM: Xóa toàn bộ dữ liệu. Chỉ chạy khi đã xác nhận!
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `don_di_muon`;
DROP TABLE IF EXISTS `phieu_tam_ung`;
DROP TABLE IF EXISTS `phieu_de_xuat_dung_cu`;
SET FOREIGN_KEY_CHECKS = 1;
```

---

## Xác minh sau thực thi

```sql
SELECT TABLE_NAME, TABLE_ROWS, CREATE_TIME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'qlns_erp'
  AND TABLE_NAME IN ('phieu_de_xuat_dung_cu', 'phieu_tam_ung', 'don_di_muon');
```

**Kết quả mong đợi**: 3 rows trả về với `CREATE_TIME` hợp lệ.

---

## Quan hệ bảng

```
nv_ho_so (id) ←── nv_ho_so_id ─── phieu_de_xuat_dung_cu
nv_ho_so (id) ←── nv_ho_so_id ─── phieu_tam_ung
nv_ho_so (id) ←── nv_ho_so_id ─── don_di_muon

tai_khoan (id) ←── nguoi_duyet_id ─── phieu_de_xuat_dung_cu
tai_khoan (id) ←── nguoi_duyet_id ─── phieu_tam_ung
tai_khoan (id) ←── nguoi_duyet_id ─── don_di_muon
```

---

## File nguồn

- SQL gốc: `QLNS_ERP_BE/sql/noi_lam_viec_migration.sql`
- BE Service: `QLNS_BE/Services/NoiLamViecService.cs`
- BE Controller: `QLNS_BE/Controllers/NoiLamViecController.cs`
- FE API Service: `src/app/core/services/api/noi-lam-viec-api.service.ts`
