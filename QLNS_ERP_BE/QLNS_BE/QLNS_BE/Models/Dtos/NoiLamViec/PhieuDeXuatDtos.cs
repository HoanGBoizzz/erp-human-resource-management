namespace QLNS_BE.Models.Dtos.NoiLamViec
{
    // ─── Create ───────────────────────────────────────────
    public class CreatePhieuDeXuatDto
    {
        public string TenDungCu { get; set; } = null!;
        public string DonViTinh { get; set; } = null!;
        public int SoLuong { get; set; }
        public decimal GiaTien { get; set; }
        public string LyDo { get; set; } = null!;
    }

    // ─── Update ───────────────────────────────────────────
    public class UpdatePhieuDeXuatDto
    {
        public string TenDungCu { get; set; } = null!;
        public string DonViTinh { get; set; } = null!;
        public int SoLuong { get; set; }
        public decimal GiaTien { get; set; }
        public string LyDo { get; set; } = null!;
    }

    // ─── List item ───────────────────────────────────────────
    public class PhieuDeXuatListItemDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string MaNhanVien { get; set; } = null!;
        public string HoTenNhanVien { get; set; } = null!;
        public string TenDungCu { get; set; } = null!;
        public string DonViTinh { get; set; } = null!;
        public int SoLuong { get; set; }
        public decimal GiaTien { get; set; }
        public decimal TongTien { get; set; }
        public string LyDo { get; set; } = null!;
        public string TrangThai { get; set; } = null!;
        public string? LyDoTuChoi { get; set; }
        public string? HoTenNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Approve/Reject request ───────────────────────────
    public class DuyetPhieuDeXuatDto
    {
        public int PhieuId { get; set; }
        public bool ChapNhan { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
