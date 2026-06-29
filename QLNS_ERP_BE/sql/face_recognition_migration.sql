-- =====================================================
-- MIGRATION: Face Recognition System
-- Ngày tạo: 2026-02-07
-- Mô tả: Thêm các bảng và cột cho hệ thống chấm công nhận diện khuôn mặt
-- =====================================================

USE qlns_erp;

-- =====================================================
-- BƯỚC 1: TẠO BẢNG NV_FACE_DATA (Dữ liệu khuôn mặt nhân viên)
-- =====================================================
CREATE TABLE IF NOT EXISTS `nv_face_data` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nv_ho_so_id` INT NOT NULL COMMENT 'ID nhân viên',
  `face_encoding` TEXT NOT NULL COMMENT 'JSON array - 128 face embeddings',
  `face_image_url` VARCHAR(500) COMMENT 'URL ảnh khuôn mặt mẫu',
  `face_image_thumbnail` VARCHAR(500) COMMENT 'URL ảnh thumbnail',
  `is_active` TINYINT(1) DEFAULT 1 COMMENT 'Còn sử dụng không',
  `quality_score` DECIMAL(5,4) COMMENT 'Chất lượng ảnh (0-1)',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `created_by` INT COMMENT 'Tài khoản HR tạo',
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `updated_by` INT COMMENT 'Tài khoản cập nhật',
  
  KEY `FK_FACE_NV_HO_SO` (`nv_ho_so_id`),
  KEY `FK_FACE_CREATED_BY` (`created_by`),
  KEY `IDX_ACTIVE` (`is_active`),
  KEY `IDX_NV_ACTIVE` (`nv_ho_so_id`, `is_active`),
  
  CONSTRAINT `FK_FACE_NV_HO_SO` 
    FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) 
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_FACE_CREATED_BY` 
    FOREIGN KEY (`created_by`) REFERENCES `tai_khoan` (`id`) 
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci 
COMMENT='Dữ liệu khuôn mặt nhân viên cho Face Recognition';

-- =====================================================
-- BƯỚC 2: TẠO BẢNG CHAM_CONG_FACE_LOG (Lịch sử nhận diện)
-- =====================================================
CREATE TABLE IF NOT EXISTS `cham_cong_face_log` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `cham_cong_id` INT DEFAULT NULL COMMENT 'NULL nếu nhận diện thất bại',
  `nv_ho_so_id` INT DEFAULT NULL COMMENT 'Nhân viên được nhận diện',
  `thoi_gian` DATETIME NOT NULL COMMENT 'Thời điểm chấm công',
  `loai` ENUM('VAO','RA') NOT NULL,
  `face_image_url` VARCHAR(500) COMMENT 'Ảnh chụp lúc chấm công',
  `confidence_score` DECIMAL(5,4) COMMENT 'Độ tin cậy nhận diện (0-1)',
  `trang_thai` ENUM('THANH_CONG','THAT_BAI','NGHI_NGO','DA_XU_LY') NOT NULL DEFAULT 'THANH_CONG',
  `ly_do_that_bai` VARCHAR(500) COMMENT 'Lý do thất bại: không phát hiện mặt, không khớp, v.v.',
  `ghi_chu` VARCHAR(500),
  
  -- Thông tin thiết bị & bảo mật
  `ip_address` VARCHAR(50),
  `device_info` VARCHAR(200) COMMENT 'User agent, device name',
  `location` VARCHAR(200) COMMENT 'Vị trí GPS (nếu có)',
  
  -- Metadata
  `processing_time_ms` INT COMMENT 'Thời gian xử lý (ms)',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  
  KEY `FK_FACE_LOG_CHAM_CONG` (`cham_cong_id`),
  KEY `FK_FACE_LOG_NV` (`nv_ho_so_id`),
  KEY `IDX_THOI_GIAN` (`thoi_gian`),
  KEY `IDX_TRANG_THAI` (`trang_thai`),
  KEY `IDX_THOI_GIAN_TRANG_THAI` (`thoi_gian`, `trang_thai`),
  
  CONSTRAINT `FK_FACE_LOG_CHAM_CONG` 
    FOREIGN KEY (`cham_cong_id`) REFERENCES `cham_cong` (`id`) 
    ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_FACE_LOG_NV` 
    FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) 
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci 
COMMENT='Lịch sử nhận diện khuôn mặt chấm công';

-- =====================================================
-- BƯỚC 3: TẠO BẢNG FACE_RECOGNITION_CONFIG (Cấu hình hệ thống)
-- =====================================================
CREATE TABLE IF NOT EXISTS `face_recognition_config` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `key_name` VARCHAR(100) NOT NULL UNIQUE COMMENT 'confidence_threshold, max_attempts',
  `value` VARCHAR(500) NOT NULL,
  `data_type` ENUM('STRING','INT','DECIMAL','BOOLEAN','JSON') DEFAULT 'STRING',
  `description` VARCHAR(500),
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `updated_by` INT,
  
  KEY `FK_CONFIG_UPDATED_BY` (`updated_by`),
  CONSTRAINT `FK_CONFIG_UPDATED_BY` 
    FOREIGN KEY (`updated_by`) REFERENCES `tai_khoan` (`id`) 
    ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci 
COMMENT='Cấu hình hệ thống nhận diện khuôn mặt';

-- =====================================================
-- BƯỚC 4: INSERT DỮ LIỆU MẪU CHO FACE_RECOGNITION_CONFIG
-- =====================================================
INSERT INTO `face_recognition_config` (`key_name`, `value`, `data_type`, `description`) 
VALUES
  ('confidence_threshold', '0.60', 'DECIMAL', 'Ngưỡng độ tin cậy tối thiểu (0-1)'),
  ('max_face_per_employee', '3', 'INT', 'Số ảnh khuôn mặt tối đa/nhân viên'),
  ('enable_liveness_check', 'false', 'BOOLEAN', 'Kiểm tra ảnh thật (chống ảnh in)'),
  ('allow_checkin_minutes_before', '30', 'INT', 'Cho phép chấm công sớm trước giờ làm (phút)'),
  ('allow_multiple_checkin_per_day', 'false', 'BOOLEAN', 'Cho phép check-in nhiều lần/ngày')
ON DUPLICATE KEY UPDATE 
  `value` = VALUES(`value`),
  `updated_at` = CURRENT_TIMESTAMP;

-- =====================================================
-- BƯỚC 5: CẬP NHẬT BẢNG CHAM_CONG - THÊM CỘT MỚI
-- =====================================================

-- Thêm cột phuong_thuc
ALTER TABLE `cham_cong` 
ADD COLUMN IF NOT EXISTS `phuong_thuc` ENUM('MANUAL','FACE_RECOGNITION','QR_CODE','RFID_CARD','BIOMETRIC') 
  DEFAULT 'MANUAL' 
  COMMENT 'Phương thức chấm công' 
  AFTER `trang_thai`;

-- Thêm cột created_by
ALTER TABLE `cham_cong` 
ADD COLUMN IF NOT EXISTS `created_by` INT DEFAULT NULL 
  COMMENT 'Tài khoản tạo (nếu manual/HR)' 
  AFTER `phuong_thuc`;

-- Thêm cột face_log_vao_id
ALTER TABLE `cham_cong` 
ADD COLUMN IF NOT EXISTS `face_log_vao_id` INT DEFAULT NULL 
  COMMENT 'ID log nhận diện khi vào' 
  AFTER `created_by`;

-- Thêm cột face_log_ra_id
ALTER TABLE `cham_cong` 
ADD COLUMN IF NOT EXISTS `face_log_ra_id` INT DEFAULT NULL 
  COMMENT 'ID log nhận diện khi ra' 
  AFTER `face_log_vao_id`;

-- =====================================================
-- BƯỚC 6: THÊM INDEXES CHO BẢNG CHAM_CONG
-- =====================================================
-- Kiểm tra và thêm index nếu chưa tồn tại
SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND INDEX_NAME = 'FK_CHAM_CONG_CREATED_BY');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD KEY `FK_CHAM_CONG_CREATED_BY` (`created_by`)', 
  'SELECT ''Index FK_CHAM_CONG_CREATED_BY already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND INDEX_NAME = 'FK_CHAM_CONG_FACE_VAO');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD KEY `FK_CHAM_CONG_FACE_VAO` (`face_log_vao_id`)', 
  'SELECT ''Index FK_CHAM_CONG_FACE_VAO already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND INDEX_NAME = 'FK_CHAM_CONG_FACE_RA');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD KEY `FK_CHAM_CONG_FACE_RA` (`face_log_ra_id`)', 
  'SELECT ''Index FK_CHAM_CONG_FACE_RA already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND INDEX_NAME = 'IDX_PHUONG_THUC');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD KEY `IDX_PHUONG_THUC` (`phuong_thuc`)', 
  'SELECT ''Index IDX_PHUONG_THUC already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- BƯỚC 7: THÊM FOREIGN KEY CONSTRAINTS CHO BẢNG CHAM_CONG
-- =====================================================
-- Kiểm tra và thêm FK nếu chưa tồn tại
SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND CONSTRAINT_NAME = 'FK_CHAM_CONG_CREATED_BY');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD CONSTRAINT `FK_CHAM_CONG_CREATED_BY` 
   FOREIGN KEY (`created_by`) REFERENCES `tai_khoan` (`id`) 
   ON DELETE SET NULL ON UPDATE CASCADE', 
  'SELECT ''FK FK_CHAM_CONG_CREATED_BY already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND CONSTRAINT_NAME = 'FK_CHAM_CONG_FACE_VAO');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD CONSTRAINT `FK_CHAM_CONG_FACE_VAO` 
   FOREIGN KEY (`face_log_vao_id`) REFERENCES `cham_cong_face_log` (`id`) 
   ON DELETE SET NULL ON UPDATE CASCADE', 
  'SELECT ''FK FK_CHAM_CONG_FACE_VAO already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS 
               WHERE TABLE_SCHEMA = 'qlns_erp' 
               AND TABLE_NAME = 'cham_cong' 
               AND CONSTRAINT_NAME = 'FK_CHAM_CONG_FACE_RA');
SET @sqlstmt := IF(@exist = 0, 
  'ALTER TABLE `cham_cong` ADD CONSTRAINT `FK_CHAM_CONG_FACE_RA` 
   FOREIGN KEY (`face_log_ra_id`) REFERENCES `cham_cong_face_log` (`id`) 
   ON DELETE SET NULL ON UPDATE CASCADE', 
  'SELECT ''FK FK_CHAM_CONG_FACE_RA already exists'' AS msg');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- =====================================================
-- HOÀN TẤT MIGRATION
-- =====================================================
SELECT 'Face Recognition Migration completed successfully!' AS status;

-- Kiểm tra kết quả
SELECT 
  'nv_face_data' AS table_name, 
  COUNT(*) AS record_count 
FROM nv_face_data
UNION ALL
SELECT 
  'cham_cong_face_log', 
  COUNT(*) 
FROM cham_cong_face_log
UNION ALL
SELECT 
  'face_recognition_config', 
  COUNT(*) 
FROM face_recognition_config
UNION ALL
SELECT 
  'cham_cong (MANUAL)', 
  COUNT(*) 
FROM cham_cong 
WHERE phuong_thuc = 'MANUAL';
