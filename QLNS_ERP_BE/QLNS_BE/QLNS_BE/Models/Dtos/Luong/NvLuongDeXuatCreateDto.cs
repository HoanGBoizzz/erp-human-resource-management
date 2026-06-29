namespace QLNS_BE.Models.Dtos.Luong
{
    public class NvLuongDeXuatCreateDto
    {
        public string LoaiDeXuat { get; set; } = null!;  // LUONG_CO_BAN, TAI_KHOAN_NH,...
        public string TruongDuLieu { get; set; } = null!;
        public string? GiaTriMoi { get; set; }
        public DateTime NgayHieuLucDeXuat { get; set; }
        public string? LyDo { get; set; }
    }
}
