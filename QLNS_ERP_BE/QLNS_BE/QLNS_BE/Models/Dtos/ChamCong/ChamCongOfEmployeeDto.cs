namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class ChamCongOfEmployeeDto
    {
        public int ChamCongId { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;

        public DateTime Ngay { get; set; }
        public DateTime? GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        public DateTime? GioVaoOt { get; set; }
        public DateTime? GioRaOt { get; set; }

        public decimal SoGioOt { get; set; }
        public string TrangThai { get; set; } = null!;
        public string? GhiChu { get; set; }
        public bool IsLockedByModule { get; set; }
    }
}
