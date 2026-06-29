namespace QLNS_BE.Models.Dtos.PhongBan
{
    /// <summary>
    /// DTO cho danh sách phòng ban (hiển thị trong bảng)
    /// </summary>
    public class PhongBanListDto
    {
        public int Id { get; set; }
        public string MaPhongBan { get; set; } = null!;
        public string TenPhongBan { get; set; } = null!;
        public int? PhongBanChaId { get; set; }
        public string? TenPhongBanCha { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public int SoNhanVienDangLam { get; set; }  // Chỉ đếm NV đang làm (TrangThaiLamViec = 1)
        public int TongNhanVien { get; set; }       // Đếm tất cả NV trong phòng ban
    }
}
