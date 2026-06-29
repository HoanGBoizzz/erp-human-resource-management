namespace QLNS_BE.Models.Dtos.NoiLamViec
{
    // ─── Create ───────────────────────────────────────────
    public class CreatePhieuTamUngDto
    {
        public string MucDich { get; set; } = null!;
        public decimal SoTien { get; set; }
        public DateTime NgayCanTamUng { get; set; }
        public string LyDo { get; set; } = null!;
    }

    // ─── List item ────────────────────────────────────────
    public class PhieuTamUngListItemDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string MaNhanVien { get; set; } = null!;
        public string HoTenNhanVien { get; set; } = null!; public string MucDich { get; set; } = null!;
        public decimal SoTien { get; set; }
        public DateTime NgayCanTamUng { get; set; }
        public string LyDo { get; set; } = null!;
        public string TrangThai { get; set; } = null!;
        public string? LyDoTuChoi { get; set; }
        public string? HoTenNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Update ───────────────────────────────────────────
    public class UpdatePhieuTamUngDto
    {
        public string MucDich { get; set; } = null!;
        public decimal SoTien { get; set; }
        public DateTime NgayCanTamUng { get; set; }
        public string LyDo { get; set; } = null!;
    }

    // ─── Approve/Reject request ───────────────────────────
    public class DuyetPhieuTamUngDto
    {
        public int PhieuId { get; set; }
        public bool ChapNhan { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
