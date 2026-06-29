namespace QLNS_BE.Models.Dtos.Luong
{
    public class PhuCapLoaiDto
    {
        public int Id { get; set; }
        public string TenPhuCap { get; set; } = null!;
        public string? MoTa { get; set; }
        public bool LaCoDinh { get; set; }
        public string DonVi { get; set; } = "VND";
        public int ThuTu { get; set; }
        public bool DangHoatDong { get; set; }
    }

    public class PhuCapLoaiCreateDto
    {
        public string TenPhuCap { get; set; } = null!;
        public string? MoTa { get; set; }
        public bool LaCoDinh { get; set; } = true;
        public string DonVi { get; set; } = "VND";
        public int ThuTu { get; set; } = 0;
    }

    // ─── NvPhuCap ─────────────────────────────────────

    public class NvPhuCapDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public int PhuCapLoaiId { get; set; }
        public string TenPhuCap { get; set; } = null!;
        public decimal SoTien { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public bool DangApDung { get; set; }
        public string? GhiChu { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class NvPhuCapCreateDto
    {
        public int NvHoSoId { get; set; }
        public int PhuCapLoaiId { get; set; }
        public decimal SoTien { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string? GhiChu { get; set; }
    }

    public class NvPhuCapUpdateDto
    {
        public decimal SoTien { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public bool DangApDung { get; set; }
        public string? GhiChu { get; set; }
    }

    // ─── BangLuongItem ─────────────────────────────────

    public class BangLuongItemDto
    {
        public int Id { get; set; }
        public int BangLuongThangId { get; set; }
        public string Loai { get; set; } = null!;  // "THUONG" | "KHAU_TRU"
        public string LyDo { get; set; } = null!;
        public decimal SoTien { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class BangLuongItemCreateDto
    {
        public string Loai { get; set; } = null!;
        public string LyDo { get; set; } = null!;
        public decimal SoTien { get; set; }
    }

    // ─── LuongCoBan (NvLuongHienTai) ────────────────────

    public class LuongCoBanDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public string MaNhanVien { get; set; } = null!;
        public string? TenPhongBan { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal PhuCapCoDinh { get; set; }
        public string? SoTaiKhoanNganHang { get; set; }
        public string? TenNganHang { get; set; }
        public string? ChiNhanhNganHang { get; set; }
        public DateTime NgayBatDauHieuLuc { get; set; }
        public DateTime? NgayKetThucHieuLuc { get; set; }
        public bool DangApDung { get; set; }
    }

    public class LuongCoBanUpdateDto
    {
        public decimal LuongCoBan { get; set; }
        public decimal PhuCapCoDinh { get; set; }
        public string? SoTaiKhoanNganHang { get; set; }
        public string? TenNganHang { get; set; }
        public string? ChiNhanhNganHang { get; set; }
        public DateTime NgayBatDauHieuLuc { get; set; }
    }
}
