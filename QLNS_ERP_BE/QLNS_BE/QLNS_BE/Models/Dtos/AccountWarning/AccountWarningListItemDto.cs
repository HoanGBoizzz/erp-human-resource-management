namespace QLNS_BE.Models.Dtos.AccountWarning
{
    public class AccountWarningListItemDto
    {
        public int Id { get; set; }
        public string TenDangNhap { get; set; } = null!;
        public string? HoTen { get; set; }
        public string TrangThaiCanhBao { get; set; } = null!;
        public string? LyDoCanhBao { get; set; }
        public string? NguoiCanhBao { get; set; }
        public DateTime? NgayCanhBao { get; set; }
        public int SoLanDangNhapSai { get; set; }
        public DateTime? ThoiGianKhoa { get; set; }
    }
}
