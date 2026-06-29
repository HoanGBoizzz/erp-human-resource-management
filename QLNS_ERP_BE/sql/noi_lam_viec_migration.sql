-- ============================================================
-- NƠI LÀM VIỆC: Migration script
-- Tạo 3 bảng: PHIEU_DE_XUAT_DUNG_CU, PHIEU_TAM_UNG, DON_DI_MUON
-- ============================================================

-- 1. PHIẾU ĐỀ XUẤT DỤNG CỤ + THANH TOÁN
CREATE TABLE IF NOT EXISTS `PHIEU_DE_XUAT_DUNG_CU` (
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `nv_ho_so_id` INT NOT NULL,
    `ten_dung_cu` VARCHAR(200) NOT NULL,
    `don_vi_tinh` VARCHAR(50) NOT NULL,
    `so_luong` INT NOT NULL DEFAULT 1,
    `gia_tien` DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `tong_tien` DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `ly_do` TEXT NOT NULL,
    `trang_thai` VARCHAR(20) NOT NULL DEFAULT 'CHO_DUYET' COMMENT 'CHO_DUYET | DA_DUYET | TU_CHOI',
    `nguoi_duyet_id` INT NULL,
    `ngay_duyet` DATETIME NULL,
    `ly_do_tu_choi` TEXT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT `fk_pdxdc_nv_ho_so` FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_pdxdc_nguoi_duyet` FOREIGN KEY (`nguoi_duyet_id`) REFERENCES `tai_khoan` (`id`) ON DELETE SET NULL,
    INDEX `idx_pdxdc_nv_ho_so_id` (`nv_ho_so_id`),
    INDEX `idx_pdxdc_trang_thai` (`trang_thai`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Phiếu đề xuất mua dụng cụ';

-- 2. PHIẾU TẠM ỨNG
CREATE TABLE IF NOT EXISTS `PHIEU_TAM_UNG` (
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `nv_ho_so_id` INT NOT NULL,
    `muc_dich` VARCHAR(200) NOT NULL,
    `so_tien` DECIMAL(18, 2) NOT NULL DEFAULT 0,
    `ngay_can_tam_ung` DATE NOT NULL,
    `ly_do` TEXT NOT NULL,
    `trang_thai` VARCHAR(20) NOT NULL DEFAULT 'CHO_DUYET' COMMENT 'CHO_DUYET | DA_DUYET | TU_CHOI',
    `nguoi_duyet_id` INT NULL,
    `ngay_duyet` DATETIME NULL,
    `ly_do_tu_choi` TEXT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT `fk_ptu_nv_ho_so` FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_ptu_nguoi_duyet` FOREIGN KEY (`nguoi_duyet_id`) REFERENCES `tai_khoan` (`id`) ON DELETE SET NULL,
    INDEX `idx_ptu_nv_ho_so_id` (`nv_ho_so_id`),
    INDEX `idx_ptu_trang_thai` (`trang_thai`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Phiếu tạm ứng';

-- 3. ĐƠN XIN ĐI MUỘN / VỀ SỚM
CREATE TABLE IF NOT EXISTS `DON_DI_MUON` (
    `id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `nv_ho_so_id` INT NOT NULL,
    `loai` VARCHAR(20) NOT NULL COMMENT 'DI_MUON | VE_SOM | CA_HAI',
    `ngay_ap_dung` DATE NOT NULL,
    `thoi_gian_bat_dau` TIME NOT NULL,
    `thoi_gian_ket_thuc` TIME NOT NULL,
    `ly_do` TEXT NOT NULL,
    `trang_thai` VARCHAR(20) NOT NULL DEFAULT 'CHO_DUYET' COMMENT 'CHO_DUYET | DA_DUYET | TU_CHOI',
    `nguoi_duyet_id` INT NULL,
    `ngay_duyet` DATETIME NULL,
    `ly_do_tu_choi` TEXT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT `fk_ddm_nv_ho_so` FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_ddm_nguoi_duyet` FOREIGN KEY (`nguoi_duyet_id`) REFERENCES `tai_khoan` (`id`) ON DELETE SET NULL,
    INDEX `idx_ddm_nv_ho_so_id` (`nv_ho_so_id`),
    INDEX `idx_ddm_trang_thai` (`trang_thai`),
    INDEX `idx_ddm_ngay_ap_dung` (`ngay_ap_dung`)
) ENGINE = InnoDB DEFAULT CHARSET = utf8mb4 COLLATE = utf8mb4_unicode_ci COMMENT = 'Đơn xin đi muộn / về sớm';