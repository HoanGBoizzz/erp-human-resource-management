-- ============================================================
-- ĐỀ XUẤT GIÁM ĐỐC: Migration script
-- Tạo bảng DE_XUAT_GIAM_DOC
-- Trạng thái: NHAP | CHO_DUYET | DA_DUYET | TU_CHOI | DA_THU_HOI
-- ============================================================

CREATE TABLE IF NOT EXISTS `DE_XUAT_GIAM_DOC` (
    `id`                        INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `ten_de_xuat`               VARCHAR(300) NOT NULL COMMENT 'Tên đề xuất',
    `mo_ta`                     TEXT NULL COMMENT 'Mô tả chi tiết đề xuất',
    `ngay_de_xuat`              DATE NOT NULL COMMENT 'Ngày đề xuất',

-- File đính kèm
`tep_tin_url` VARCHAR(500) NULL COMMENT 'Đường dẫn file đính kèm',
`tep_tin_ten_goc` VARCHAR(300) NULL COMMENT 'Tên gốc file đính kèm',
`tep_tin_mime` VARCHAR(100) NULL COMMENT 'MIME type file',
`tep_tin_size` BIGINT NULL COMMENT 'Kích thước file (bytes)',

-- Trạng thái workflow
`trang_thai` VARCHAR(20) NOT NULL DEFAULT 'NHAP' COMMENT 'NHAP | CHO_DUYET | DA_DUYET | TU_CHOI | DA_THU_HOI',

-- Người tạo (HR)
`tai_khoan_tao_id` INT NOT NULL,

-- Người duyệt (Giám đốc)
`tai_khoan_duyet_id` INT NULL,
`ngay_gui_duyet` DATETIME NULL COMMENT 'Ngày HR gửi duyệt',
`ngay_duyet` DATETIME NULL COMMENT 'Ngày giám đốc duyệt/từ chối',
`ly_do_tu_choi` TEXT NULL COMMENT 'Lý do từ chối (nếu có)',

-- Timestamp
`created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
`updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

-- FK
CONSTRAINT `fk_dxgd_tai_khoan_tao` FOREIGN KEY (`tai_khoan_tao_id`) REFERENCES `TAI_KHOAN` (`id`) ON DELETE RESTRICT,
CONSTRAINT `fk_dxgd_tai_khoan_duyet` FOREIGN KEY (`tai_khoan_duyet_id`) REFERENCES `TAI_KHOAN` (`id`) ON DELETE SET NULL,

-- Index
INDEX `idx_dxgd_tai_khoan_tao` (`tai_khoan_tao_id`),
    INDEX `idx_dxgd_trang_thai` (`trang_thai`),
    INDEX `idx_dxgd_ngay_de_xuat` (`ngay_de_xuat`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci
  COMMENT = 'Đề xuất gửi giám đốc duyệt';