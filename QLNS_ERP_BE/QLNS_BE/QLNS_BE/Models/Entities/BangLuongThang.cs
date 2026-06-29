namespace QLNS_BE.Models.Entities
{
    public class BangLuongThang
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public int BangCongThangId { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal TongCong { get; set; }
        public decimal TongOt { get; set; }
        public decimal LuongCoBanTinh { get; set; }
        public decimal PhuCapTinh { get; set; }
        public decimal Thuong { get; set; }
        public decimal KhauTru { get; set; }
        public decimal TongLuong { get; set; }
        public string TrangThai { get; set; } = null!;   // CAN_TINH_LAI, TAM_TINH,...
        public bool IsDirty { get; set; }
        public DateTime? NgayTinhLuong { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyetGiamDoc { get; set; }
        public DateTime? NgayKhoaLuong { get; set; }
        public int? TaiKhoanTinhId { get; set; }
        public int? TaiKhoanGuiDuyetId { get; set; }
        public int? TaiKhoanDuyetId { get; set; }
        public int? TaiKhoanKhoaId { get; set; }

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
        public BangCongThang BangCongThang { get; set; } = null!;
        public TaiKhoan? TaiKhoanTinh { get; set; }
        public TaiKhoan? TaiKhoanGuiDuyet { get; set; }
        public TaiKhoan? TaiKhoanDuyet { get; set; }
        public TaiKhoan? TaiKhoanKhoa { get; set; }
        public ICollection<BangLuongItem> BangLuongItems { get; set; } = new List<BangLuongItem>();
    }
}
