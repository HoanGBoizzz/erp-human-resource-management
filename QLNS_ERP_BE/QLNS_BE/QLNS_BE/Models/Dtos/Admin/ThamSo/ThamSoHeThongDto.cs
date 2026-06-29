namespace QLNS_BE.Models.Dtos.Admin.ThamSo
{
    public class ThamSoHeThongDto
    {
        public int Id { get; set; }
        public string MaThamSo { get; set; } = null!;
        public string GiaTri { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayBatDauHieuLuc { get; set; }
        public DateTime? NgayKetThucHieuLuc { get; set; }
    }
}
