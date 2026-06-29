// Models/Entities/ChamCongChiTiet.cs
namespace QLNS_BE.Models.Entities
{
    /// <summary>
    /// Bảng Chi tiết chấm công
    /// </summary>
    public class ChamCongChiTiet
    {
        public int Id { get; set; }
        public int BangCongThangId { get; set; }
        public int NvHoSoId { get; set; }
        public DateTime Ngay { get; set; }
        public TimeSpan? GioVao { get; set; }
        public TimeSpan? GioRa { get; set; }
        public decimal SoCong { get; set; } = 0;
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }

        // Navigation properties
        public virtual BangCongThang BangCongThang { get; set; } = null!;
        public virtual NvHoSo NhanVien { get; set; } = null!;
    }
}