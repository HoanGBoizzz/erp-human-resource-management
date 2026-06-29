namespace QLNS_BE.Models.Entities
{
    public class ChucNang
    {
        public int Id { get; set; }
        public string MaChucNang { get; set; } = null!;
        public string TenChucNang { get; set; } = null!;
        public string? DuongDan { get; set; }
        public string? Nhom { get; set; }
        public int ThuTuHienThi { get; set; }
        public bool TrangThai { get; set; }

        // Navigation
        public ICollection<VaiTroChucNang> VaiTroChucNangs { get; set; } = new List<VaiTroChucNang>();
    }
}
