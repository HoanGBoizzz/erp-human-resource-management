namespace QLNS_BE.Models.Entities
{
    public class DuAn
    {
        public int Id { get; set; }
        public string MaDuAn { get; set; } = null!;
        public string TenDuAn { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public decimal? NganSach { get; set; }
        


        // DANG_NHAP, CHO_DUYET_GIAM_DOC, DA_DUYET, TU_CHOI
        public string TrangThaiDuAn { get; set; } = null!;

        public int? NvPhuTrachId { get; set; }
        public int TaiKhoanTaoId { get; set; }
        public int? TaiKhoanDuyetId { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public NvHoSo? NvPhuTrach { get; set; }
        public TaiKhoan TaiKhoanTao { get; set; } = null!;
        public TaiKhoan? TaiKhoanDuyet { get; set; }

        public ICollection<DuAnThanhVien> ThanhViens { get; set; } = new List<DuAnThanhVien>();
        public ICollection<DuAnNhatKyTrangThai> NhatKyTrangThais { get; set; } = new List<DuAnNhatKyTrangThai>();
        public ICollection<DuAnTask> Tasks { get; set; } = new List<DuAnTask>();
        public ICollection<DuAnFile> Files { get; set; } = new List<DuAnFile>();
        
        // ADD - DEPRECATED: Use Files collection instead
        public string? TepTinDinhKemUrl { get; set; }      // map: tep_tin_dinh_kem_url
                                                           // ADD
        public string? TepTinDinhKemTenGoc { get; set; }   // map: tep_tin_dinh_kem_ten_goc
                                                           // ADD
        public string? TepTinDinhKemMime { get; set; }     // map: tep_tin_dinh_kem_mime
                                                           // ADD
        public long? TepTinDinhKemSize { get; set; }       // map: tep_tin_dinh_kem_size
    }
}
