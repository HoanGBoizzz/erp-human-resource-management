namespace QLNS_BE.Models.Dtos.NhanVien
{/// <summary>
 /// Thông tin chi tiết về một bản ghi NV_CONG_VIEC (công việc của nhân viên).
 /// </summary>
    public class NvCongViecDto
    {
        public int Id { get; set; }                          // NV_CONG_VIEC.id
        public int NvHoSoId { get; set; }                    // NV_CONG_VIEC.nv_ho_so_id
        public int PhongBanId { get; set; }                  // NV_CONG_VIEC.phong_ban_id
        public string? TenPhongBan { get; set; }             // PHONG_BAN.ten_phong_ban
        public int ChucVuId { get; set; }                    // NV_CONG_VIEC.chuc_vu_id
        public string? TenChucVu { get; set; }               // CHUC_VU.ten_chuc_vu
        public DateTime NgayVaoLam { get; set; }             // NV_CONG_VIEC.ngay_vao_lam
        public DateTime? NgayNghiViec { get; set; }          // NV_CONG_VIEC.ngay_nghi_viec
        public string? LoaiHopDong { get; set; }             // NV_CONG_VIEC.loai_hop_dong
        public DateTime? NgayKyHopDong { get; set; }         // NV_CONG_VIEC.ngay_ky_hop_dong
        public DateTime? NgayHetHanHopDong { get; set; }     // NV_CONG_VIEC.ngay_het_han_hop_dong
        public byte TrangThaiLamViec { get; set; }           // NV_CONG_VIEC.trang_thai_lam_viec
        public string? GhiChu { get; set; }                  // NV_CONG_VIEC.ghi_chu
    }
}
