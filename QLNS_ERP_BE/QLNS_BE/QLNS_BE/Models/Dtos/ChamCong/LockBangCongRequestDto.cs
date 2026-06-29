namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class LockBangCongRequestDto
    {
        public int BangCongThangId { get; set; }
        public bool Lock { get; set; }     // true = khóa, false = mở khóa
        public string? GhiChu { get; set; }
    }
}
