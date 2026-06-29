namespace QLNS_BE.Models.Entities
{
    /// <summary>
    /// Đề xuất gửi giám đốc duyệt (do HR tạo)
    /// TrangThai: NHAP | CHO_DUYET | DA_DUYET | TU_CHOI | DA_THU_HOI
    /// </summary>
    public class DeXuatGiamDoc
    {
        public int Id { get; set; }
        public string TenDeXuat { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayDeXuat { get; set; }

        // File đính kèm
        public string? TepTinUrl { get; set; }
        public string? TepTinTenGoc { get; set; }
        public string? TepTinMime { get; set; }
        public long? TepTinSize { get; set; }

        // Workflow
        public string TrangThai { get; set; } = "NHAP";

        public int TaiKhoanTaoId { get; set; }
        public int? TaiKhoanDuyetId { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public TaiKhoan TaiKhoanTao { get; set; } = null!;
        public TaiKhoan? TaiKhoanDuyet { get; set; }
    }
}
