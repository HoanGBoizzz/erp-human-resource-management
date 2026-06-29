namespace QLNS_BE.Models.Dtos.NhanVien
{
    /// <summary>
    /// Dùng cho màn danh sách nhân viên (grid).
    /// </summary>
    public class NhanVienListItemDto
    {
        public int Id { get; set; }                      // NV_HO_SO.id
        public string MaNhanVien { get; set; } = null!;  // NV_HO_SO.ma_nhan_vien
        public string HoTen { get; set; } = null!;       // NV_HO_SO.ho_ten

        public int? PhongBanId { get; set; }             // NV_CONG_VIEC.phong_ban_id
        public string? TenPhongBan { get; set; }         // PHONG_BAN.ten_phong_ban

        public int? ChucVuId { get; set; }               // NV_CONG_VIEC.chuc_vu_id
        public string? TenChucVu { get; set; }           // CHUC_VU.ten_chuc_vu

        public byte TrangThaiLamViec { get; set; }       // NV_CONG_VIEC.trang_thai_lam_viec (1: đang làm, 0: nghỉ)
        public DateTime? NgayVaoLam { get; set; }        // NV_CONG_VIEC.ngay_vao_lam
        public DateTime? NgayNghiViec { get; set; }      // NV_CONG_VIEC.ngay_nghi_viec
    }
}
