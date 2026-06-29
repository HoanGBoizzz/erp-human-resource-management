namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class ChamCongChiTietDto
    {
        public int Id { get; set; }
        public int BangCongThangId { get; set; }
        public int NvHoSoId { get; set; }
        public string MaNhanVien { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public DateTime Ngay { get; set; }
        public string? GioVao { get; set; }             // HH:mm hoặc ISO
        public string? GioRa { get; set; }              // HH:mm hoặc ISO
        public string TrangThai { get; set; } = string.Empty;  // "Đi làm", "Nghỉ", "Trễ", etc.
        public string? GhiChu { get; set; }
    }
}
