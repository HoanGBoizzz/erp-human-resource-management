namespace QLNS_BE.Models.Dtos.FaceRecognition
{
    public class FaceDataDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string? TenNhanVien { get; set; }
        public string? MaNhanVien { get; set; }
        public int SoLuongAnh { get; set; }
        public string? FaceImageUrl { get; set; }
        public decimal? QualityScore { get; set; }
        public decimal? ChatLuongTrungBinh { get; set; }
        public DateTime NgayDangKy { get; set; }
        public bool IsActive { get; set; }
    }

    public class FaceLogDto
    {
        public int Id { get; set; }
        public int? NvHoSoId { get; set; }
        public string? TenNhanVien { get; set; }
        public DateTime ThoiGian { get; set; }
        public string Loai { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public decimal? ConfidenceScore { get; set; }
        public string? FaceImageUrl { get; set; }
        public string? LyDoThatBai { get; set; }
        public string? IpAddress { get; set; }
        public int? ProcessingTimeMs { get; set; }
    }
}
