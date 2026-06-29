namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnListItemDto
    {
        public int Id { get; set; }
        public string MaDuAn { get; set; } = null!;
        public string TenDuAn { get; set; } = null!;
        public string TrangThaiDuAn { get; set; } = null!;
        public string? TenNhanVienPhuTrach { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        // ADD
        public string? TepTinDinhKemUrl { get; set; }
        // ADD
        public string? TepTinDinhKemTenGoc { get; set; }

    }
}
