namespace QLNS_BE.Models.Dtos.NhanVien
{/// <summary>
 /// Dùng khi HR tạo mới nhân viên (hồ sơ + một dòng công việc).
 /// </summary>
    public class NhanVienCreateDto
    {
        // NV_HO_SO
        public string MaNhanVien { get; set; } = null!;      // NV_HO_SO.ma_nhan_vien
        public string HoTen { get; set; } = null!;           // NV_HO_SO.ho_ten
        public DateTime? NgaySinh { get; set; }              // NV_HO_SO.ngay_sinh
        public byte? GioiTinh { get; set; }                  // NV_HO_SO.gioi_tinh
        public string? DiaChi { get; set; }                  // NV_HO_SO.dia_chi
        public string? SoDienThoai { get; set; }             // NV_HO_SO.so_dien_thoai
        public string? EmailCaNhan { get; set; }             // NV_HO_SO.email_ca_nhan

        // NV_CONG_VIEC
        public int PhongBanId { get; set; }                  // NV_CONG_VIEC.phong_ban_id
        public int ChucVuId { get; set; }                    // NV_CONG_VIEC.chuc_vu_id
        public DateTime NgayVaoLam { get; set; }             // NV_CONG_VIEC.ngay_vao_lam
        public string? LoaiHopDong { get; set; }             // NV_CONG_VIEC.loai_hop_dong
        public DateTime? NgayKyHopDong { get; set; }         // NV_CONG_VIEC.ngay_ky_hop_dong
        public DateTime? NgayHetHanHopDong { get; set; }     // NV_CONG_VIEC.ngay_het_han_hop_dong
        public string? GhiChu { get; set; }                  // NV_CONG_VIEC.ghi_chu
    }
}
