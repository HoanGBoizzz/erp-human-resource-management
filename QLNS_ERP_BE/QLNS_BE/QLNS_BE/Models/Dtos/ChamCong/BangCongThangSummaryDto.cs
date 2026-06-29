namespace QLNS_BE.Models.Dtos.ChamCong
{
    public class BangCongThangSummaryDto
    {
        public int Id { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }

        public string TrangThaiCong { get; set; } = null!;
        public DateTime? NgayChotCong { get; set; }
    }
}
