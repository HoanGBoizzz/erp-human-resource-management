namespace QLNS_BE.Models.Entities
{
    public class PhieuTamUng
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }

        public string MucDich { get; set; } = null!;
        public decimal SoTien { get; set; }
        public DateTime NgayCanTamUng { get; set; }
        public string LyDo { get; set; } = null!;

        public string TrangThai { get; set; } = "CHO_DUYET"; // CHO_DUYET | DA_DUYET | TU_CHOI
        public int? NguoiDuyetId { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public NvHoSo NvHoSo { get; set; } = null!;
        public TaiKhoan? NguoiDuyet { get; set; }
    }
}
