-- =====================================================
-- ROLLBACK: Face Recognition System
-- Ngày tạo: 2026-02-07
-- Mô tả: Script rollback để xóa tất cả thay đổi của face recognition
-- CẢNH BÁO: Script này sẽ XÓA TẤT CẢ dữ liệu face recognition!
-- =====================================================

USE qlns_erp;

-- =====================================================
-- BƯỚC 1: XÓA FOREIGN KEY CONSTRAINTS TỪ BẢNG CHAM_CONG
-- =====================================================
ALTER TABLE `cham_cong` DROP FOREIGN KEY IF EXISTS `FK_CHAM_CONG_CREATED_BY`;
ALTER TABLE `cham_cong` DROP FOREIGN KEY IF EXISTS `FK_CHAM_CONG_FACE_VAO`;
ALTER TABLE `cham_cong` DROP FOREIGN KEY IF EXISTS `FK_CHAM_CONG_FACE_RA`;

-- =====================================================
-- BƯỚC 2: XÓA INDEXES TỪ BẢNG CHAM_CONG
-- =====================================================
ALTER TABLE `cham_cong` DROP INDEX IF EXISTS `FK_CHAM_CONG_CREATED_BY`;
ALTER TABLE `cham_cong` DROP INDEX IF EXISTS `FK_CHAM_CONG_FACE_VAO`;
ALTER TABLE `cham_cong` DROP INDEX IF EXISTS `FK_CHAM_CONG_FACE_RA`;
ALTER TABLE `cham_cong` DROP INDEX IF EXISTS `IDX_PHUONG_THUC`;

-- =====================================================
-- BƯỚC 3: XÓA CÁC CỘT MỚI TỪ BẢNG CHAM_CONG
-- =====================================================
ALTER TABLE `cham_cong` DROP COLUMN IF EXISTS `face_log_ra_id`;
ALTER TABLE `cham_cong` DROP COLUMN IF EXISTS `face_log_vao_id`;
ALTER TABLE `cham_cong` DROP COLUMN IF EXISTS `created_by`;
ALTER TABLE `cham_cong` DROP COLUMN IF EXISTS `phuong_thuc`;

-- =====================================================
-- BƯỚC 4: XÓA CÁC BẢNG MỚI
-- =====================================================
DROP TABLE IF EXISTS `cham_cong_face_log`;
DROP TABLE IF EXISTS `nv_face_data`;
DROP TABLE IF EXISTS `face_recognition_config`;

-- =====================================================
-- HOÀN TẤT ROLLBACK
-- =====================================================
SELECT 'Face Recognition Rollback completed!' AS status;
SELECT 'Database has been restored to state before Face Recognition migration.' AS message;
