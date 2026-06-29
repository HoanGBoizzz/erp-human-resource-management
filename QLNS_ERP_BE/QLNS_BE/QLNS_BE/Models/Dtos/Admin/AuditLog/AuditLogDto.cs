namespace QLNS_BE.Models.Dtos.Admin.AuditLog
{
    /// <summary>
    /// DTO cho danh sách và chi tiết audit log
    /// </summary>
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int TaiKhoanId { get; set; }
        public string TenDangNhap { get; set; } = null!;  // Join từ TaiKhoan
        public DateTime ThoiGian { get; set; }
        public string Bang { get; set; } = null!;
        public int? DoiTuongId { get; set; }
        public string? Truong { get; set; }
        public string? GiaTriCu { get; set; }
        public string? GiaTriMoi { get; set; }
        public string HanhDong { get; set; } = null!;  // INSERT, UPDATE, DELETE, ACTION
        public string? GhiChu { get; set; }
        public string? TenDoiTuong { get; set; }
    }
}
