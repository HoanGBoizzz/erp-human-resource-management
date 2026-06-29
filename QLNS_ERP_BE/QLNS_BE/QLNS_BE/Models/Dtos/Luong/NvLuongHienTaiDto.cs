namespace QLNS_BE.Models.Dtos.Luong
{
    public class NvLuongHienTaiDto
    {
        public int NvHoSoId { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal PhuCapCoDinh { get; set; }

        public string? SoTaiKhoanNganHang { get; set; }
        public string? TenNganHang { get; set; }
        public string? ChiNhanhNganHang { get; set; }

        public DateTime NgayBatDauHieuLuc { get; set; }
    }
}
