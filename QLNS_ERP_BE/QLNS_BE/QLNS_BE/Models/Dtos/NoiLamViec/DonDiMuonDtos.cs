namespace QLNS_BE.Models.Dtos.NoiLamViec
{
    // ─── Create ───────────────────────────────────────────
    public class CreateDonDiMuonDto
    {
        /// <summary>DI_MUON | VE_SOM | CA_HAI</summary>
        public string Loai { get; set; } = null!;
        public DateTime NgayApDung { get; set; }
        public string ThoiGianBatDau { get; set; } = null!;  // "HH:mm"
        public string ThoiGianKetThuc { get; set; } = null!; // "HH:mm"
        public string LyDo { get; set; } = null!;
    }

    // ─── Update ───────────────────────────────────────────
    public class UpdateDonDiMuonDto
    {
        public string Loai { get; set; } = null!;
        public DateTime NgayApDung { get; set; }
        public string ThoiGianBatDau { get; set; } = null!;
        public string ThoiGianKetThuc { get; set; } = null!;
        public string LyDo { get; set; } = null!;
    }

    // ─── List item ───────────────────────────────────────────
    public class DonDiMuonListItemDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string MaNhanVien { get; set; } = null!;
        public string HoTenNhanVien { get; set; } = null!;
        public string Loai { get; set; } = null!;
        public string TenLoai { get; set; } = null!;
        public DateTime NgayApDung { get; set; }
        public string ThoiGianBatDau { get; set; } = null!;
        public string ThoiGianKetThuc { get; set; } = null!;
        public string LyDo { get; set; } = null!;
        public string TrangThai { get; set; } = null!;
        public string? LyDoTuChoi { get; set; }
        public string? HoTenNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Approve/Reject request ───────────────────────────
    public class DuyetDonDiMuonDto
    {
        public int DonId { get; set; }
        public bool ChapNhan { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
