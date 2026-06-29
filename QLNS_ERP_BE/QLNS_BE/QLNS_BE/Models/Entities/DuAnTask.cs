namespace QLNS_BE.Models.Entities
{
    public class DuAnTask
    {
        public int Id { get; set; }
        public int DuAnId { get; set; }
        public string TieuDe { get; set; } = null!;
        public string? MoTa { get; set; }
        public int NhanVienId { get; set; }
        public int NguoiGiaoId { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        
        // THAP, BINH_THUONG, CAO, KHAN_CAP
        public string UuTien { get; set; } = "BINH_THUONG";
        
        // MOI, DANG_LAM, CHO_REVIEW, HOAN_THANH, HUY
        public string TrangThai { get; set; } = "MOI";
        
        public int PhanTramHoanThanh { get; set; } = 0;
        public string? GhiChu { get; set; }
        public DateTime? NgayHoanThanh { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation Properties
        public DuAn DuAn { get; set; } = null!;
        public NvHoSo NhanVien { get; set; } = null!;
        public NvHoSo NguoiGiao { get; set; } = null!;
    }
}
