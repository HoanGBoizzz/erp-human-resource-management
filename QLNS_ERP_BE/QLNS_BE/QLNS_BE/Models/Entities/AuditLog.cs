namespace QLNS_BE.Models.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int TaiKhoanId { get; set; }
        public DateTime ThoiGian { get; set; }
        public string Bang { get; set; } = null!;
        public int? DoiTuongId { get; set; }
        public string? Truong { get; set; }
        public string? GiaTriCu { get; set; }
        public string? GiaTriMoi { get; set; }
        public string HanhDong { get; set; } = null!;   // INSERT, UPDATE, DELETE, ACTION
        public string? GhiChu { get; set; }
        public string? TenDoiTuong { get; set; }

        // Navigation
        public TaiKhoan TaiKhoan { get; set; } = null!;
    }
}
