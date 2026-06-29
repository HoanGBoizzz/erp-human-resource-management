namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnMyApprovedListDto
    {
        public int Id { get; set; }
        public string MaDuAn { get; set; } = null!;
        public string TenDuAn { get; set; } = null!;
        public DateTime NgayDuyet { get; set; }
        public string TrangThaiDuAn { get; set; } = null!;
    }
}
