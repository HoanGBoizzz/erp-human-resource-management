-- =====================================================
-- MIGRATION: Hệ thống tính lương chuyên nghiệp
-- Ngày: 2026-02-24
-- =====================================================

-- 1. Bảng LOẠI PHỤ CẤP (danh mục master)
CREATE TABLE IF NOT EXISTS PHU_CAP_LOAI (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    TenPhuCap VARCHAR(100) NOT NULL,
    MoTa VARCHAR(255) NULL,
    LaCoDinh TINYINT(1) NOT NULL DEFAULT 1 COMMENT '1=cố định hàng tháng, 0=biến đổi',
    DonVi VARCHAR(20) NOT NULL DEFAULT 'VND',
    ThuTu INT NOT NULL DEFAULT 0,
    DangHoatDong TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Dữ liệu mẫu loại phụ cấp
INSERT IGNORE INTO
    PHU_CAP_LOAI (
        Id,
        TenPhuCap,
        MoTa,
        LaCoDinh,
        ThuTu
    )
VALUES (
        1,
        'Phụ cấp ăn trưa',
        'Hỗ trợ bữa ăn trưa tại công ty',
        1,
        1
    ),
    (
        2,
        'Phụ cấp điện thoại',
        'Phụ cấp sử dụng điện thoại công việc',
        1,
        2
    ),
    (
        3,
        'Phụ cấp xăng xe',
        'Hỗ trợ xăng xe đi lại',
        1,
        3
    ),
    (
        4,
        'Phụ cấp nhà ở',
        'Trợ cấp thuê nhà cho nhân viên',
        1,
        4
    ),
    (
        5,
        'Phụ cấp chức vụ',
        'Phụ cấp theo chức vụ / vị trí quản lý',
        1,
        5
    ),
    (
        6,
        'Phụ cấp trách nhiệm',
        'Phụ cấp trách nhiệm công việc đặc thù',
        1,
        6
    ),
    (
        7,
        'Phụ cấp độc hại',
        'Phụ cấp môi trường làm việc độc hại, nguy hiểm',
        1,
        7
    ),
    (
        8,
        'Phụ cấp thâm niên',
        'Tính theo số năm công tác',
        1,
        8
    );

-- 2. Bảng PHỤ CẤP NHÂN VIÊN (itemized, per-employee)
CREATE TABLE IF NOT EXISTS NV_PHU_CAP (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    NvHoSoId INT NOT NULL,
    PhuCapLoaiId INT NOT NULL,
    SoTien DECIMAL(18, 2) NOT NULL DEFAULT 0,
    NgayBatDau DATE NOT NULL,
    NgayKetThuc DATE NULL,
    DangApDung TINYINT(1) NOT NULL DEFAULT 1,
    GhiChu VARCHAR(255) NULL,
    TaiKhoanTaoId INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_NV_PHU_CAP_NV FOREIGN KEY (NvHoSoId) REFERENCES NV_HO_SO (Id) ON DELETE RESTRICT,
    CONSTRAINT FK_NV_PHU_CAP_LOAI FOREIGN KEY (PhuCapLoaiId) REFERENCES PHU_CAP_LOAI (Id) ON DELETE RESTRICT
);

CREATE INDEX IDX_NV_PHU_CAP_NV ON NV_PHU_CAP (NvHoSoId);

CREATE INDEX IDX_NV_PHU_CAP_LOAI ON NV_PHU_CAP (PhuCapLoaiId);

-- 3. Bảng CHI TIẾT THƯỞNG / KHẤU TRỪ trong bảng lương
CREATE TABLE IF NOT EXISTS BANG_LUONG_ITEM (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BangLuongThangId INT NOT NULL,
    Loai ENUM('THUONG', 'KHAU_TRU') NOT NULL COMMENT 'THUONG=cộng vào lương, KHAU_TRU=trừ vào lương',
    LyDo VARCHAR(255) NOT NULL,
    SoTien DECIMAL(18, 2) NOT NULL,
    TaiKhoanTaoId INT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT FK_BL_ITEM_BL FOREIGN KEY (BangLuongThangId) REFERENCES BANG_LUONG_THANG (Id) ON DELETE CASCADE
);

CREATE INDEX IDX_BL_ITEM_BL ON BANG_LUONG_ITEM (BangLuongThangId);

-- =====================================================
-- KIỂM TRA
-- =====================================================
SELECT 'PHU_CAP_LOAI' AS Bang, COUNT(*) AS SoBanGhi
FROM PHU_CAP_LOAI
UNION ALL
SELECT 'NV_PHU_CAP', COUNT(*)
FROM NV_PHU_CAP
UNION ALL
SELECT 'BANG_LUONG_ITEM', COUNT(*)
FROM BANG_LUONG_ITEM;