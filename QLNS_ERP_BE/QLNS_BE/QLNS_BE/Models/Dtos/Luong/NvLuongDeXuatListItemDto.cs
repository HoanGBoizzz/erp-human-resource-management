namespace QLNS_BE.Models.Dtos.Luong
{
    public class NvLuongDeXuatListItemDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public string LoaiDeXuat { get; set; } = null!;
        public string TruongDuLieu { get; set; } = null!;
        public string? GiaTriCu { get; set; }
        public string? GiaTriMoi { get; set; }
        public string TrangThai { get; set; } = null!;
        public DateTime NgayHieuLucDeXuat { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
