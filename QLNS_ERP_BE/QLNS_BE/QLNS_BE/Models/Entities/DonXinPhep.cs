namespace QLNS_BE.Models.Entities
{
    public class DonXinPhep
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public int LoaiPhepId { get; set; }
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }
        public decimal SoNgay { get; set; }
        public string? LyDo { get; set; }
        public string TrangThai { get; set; } = null!;   // CHO_DUYET, DA_DUYET, TU_CHOI
        public int? NguoiDuyetId { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public NvHoSo NvHoSo { get; set; } = null!;
        public LoaiPhep LoaiPhep { get; set; } = null!;
        public TaiKhoan? NguoiDuyet { get; set; }
    }
}
