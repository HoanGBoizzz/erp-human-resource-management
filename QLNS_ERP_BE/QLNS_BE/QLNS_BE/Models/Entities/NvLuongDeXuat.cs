namespace QLNS_BE.Models.Entities
{
    public class NvLuongDeXuat
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string LoaiDeXuat { get; set; } = null!;      // LUONG_CO_BAN, TAI_KHOAN_NH,...
        public string TruongDuLieu { get; set; } = null!;    // tên field
        public string? GiaTriCu { get; set; }
        public string? GiaTriMoi { get; set; }
        public DateTime NgayHieuLucDeXuat { get; set; }
        public string TrangThai { get; set; } = null!;       // CHO_DUYET_LUONG_CB, DUOC_DUYET, TU_CHOI
        public int TaiKhoanTaoId { get; set; }
        public int? TaiKhoanDuyetId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? LyDo { get; set; }

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
        public TaiKhoan TaiKhoanTao { get; set; } = null!;
        public TaiKhoan? TaiKhoanDuyet { get; set; }
    }
}
