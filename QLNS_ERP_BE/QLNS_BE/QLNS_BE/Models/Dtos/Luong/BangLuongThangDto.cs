namespace QLNS_BE.Models.Dtos.Luong
{
    /// <summary>Chi tiết một khoản phụ cấp hoặc khấu trừ</summary>
    public class ChiTietMucLuongItem
    {
        public string Ten { get; set; } = "";
        public decimal SoTien { get; set; }
    }

    public class BangLuongThangDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string? NvHoSo { get; set; }
        public string HoTen { get; set; } = null!;
        public int Thang { get; set; }
        public int Nam { get; set; }

        public decimal TongCong { get; set; }
        public decimal TongOt { get; set; }

        public decimal LuongCoBanTinh { get; set; }
        public decimal PhuCapTinh { get; set; }
        public decimal Thuong { get; set; }
        public decimal KhauTru { get; set; }

        public decimal TongLuong { get; set; }
        public string TrangThai { get; set; } = null!;

        // ── Cờ công thức (HRcó thể bật/tắt từng thành phần) ───────────
        public bool CoTinhPhuCap { get; set; } = true;
        public bool CoTinhOT { get; set; } = true;
        public bool CoTinhThuong { get; set; } = true;
        public bool CoTinhKhauTru { get; set; } = true;

        // ── Chi tiết breakdown ──────────────────────────────
        /// <summary>Từng khoản phụ cấp (tên + số tiền)</summary>
        public List<ChiTietMucLuongItem> ChiTietPhuCap { get; set; } = new();
        /// <summary>Khấu trừ từ phạt đi muộn</summary>
        public decimal KhauTruDiMuon { get; set; }
        /// <summary>Khấu trừ từ thưởng & phạt (BangLuongItems)</summary>
        public decimal KhauTruThuongPhat { get; set; }
        /// <summary>Số lần đi muộn trong tháng</summary>
        public int SoLanDiMuon { get; set; }
        /// <summary>Từng khoản khấu trừ từ hệ thống thưởng & phạt</summary>
        public List<ChiTietMucLuongItem> ChiTietKhauTruItems { get; set; } = new();
    }
}
