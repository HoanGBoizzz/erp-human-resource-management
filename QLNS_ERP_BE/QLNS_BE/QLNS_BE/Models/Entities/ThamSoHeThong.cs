namespace QLNS_BE.Models.Entities
{
    public class ThamSoHeThong
    {
        public int Id { get; set; }
        public string MaThamSo { get; set; } = null!;
        public string GiaTri { get; set; } = null!;
        public DateTime NgayBatDauHieuLuc { get; set; }
        public DateTime? NgayKetThucHieuLuc { get; set; }
        public string? MoTa { get; set; }
    }
}
