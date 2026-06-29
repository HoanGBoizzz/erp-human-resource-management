namespace QLNS_BE.Models.Dtos.FaceRecognition
{
    public class FaceRecognitionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? NvHoSoId { get; set; }
        public string? TenNhanVien { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public DateTime? ThoiGianChamCong { get; set; }
        public string? LoaiChamCong { get; set; } // VAO, RA, VAO_OT, RA_OT
        public int? ChamCongId { get; set; }
        public int? LogId { get; set; }
        public bool RequireOtConfirmation { get; set; } // true = FE hiển thị dialog "Vào tăng ca?"
    }
}
