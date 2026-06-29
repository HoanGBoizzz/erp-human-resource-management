namespace QLNS_BE.Models.Dtos.Luong
{
    public class BangLuongThangDetailDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public string? TenPhongBan { get; set; }
        public string? MaNhanVien { get; set; }

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

        public DateTime? NgayTinhLuong { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyetGiamDoc { get; set; }
        public DateTime? NgayKhoaLuong { get; set; }

        public string? NguoiTinh { get; set; }
        public string? NguoiGuiDuyet { get; set; }
        public int? TaiKhoanGuiDuyetId { get; set; } // ID tài khoản đã gửi duyệt (cho notification)
        public string? NguoiDuyet { get; set; }
        public string? NguoiKhoa { get; set; }
    }
}
