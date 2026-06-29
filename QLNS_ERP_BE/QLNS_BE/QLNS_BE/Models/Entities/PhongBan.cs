namespace QLNS_BE.Models.Entities
{
    public class PhongBan
    {
        public int Id { get; set; }
        public string MaPhongBan { get; set; } = null!;
        public string TenPhongBan { get; set; } = null!;
        public int? PhongBanChaId { get; set; }
        public bool TrangThai { get; set; }
        public string? GhiChu { get; set; }

        // Navigation
        public PhongBan? PhongBanCha { get; set; }
        public ICollection<PhongBan> PhongBanCon { get; set; } = new List<PhongBan>();
        public ICollection<NvCongViec> NhanVienCongViecs { get; set; } = new List<NvCongViec>();
    }
}
