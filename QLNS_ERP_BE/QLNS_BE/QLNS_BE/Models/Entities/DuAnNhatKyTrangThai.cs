namespace QLNS_BE.Models.Entities
{
    public class DuAnNhatKyTrangThai
    {
        public int Id { get; set; }
        public int DuAnId { get; set; }
        public string? TrangThaiCu { get; set; }
        public string TrangThaiMoi { get; set; } = null!;
        public string? GhiChu { get; set; }
        public DateTime ThoiGian { get; set; }
        public int TaiKhoanThucHienId { get; set; }

        // Navigation
        public DuAn DuAn { get; set; } = null!;
        public TaiKhoan TaiKhoanThucHien { get; set; } = null!;
    }
}
