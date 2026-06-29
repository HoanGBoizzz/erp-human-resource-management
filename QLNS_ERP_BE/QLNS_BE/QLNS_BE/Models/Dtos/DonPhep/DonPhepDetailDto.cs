namespace QLNS_BE.Models.Dtos.DonPhep
{
    public class DonPhepDetailDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public int? TaiKhoanId { get; set; } // ID tài khoản đã tạo đơn (cho notification)
        public int LoaiPhepId { get; set; }
        public string TenLoaiPhep { get; set; } = null!;
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal SoNgay { get; set; }
        public string? LyDo { get; set; }
        public string TrangThai { get; set; } = null!;
        public int? NguoiDuyetId { get; set; }
        public string? TenNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
