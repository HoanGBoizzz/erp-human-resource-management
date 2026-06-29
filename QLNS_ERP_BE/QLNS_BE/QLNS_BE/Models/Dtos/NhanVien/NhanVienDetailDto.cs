namespace QLNS_BE.Models.Dtos.NhanVien
{/// <summary>
 /// Thông tin chi tiết nhân viên (hồ sơ + công việc hiện tại).
 /// </summary>
    public class NhanVienDetailDto
    {
        // NV_HO_SO
        public int Id { get; set; }                          // NV_HO_SO.id
        public string MaNhanVien { get; set; } = null!;      // NV_HO_SO.ma_nhan_vien
        public string HoTen { get; set; } = null!;           // NV_HO_SO.ho_ten
        public DateTime? NgaySinh { get; set; }              // NV_HO_SO.ngay_sinh
        public byte? GioiTinh { get; set; }                  // NV_HO_SO.gioi_tinh (0: nữ,1:nam,2:khác)
        public string? DiaChi { get; set; }                  // NV_HO_SO.dia_chi
        public string? SoDienThoai { get; set; }             // NV_HO_SO.so_dien_thoai
        public string? EmailCaNhan { get; set; }             // NV_HO_SO.email_ca_nhan
        public string? SoTaiKhoanNganHang { get; set; }      // NV_HO_SO.so_tai_khoan_ngan_hang
        public string? AnhStkUrl { get; set; }               // NV_HO_SO.anh_stk_url
        // Thông tin ngân hàng đầy đủ (lấy từ NvLuongHienTai đang áp dụng)
        public string? TenNganHang { get; set; }
        public string? ChiNhanhNganHang { get; set; }
        public string? HopDongUrl { get; set; }              // NV_HO_SO.hop_dong_url

        // NV_CONG_VIEC hiện tại (mới nhất)
        public int? NvCongViecId { get; set; }               // NV_CONG_VIEC.id
        public int? PhongBanId { get; set; }                 // NV_CONG_VIEC.phong_ban_id
        public string? TenPhongBan { get; set; }             // PHONG_BAN.ten_phong_ban
        public int? ChucVuId { get; set; }                   // NV_CONG_VIEC.chuc_vu_id
        public string? TenChucVu { get; set; }               // CHUC_VU.ten_chuc_vu
        public DateTime? NgayVaoLam { get; set; }            // NV_CONG_VIEC.ngay_vao_lam
        public DateTime? NgayNghiViec { get; set; }          // NV_CONG_VIEC.ngay_nghi_viec
        public string? LoaiHopDong { get; set; }             // NV_CONG_VIEC.loai_hop_dong
        public DateTime? NgayKyHopDong { get; set; }         // NV_CONG_VIEC.ngay_ky_hop_dong
        public DateTime? NgayHetHanHopDong { get; set; }     // NV_CONG_VIEC.ngay_het_han_hop_dong
        public byte? TrangThaiLamViec { get; set; }          // NV_CONG_VIEC.trang_thai_lam_viec
        public string? GhiChu { get; set; }                  // NV_CONG_VIEC.ghi_chu
    }
}
