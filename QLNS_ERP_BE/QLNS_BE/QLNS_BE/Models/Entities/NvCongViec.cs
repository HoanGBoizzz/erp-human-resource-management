namespace QLNS_BE.Models.Entities
{
    public class NvCongViec
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public int PhongBanId { get; set; }
        public int ChucVuId { get; set; }
        public DateTime NgayVaoLam { get; set; }
        public DateTime? NgayNghiViec { get; set; }
        public string? LoaiHopDong { get; set; }
        public DateTime? NgayKyHopDong { get; set; }
        public DateTime? NgayHetHanHopDong { get; set; }
        public byte TrangThaiLamViec { get; set; }
        public string? GhiChu { get; set; }

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
        public PhongBan PhongBan { get; set; } = null!;
        public ChucVu ChucVu { get; set; } = null!;
    }
}
