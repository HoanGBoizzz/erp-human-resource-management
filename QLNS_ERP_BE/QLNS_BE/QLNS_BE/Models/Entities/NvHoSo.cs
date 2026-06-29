namespace QLNS_BE.Models.Entities
{
    public class NvHoSo
    {
        public int Id { get; set; }
        public string MaNhanVien { get; set; } = null!;
        public string HoTen { get; set; } = null!;
        public DateTime? NgaySinh { get; set; }
        public byte? GioiTinh { get; set; }        // 0: nữ, 1: nam, 2: khác
        public string? DiaChi { get; set; }
        public string? SoDienThoai { get; set; }
        public string? EmailCaNhan { get; set; }
        public int TrangThaiLamViec { get; set; } = 1;  // 1: Đang làm, 2: Nghỉ việc
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? AnhCaNhanUrl { get; set; }
        public string? SoTaiKhoanNganHang { get; set; }
        public string? AnhStkUrl { get; set; }
        public string? HopDongUrl { get; set; }   // File hợp đồng lao động

        // Navigation
        public ICollection<NvCongViec> CongViecs { get; set; } = new List<NvCongViec>();
        public ICollection<NvLuongHienTai> LuongHienTais { get; set; } = new List<NvLuongHienTai>();
        public ICollection<NvLuongDeXuat> LuongDeXuats { get; set; } = new List<NvLuongDeXuat>();
        public ICollection<DonXinPhep> DonXinPheps { get; set; } = new List<DonXinPhep>();
        public ICollection<ChamCong> ChamCongs { get; set; } = new List<ChamCong>();
        public ICollection<BangLuongThang> BangLuongThangs { get; set; } = new List<BangLuongThang>();
        public ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
        public ICollection<DuAn> DuAnPhuTrachs { get; set; } = new List<DuAn>();
        public ICollection<DuAnThanhVien> DuAnThanhViens { get; set; } = new List<DuAnThanhVien>();
        public virtual ICollection<ChamCongChiTiet> ChamCongChiTiets { get; set; } = new List<ChamCongChiTiet>();
        
        // Navigation for Tasks
        public ICollection<DuAnTask> TasksNhan { get; set; } = new List<DuAnTask>();
        public ICollection<DuAnTask> TasksGiao { get; set; } = new List<DuAnTask>();

    }
}
