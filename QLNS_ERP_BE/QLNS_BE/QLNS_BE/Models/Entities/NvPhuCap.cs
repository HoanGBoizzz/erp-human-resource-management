namespace QLNS_BE.Models.Entities
{
    /// <summary>Phụ cấp của từng nhân viên (itemized per-employee allowances)</summary>
    public class NvPhuCap
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public int PhuCapLoaiId { get; set; }
        public decimal SoTien { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public bool DangApDung { get; set; } = true;
        public string? GhiChu { get; set; }
        public int? TaiKhoanTaoId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
        public PhuCapLoai PhuCapLoai { get; set; } = null!;
    }
}
