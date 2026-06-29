namespace QLNS_BE.Models.Dtos.NhanVien
{/// <summary>
 /// EMPLOYEE dùng để xem trang "Hồ sơ của tôi" (từ token suy ra NV_HO_SO).
 /// </summary>
    public class HoSoCaNhanDto
    {
        // Thông tin tài khoản (TAI_KHOAN + VAI_TRO)
        public string TenDangNhap { get; set; } = null!;     // TAI_KHOAN.ten_dang_nhap
        public int VaiTroId { get; set; }                    // TAI_KHOAN.vai_tro_id
        public string MaVaiTro { get; set; } = null!;        // VAI_TRO.ma_vai_tro
        public string TenVaiTro { get; set; } = null!;       // VAI_TRO.ten_vai_tro

        // NV_HO_SO
        public int NvHoSoId { get; set; }                    // NV_HO_SO.id
        public string MaNhanVien { get; set; } = null!;      // NV_HO_SO.ma_nhan_vien
        public string HoTen { get; set; } = null!;           // NV_HO_SO.ho_ten
        public DateTime? NgaySinh { get; set; }              // NV_HO_SO.ngay_sinh
        public byte? GioiTinh { get; set; }                  // NV_HO_SO.gioi_tinh
        public string? DiaChi { get; set; }                  // NV_HO_SO.dia_chi
        public string? SoDienThoai { get; set; }             // NV_HO_SO.so_dien_thoai
        public string? EmailCaNhan { get; set; }             // NV_HO_SO.email_ca_nhan
                                                             // ADD
        public string? AnhCaNhanUrl { get; set; }
        // ADD
        public string? SoTaiKhoanNganHang { get; set; }
        // ADD - lấy từ NvLuongHienTai đang áp dụng
        public string? TenNganHang { get; set; }
        public string? ChiNhanhNganHang { get; set; }
        // ADD
        public string? AnhStkUrl { get; set; }
        // ADD
        public string? HopDongUrl { get; set; }


        // Công việc hiện tại (NV_CONG_VIEC + PHONG_BAN + CHUC_VU)
        public NvCongViecDto? CongViecHienTai { get; set; }
    }
}
