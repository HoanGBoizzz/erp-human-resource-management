namespace QLNS_BE.Models.Dtos.FaceRecognition
{
    public class RegisterFaceDto
    {
        public int NvHoSoId { get; set; }
        public IFormFile Image { get; set; } = null!;
        public string? GhiChu { get; set; }
    }

    public class RegisterFaceResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? FaceId { get; set; }
        public decimal? QualityScore { get; set; }
        public int? ImageNumber { get; set; } // Số thứ tự ảnh (1 = gốc, 2-3 = phụ)
    }
}
