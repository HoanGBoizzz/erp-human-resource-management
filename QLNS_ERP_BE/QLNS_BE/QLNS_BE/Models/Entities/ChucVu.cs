namespace QLNS_BE.Models.Entities
{
    public class ChucVu
    {
        public int Id { get; set; }
        public string MaChucVu { get; set; } = null!;
        public string TenChucVu { get; set; } = null!;
        public decimal HeSoPhuCap { get; set; }
        public string? MoTa { get; set; }
        public bool TrangThai { get; set; }

        // Navigation
        public ICollection<NvCongViec> NhanVienCongViecs { get; set; } = new List<NvCongViec>();
    }

}
