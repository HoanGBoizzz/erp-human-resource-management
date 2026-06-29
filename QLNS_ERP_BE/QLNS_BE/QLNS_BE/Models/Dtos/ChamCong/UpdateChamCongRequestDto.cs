namespace QLNS_BE.Models.Dtos.ChamCong
{
    /// <summary>
    /// DTO for updating attendance record from frontend
    /// GioVao and GioRa are strings in "HH:mm" format from HTML time input
    /// </summary>
    public class UpdateChamCongRequestDto
    {
        public string? GioVao { get; set; }  // "08:30" format from time input
        public string? GioRa { get; set; }   // "17:30" format from time input
        public decimal SoGioOt { get; set; }
        public string TrangThai { get; set; } = null!;
        public string? GhiChu { get; set; }
    }
}
