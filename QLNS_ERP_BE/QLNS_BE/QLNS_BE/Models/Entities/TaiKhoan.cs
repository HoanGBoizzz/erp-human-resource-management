namespace QLNS_BE.Models.Entities
{
    public class TaiKhoan
    {
        public int Id { get; set; }
        public string TenDangNhap { get; set; } = null!;
        public string MatKhauHash { get; set; } = null!;
        public string? MatKhauSalt { get; set; }
        public int? NvHoSoId { get; set; }
        public int VaiTroId { get; set; }
        public bool TrangThai { get; set; }
        public DateTime? LanDangNhapCuoi { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Login Fail Tracking
        public int SoLanDangNhapSai { get; set; } = 0;
        public DateTime? ThoiGianKhoa { get; set; }
        
        // Account Warning Management
        public string TrangThaiCanhBao { get; set; } = "BINH_THUONG";  // BINH_THUONG, CANH_BAO, CAM
        public string? LyDoCanhBao { get; set; }
        public int? TaiKhoanCanhBaoBoiId { get; set; }  // Changed: Added "Id" suffix
        public DateTime? NgayCanhBao { get; set; }

        /// <summary>
        /// Mật khẩu tạm thời (plaintext) - chỉ lưu khi tạo mới
        /// Được xóa khi user đăng nhập lần đầu
        /// </summary>
        public string? MatKhauTam { get; set; }

        // Navigation
        public NvHoSo? NvHoSo { get; set; }
        public VaiTro VaiTro { get; set; } = null!;
        public ICollection<VaiTroChucNang> VaiTroChucNangs { get; set; } = new List<VaiTroChucNang>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<NvLuongDeXuat> LuongDeXuatsTao { get; set; } = new List<NvLuongDeXuat>();
        public ICollection<NvLuongDeXuat> LuongDeXuatsDuyet { get; set; } = new List<NvLuongDeXuat>();
        public ICollection<BangLuongThang> BangLuongDaTinh { get; set; } = new List<BangLuongThang>();
        public ICollection<BangLuongThang> BangLuongGuiDuyet { get; set; } = new List<BangLuongThang>();
        public ICollection<BangLuongThang> BangLuongDaDuyet { get; set; } = new List<BangLuongThang>();
        public ICollection<BangLuongThang> BangLuongDaKhoa { get; set; } = new List<BangLuongThang>();
        public ICollection<BangCongThang> BangCongDaChot { get; set; } = new List<BangCongThang>();
        public ICollection<DuAn> DuAnTao { get; set; } = new List<DuAn>();
        public ICollection<DuAn> DuAnDuyet { get; set; } = new List<DuAn>();
        public ICollection<DuAnNhatKyTrangThai> DuAnNhatKyTrangThais { get; set; } = new List<DuAnNhatKyTrangThai>();
        
        // Navigation for Account Warning
        public TaiKhoan? TaiKhoanCanhBaoBoi_Nav { get; set; }
        public ICollection<TaiKhoan> TaiKhoanDaCanhBao { get; set; } = new List<TaiKhoan>();

    }
}
