namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class BangCongThangDetailDto
    {
        public int Id { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }

        public string TrangThaiCong { get; set; } = null!;
        public DateTime? NgayChotCong { get; set; }
        public int? TaiKhoanChotId { get; set; }
        public string? TenNguoiChot { get; set; }

        public List<ChamCongNgayDto> NgayCongs { get; set; } = new();
    }
}
