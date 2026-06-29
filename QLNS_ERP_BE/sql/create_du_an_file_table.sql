-- Migration: Add du_an_file table for multi-file support
-- Run this in MySQL
-- IMPORTANT: Use snake_case to match EF Core naming convention

DROP TABLE IF EXISTS du_an_file;

CREATE TABLE du_an_file (
    id INT PRIMARY KEY AUTO_INCREMENT,
    du_an_id INT NOT NULL,
    ten_file VARCHAR(255) NOT NULL,
    duong_dan_file VARCHAR(500) NOT NULL,
    kich_thuoc BIGINT NULL,
    loai_file VARCHAR(100) NULL,
    ngay_tao DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    tai_khoan_tao_id INT NULL,
    
    CONSTRAINT fk_du_an_file_du_an FOREIGN KEY (du_an_id) 
        REFERENCES du_an(id) ON DELETE CASCADE,
    CONSTRAINT fk_du_an_file_tai_khoan FOREIGN KEY (tai_khoan_tao_id) 
        REFERENCES tai_khoan(id) ON DELETE SET NULL,
        
    INDEX idx_du_an_file_du_an_id (du_an_id),
    INDEX idx_du_an_file_ngay_tao (ngay_tao)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
