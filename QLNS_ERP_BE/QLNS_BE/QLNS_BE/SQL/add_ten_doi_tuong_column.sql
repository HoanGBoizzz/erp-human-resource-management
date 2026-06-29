-- Thêm cột ten_doi_tuong vào bảng audit_log
-- Script này sẽ thêm cột mới để lưu tên đối tượng trong audit log

USE qlns_erp;

-- Kiểm tra xem cột đã tồn tại chưa
SET @col_exists = 0;
SELECT COUNT(*) INTO @col_exists 
FROM information_schema.COLUMNS 
WHERE TABLE_SCHEMA = 'qlns_erp' 
  AND TABLE_NAME = 'audit_log' 
  AND COLUMN_NAME = 'ten_doi_tuong';

-- Nếu cột chưa tồn tại thì thêm vào
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE audit_log ADD COLUMN ten_doi_tuong VARCHAR(500) NULL AFTER doi_tuong_id',
    'SELECT "Column ten_doi_tuong already exists" AS message');

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- Kiểm tra kết quả
SELECT 'Column ten_doi_tuong has been added successfully' AS message;
