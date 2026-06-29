namespace QLNS_BE.Models.Entities
{
    public class NvLuongHienTai
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal PhuCapCoDinh { get; set; }
        public string? SoTaiKhoanNganHang { get; set; }
        public string? TenNganHang { get; set; }
        public string? ChiNhanhNganHang { get; set; }
        public DateTime NgayBatDauHieuLuc { get; set; }
        public DateTime? NgayKetThucHieuLuc { get; set; }
        public bool DangApDung { get; set; }

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
    }
}
