-- ================================================
-- QLNS ERP - DATABASE UPDATE MIGRATION
-- Ngày tạo: 2026-01-08
-- Mục đích: Thêm các tính năng mới
--   1. Upload ảnh STK ngân hàng
--   2. Task management (Trưởng phòng giao việc)
--   3. Login fail tracking (khóa 15 phút sau 3 lần sai)
--   4. Account warning management (HR quản lý tài khoản)
--   5. Role TRUONG_PHONG
-- ================================================

USE `qlns_erp`;

-- ================================================
-- 1. CẬP NHẬT BẢNG nv_ho_so - THÊM TRƯỜNG ẢNH STK
-- ================================================

ALTER TABLE `nv_ho_so` 
ADD COLUMN `anh_stk_url` VARCHAR(500) NULL COMMENT 'URL ảnh sao kê tài khoản ngân hàng' 
AFTER `so_tai_khoan_ngan_hang`;

-- ================================================
-- 2. CẬP NHẬT BẢNG tai_khoan - LOGIN TRACKING & WARNING
-- ================================================

ALTER TABLE `tai_khoan`
ADD COLUMN `so_lan_dang_nhap_sai` INT NOT NULL DEFAULT 0 COMMENT 'Số lần đăng nhập sai liên tiếp',
ADD COLUMN `thoi_gian_khoa` DATETIME NULL COMMENT 'Thời điểm tài khoản bị khóa do đăng nhập sai',
ADD COLUMN `trang_thai_canh_bao` ENUM('BINH_THUONG', 'CANH_BAO', 'CAM') NOT NULL DEFAULT 'BINH_THUONG' COMMENT 'Trạng thái cảnh báo tài khoản',
ADD COLUMN `ly_do_canh_bao` VARCHAR(500) NULL COMMENT 'Lý do cảnh báo/cấm tài khoản',
ADD COLUMN `tai_khoan_canh_bao_boi_id` INT NULL COMMENT 'ID tài khoản HR/Admin đánh cảnh báo',
ADD COLUMN `ngay_canh_bao` DATETIME NULL COMMENT 'Ngày đánh cảnh báo';

ALTER TABLE `tai_khoan`
ADD CONSTRAINT `FK_TAI_KHOAN_CANH_BAO` 
FOREIGN KEY (`tai_khoan_canh_bao_boi_id`) REFERENCES `tai_khoan` (`id`) 
ON DELETE SET NULL ON UPDATE CASCADE;

-- ================================================
-- 3. TẠO BẢNG du_an_task - TASK MANAGEMENT
-- ================================================

CREATE TABLE `du_an_task` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `du_an_id` INT NOT NULL COMMENT 'ID dự án',
  `tieu_de` VARCHAR(200) NOT NULL COMMENT 'Tiêu đề task',
  `mo_ta` TEXT NULL COMMENT 'Mô tả chi tiết task',
  `nhan_vien_id` INT NOT NULL COMMENT 'ID nhân viên được giao',
  `nguoi_giao_id` INT NOT NULL COMMENT 'ID người giao (Trưởng phòng)',
  `ngay_bat_dau` DATE NULL COMMENT 'Ngày bắt đầu dự kiến',
  `ngay_ket_thuc` DATE NULL COMMENT 'Deadline',
  `uu_tien` ENUM('THAP', 'BINH_THUONG', 'CAO', 'KHAN_CAP') NOT NULL DEFAULT 'BINH_THUONG' COMMENT 'Mức độ ưu tiên',
  `trang_thai` ENUM('MOI', 'DANG_LAM', 'CHO_REVIEW', 'HOAN_THANH', 'HUY') NOT NULL DEFAULT 'MOI' COMMENT 'Trạng thái task',
  `phan_tram_hoan_thanh` INT NOT NULL DEFAULT 0 COMMENT 'Tiến độ (%)',
  `ghi_chu` TEXT NULL COMMENT 'Ghi chú của nhân viên',
  `ngay_hoan_thanh` DATETIME NULL COMMENT 'Ngày hoàn thành thực tế',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`),
  INDEX `idx_du_an_task_du_an` (`du_an_id`),
  INDEX `idx_du_an_task_nhan_vien` (`nhan_vien_id`),
  INDEX `idx_du_an_task_nguoi_giao` (`nguoi_giao_id`),
  INDEX `idx_du_an_task_trang_thai` (`trang_thai`),
  INDEX `idx_du_an_task_uu_tien` (`uu_tien`),
  CONSTRAINT `FK_DU_AN_TASK_DU_AN` 
    FOREIGN KEY (`du_an_id`) REFERENCES `du_an` (`id`) 
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_DU_AN_TASK_NHAN_VIEN` 
    FOREIGN KEY (`nhan_vien_id`) REFERENCES `nv_ho_so` (`id`) 
    ON DELETE RESTRICT ON UPDATE CASCADE,
  CONSTRAINT `FK_DU_AN_TASK_NGUOI_GIAO` 
    FOREIGN KEY (`nguoi_giao_id`) REFERENCES `nv_ho_so` (`id`) 
    ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci 
COMMENT='Task công việc trong dự án - giao bởi trưởng phòng';

-- ================================================
-- 4. THÊM ROLE TRUONG_PHONG
-- ================================================

INSERT INTO `vai_tro` (`ten_vai_tro`, `mo_ta`) 
VALUES ('TRUONG_PHONG', 'Trưởng phòng - Quản lý phòng ban, giao task trong dự án');

-- ================================================
-- 5. DỮ LIỆU MẪU - TÀI KHOẢN TRƯỞNG PHÒNG
-- ================================================

-- Tạo tài khoản trưởng phòng IT (NV001 - Nguyễn Văn A)
-- Password mặc định: "TruongPhong@123" (cần hash thật trong code)
UPDATE `tai_khoan` 
SET `vai_tro_id` = (SELECT `id` FROM `vai_tro` WHERE `ten_vai_tro` = 'TRUONG_PHONG' LIMIT 1)
WHERE `ten_dang_nhap` = 'nv002';

-- Tạo tài khoản trưởng phòng Kế toán (NV002 - Lê Thị B)
UPDATE `tai_khoan` 
SET `vai_tro_id` = (SELECT `id` FROM `vai_tro` WHERE `ten_vai_tro` = 'TRUONG_PHONG' LIMIT 1)
WHERE `ten_dang_nhap` = 'NV003';

-- ================================================
-- 6. DỮ LIỆU MẪU - TASKS
-- ================================================

-- Task mẫu cho dự án (giả sử dự án ID = 1 tồn tại)
INSERT INTO `du_an_task` 
(`du_an_id`, `tieu_de`, `mo_ta`, `nhan_vien_id`, `nguoi_giao_id`, `ngay_bat_dau`, `ngay_ket_thuc`, `uu_tien`, `trang_thai`, `phan_tram_hoan_thanh`, `created_at`)
VALUES
-- Task từ Trưởng phòng IT (NV001) giao cho các nhân viên
(1, 'Thiết kế Database Schema cho Module Lương', 
 'Thiết kế cấu trúc database bao gồm: bảng lương tháng, lương cơ bản, phụ cấp, khấu trừ. Cần tuân thủ chuẩn normalization và có index hợp lý.', 
 5, 1, '2026-01-10', '2026-01-15', 'CAO', 'DANG_LAM', 60, NOW()),

(1, 'Xây dựng API Authentication & Authorization', 
 'Tạo các API: Login, Register, Forgot Password, Change Password. Implement JWT token với refresh token. Role-based access control.', 
 6, 1, '2026-01-12', '2026-01-20', 'KHAN_CAP', 'MOI', 0, NOW()),

(1, 'Viết Unit Test cho Module Chấm Công', 
 'Viết unit test cho tất cả business logic trong module chấm công. Đảm bảo coverage ít nhất 80%. Sử dụng xUnit và Moq.', 
 7, 1, '2026-01-15', '2026-01-25', 'BINH_THUONG', 'MOI', 0, NOW()),

(1, 'Review và Optimize SQL Queries', 
 'Analyze slow queries trong module lương. Thêm index, optimize JOIN. Benchmark trước và sau optimize.', 
 8, 1, '2026-01-08', '2026-01-12', 'CAO', 'HOAN_THANH', 100, NOW()),

-- Task từ Trưởng phòng Kế toán (NV002) giao
(1, 'Kiểm tra quy trình tính lương', 
 'Đối chiếu công thức tính lương với quy định mới của công ty. Cập nhật tài liệu nếu cần.', 
 9, 2, '2026-01-09', '2026-01-14', 'CAO', 'DANG_LAM', 40, NOW()),

(1, 'Chuẩn bị báo cáo thuế thu nhập cá nhân Q4/2025', 
 'Export dữ liệu lương Q4, tính toán thuế TNCN theo luật mới, chuẩn bị file Excel gửi cơ quan thuế.', 
 10, 2, '2026-01-05', '2026-01-18', 'KHAN_CAP', 'CHO_REVIEW', 90, NOW());

-- ================================================
-- 7. DỮ LIỆU MẪU - ẢNH STK (chỉ URL mẫu)
-- ================================================

-- Update một số nhân viên có ảnh STK (trong thực tế sẽ upload thật)
UPDATE `nv_ho_so` SET `anh_stk_url` = '/uploads/stk/nv001_stk_20260108.jpg' WHERE `ma_nhan_vien` = 'NV001';
UPDATE `nv_ho_so` SET `anh_stk_url` = '/uploads/stk/nv002_stk_20260108.jpg' WHERE `ma_nhan_vien` = 'NV002';
UPDATE `nv_ho_so` SET `anh_stk_url` = '/uploads/stk/nv005_stk_20260107.jpg' WHERE `ma_nhan_vien` = 'NV005';

-- ================================================
-- 8. DỮ LIỆU MẪU - TÀI KHOẢN BỊ CẢNH BÁO
-- ================================================

-- Giả sử tài khoản NV010 có hành vi vi phạm
UPDATE `tai_khoan` 
SET 
  `trang_thai_canh_bao` = 'CANH_BAO',
  `ly_do_canh_bao` = 'Đến muộn liên tục 5 lần trong tháng 12/2025',
  `tai_khoan_canh_bao_boi_id` = (SELECT `id` FROM `tai_khoan` WHERE `ten_dang_nhap` = 'hr_ketoan' LIMIT 1),
  `ngay_canh_bao` = '2026-01-05 09:00:00'
WHERE `nv_ho_so_id` = 10;

-- ================================================
-- 9. THÊM INDEX ĐỂ TỐI ƯU PERFORMANCE
-- ================================================

-- Index cho login tracking (query tài khoản bị khóa)
CREATE INDEX `idx_tai_khoan_login_tracking` 
ON `tai_khoan` (`thoi_gian_khoa`, `so_lan_dang_nhap_sai`);

-- Index cho account warning management
CREATE INDEX `idx_tai_khoan_canh_bao` 
ON `tai_khoan` (`trang_thai_canh_bao`, `ngay_canh_bao`);

-- ================================================
-- 10. ADD AUDIT LOG CHO CÁC THAY ĐỔI QUAN TRỌNG
-- ================================================

INSERT INTO `audit_log` 
(`tai_khoan_id`, `bang`, `doi_tuong_id`, `hanh_dong`, `ghi_chu`, `TenDoiTuong`)
VALUES
(3, 'DATABASE', NULL, 'MIGRATION', 'Chạy migration: Thêm tính năng upload STK, task management, login tracking, account warning', 'Database Update 2026-01-08');

-- ================================================
-- KẾT THÚC MIGRATION
-- ================================================

-- Verify changes
SELECT 'Migration completed successfully!' AS status;

-- Show new role
SELECT * FROM `vai_tro` WHERE `ten_vai_tro` = 'TRUONG_PHONG';

-- Show sample tasks
SELECT 
  t.id,
  t.tieu_de,
  d.ten_du_an,
  nv.ho_ten AS nhan_vien,
  ng.ho_ten AS nguoi_giao,
  t.trang_thai,
  t.phan_tram_hoan_thanh
FROM `du_an_task` t
JOIN `du_an` d ON t.du_an_id = d.id
JOIN `nv_ho_so` nv ON t.nhan_vien_id = nv.id
JOIN `nv_ho_so` ng ON t.nguoi_giao_id = ng.id
ORDER BY t.created_at DESC
LIMIT 10;

-- Show accounts with bank account images
SELECT 
  ma_nhan_vien,
  ho_ten,
  so_tai_khoan_ngan_hang,
  CASE WHEN anh_stk_url IS NOT NULL THEN 'Đã upload' ELSE 'Chưa upload' END AS trang_thai_anh_stk
FROM `nv_ho_so`
WHERE so_tai_khoan_ngan_hang IS NOT NULL
LIMIT 10;

-- Show warned accounts
SELECT 
  tk.ten_dang_nhap,
  nv.ho_ten,
  tk.trang_thai_canh_bao,
  tk.ly_do_canh_bao,
  tk.ngay_canh_bao
FROM `tai_khoan` tk
LEFT JOIN `nv_ho_so` nv ON tk.nv_ho_so_id = nv.id
WHERE tk.trang_thai_canh_bao != 'BINH_THUONG';
