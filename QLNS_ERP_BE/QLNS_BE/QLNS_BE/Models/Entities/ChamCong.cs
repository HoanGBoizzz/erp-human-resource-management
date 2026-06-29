namespace QLNS_BE.Models.Entities
{
    public class ChamCong
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public int BangCongThangId { get; set; }
        public DateTime Ngay { get; set; }
        public DateTime? GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        public DateTime? GioVaoOt { get; set; } // Giờ bắt đầu tăng ca
        public DateTime? GioRaOt { get; set; }  // Giờ kết thúc tăng ca
        public decimal SoGioOt { get; set; }
        public string TrangThai { get; set; } = null!;
        public string? SourceModule { get; set; }
        public bool IsLockedByModule { get; set; }
        public string? GhiChu { get; set; }

        // Face Recognition fields (2026)
        public string? PhuongThuc { get; set; } // MANUAL, FACE_RECOGNITION
        public int? CreatedBy { get; set; }
        public int? FaceLogVaoId { get; set; }
        public int? FaceLogRaId { get; set; }

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
        public BangCongThang BangCongThang { get; set; } = null!;
        public ChamCongFaceLog? FaceLogVao { get; set; }
        public ChamCongFaceLog? FaceLogRa { get; set; }
        public TaiKhoan? Creator { get; set; }
    }
}
