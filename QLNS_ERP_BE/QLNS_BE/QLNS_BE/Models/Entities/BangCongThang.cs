namespace QLNS_BE.Models.Entities
{
    public class BangCongThang
    {
        public int Id { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public string TrangThaiCong { get; set; } = null!;    // DANG_NHAP_LIEU, DA_CHOT_CONG
        public DateTime? NgayChotCong { get; set; }
        public int? TaiKhoanChotId { get; set; }

        // Navigation
        public TaiKhoan? TaiKhoanChot { get; set; }
        public ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();
        public ICollection<BangLuongThang> BangLuongThangs { get; set; } = new List<BangLuongThang>();
        public virtual ICollection<ChamCongChiTiet> ChiTietChamCong { get; set; } = new List<ChamCongChiTiet>();
    }
}
