-- Migration: Thêm cột GioVaoOt và GioRaOt vào bảng cham_cong (MySQL)
-- Hỗ trợ tính năng chấm công tăng ca (OT) tự động
-- Ngày: 2026-03-08
-- ✅ ĐÃ CHẠY THÀNH CÔNG

ALTER TABLE cham_cong
ADD COLUMN GioVaoOt DATETIME NULL, -- Giờ bắt đầu tăng ca (do nhân viên xác nhận)
ADD COLUMN GioRaOt DATETIME NULL;
-- Giờ kết thúc tăng ca (tự động tính SoGioOt)

-- Ghi chú:
-- SoGioOt sẽ được cập nhật tự động khi nhân viên chấm công RA ca OT:
--   SoGioOt = DATEDIFF(MINUTE, GioVaoOt, GioRaOt) / 60.0
-- GioVaoOt được set khi nhân viên xác nhận "Vào tăng ca" (sau khi đã hoàn tất ca thường)
-- GioRaOt được set khi nhân viên chấm công ra trong lúc GioVaoOt đã có và GioRaOt chưa có