using System.Text.Json.Serialization;

namespace QLNS_BE.Models.Dtos.Luong
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TrangThaiLuong
    {
        KHAC = 0,
        TAM_TINH,
        CHO_DUYET_GIAM_DOC,
        DA_DUYET,
        TU_CHOI,
        DA_TINH,
        DA_KHOA
    }

    public static class TrangThaiLuongMapper
    {
        public static TrangThaiLuong FromDb(string? trangThai)
        {
            if (string.IsNullOrWhiteSpace(trangThai)) return TrangThaiLuong.KHAC;

            // DB đang lưu dạng varchar như "TAM_TINH", "CHO_DUYET_GIAM_DOC"... (LuongService đang dùng)
            if (Enum.TryParse<TrangThaiLuong>(trangThai.Trim(), ignoreCase: true, out var rs))
                return rs;

            return TrangThaiLuong.KHAC;
        }
    }

    public class BangLuongThangListItemDto
    {
        // Đã có
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = "";
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal TongLuong { get; set; }

        // BỔ SUNG
        public decimal TongCong { get; set; }
        public decimal TongOt { get; set; }
        public decimal LuongCoBanTinh { get; set; }
        public decimal PhuCapTinh { get; set; }
        public decimal Thuong { get; set; }
        public decimal KhauTru { get; set; }
        public TrangThaiLuong TrangThai { get; set; }
        public string? TenPhongBan { get; set; }  // lấy từ NvCongViec hiện tại
        public string? MaNhanVien { get; set; }

        public DateTime? NgayTinhLuong { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyetGiamDoc { get; set; }
        public DateTime? NgayKhoaLuong { get; set; }

        // ── Cờ công thức ──────────────────────────────────────
        public bool CoTinhPhuCap { get; set; } = true;
        public bool CoTinhOT { get; set; } = true;
        public bool CoTinhThuong { get; set; } = true;
        public bool CoTinhKhauTru { get; set; } = true;

        // ── Chi tiết breakdown (cho print slip) ──────────────
        public decimal KhauTruDiMuon { get; set; }
        public decimal KhauTruThuongPhat { get; set; }
    }
}
