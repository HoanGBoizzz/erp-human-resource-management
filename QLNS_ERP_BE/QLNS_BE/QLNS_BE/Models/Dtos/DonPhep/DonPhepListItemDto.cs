namespace QLNS_BE.Models.Dtos.DonPhep
{
    public class DonPhepListItemDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public string TenLoaiPhep { get; set; } = null!;
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal SoNgay { get; set; }
        public string TrangThai { get; set; } = null!;
    }
}
