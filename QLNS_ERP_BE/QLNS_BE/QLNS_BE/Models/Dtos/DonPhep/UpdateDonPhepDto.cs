namespace QLNS_BE.Models.Dtos.DonPhep
{
    public class UpdateDonPhepDto
    {
        public int LoaiPhepId { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public string LyDo { get; set; } = string.Empty;
    }
}
