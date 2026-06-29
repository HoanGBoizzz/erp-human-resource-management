namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnUpdateDto
    {
        public string TenDuAn { get; set; } = null!;
        public string? MoTa { get; set; }
        public decimal? NganSach { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public int? NvPhuTrachId { get; set; }
    }
}
