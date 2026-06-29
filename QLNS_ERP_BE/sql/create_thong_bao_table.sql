-- ====================================================
-- Table: THONG_BAO (Thông báo realtime)
-- Chạy script này để tạo bảng trước khi dùng tính năng
-- ====================================================

CREATE TABLE IF NOT EXISTS THONG_BAO (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL COMMENT 'Người nhận thông báo',
  sender_id INT NULL COMMENT 'Người gửi (optional)',
  title VARCHAR(255) NOT NULL COMMENT 'Tiêu đề',
  message TEXT NULL COMMENT 'Nội dung chi tiết',
  type VARCHAR(50) NOT NULL DEFAULT 'THONG_BAO' COMMENT 'Loại: YEU_CAU_DUYET, DA_DUYET, TU_CHOI, THONG_BAO',
  related_entity VARCHAR(50) NULL COMMENT 'Entity liên quan: DON_PHEP, BANG_LUONG, DU_AN',
  related_id INT NULL COMMENT 'ID của entity liên quan',
  link VARCHAR(255) NULL COMMENT 'URL để navigate khi click',
  is_read TINYINT(1) NOT NULL DEFAULT 0 COMMENT 'Đã đọc chưa',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT 'Thời gian tạo',
  
  -- Foreign Keys
  CONSTRAINT fk_thong_bao_user FOREIGN KEY (user_id) 
    REFERENCES TAI_KHOAN(id) ON DELETE CASCADE,
  CONSTRAINT fk_thong_bao_sender FOREIGN KEY (sender_id) 
    REFERENCES TAI_KHOAN(id) ON DELETE SET NULL,
  
  -- Index for fast unread queries
  INDEX idx_thong_bao_user_unread (user_id, is_read)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ====================================================
-- Verify table created
-- ====================================================
-- SELECT * FROM thong_bao LIMIT 10;
