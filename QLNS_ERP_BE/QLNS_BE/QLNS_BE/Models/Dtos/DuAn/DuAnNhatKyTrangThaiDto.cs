namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnNhatKyTrangThaiDto
    {
        public string? TrangThaiCu { get; set; }
        public string TrangThaiMoi { get; set; } = null!;
        public string? GhiChu { get; set; }
        public DateTime ThoiGian { get; set; }
        public string NguoiThucHien { get; set; } = null!;
    }
}
