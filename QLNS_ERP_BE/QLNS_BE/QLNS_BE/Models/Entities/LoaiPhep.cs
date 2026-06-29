namespace QLNS_BE.Models.Entities
{
    public class LoaiPhep
    {
        public int Id { get; set; }
        public string MaLoaiPhep { get; set; } = null!;
        public string TenLoaiPhep { get; set; } = null!;
        public decimal SoNgayMacDinh { get; set; }
        public bool TinhLuong { get; set; }
        public bool TrangThai { get; set; }

        // Navigation
        public ICollection<DonXinPhep> DonXinPheps { get; set; } = new List<DonXinPhep>();
    }
}
