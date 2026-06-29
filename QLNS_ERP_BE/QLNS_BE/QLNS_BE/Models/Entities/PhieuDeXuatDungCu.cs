namespace QLNS_BE.Models.Entities
{
    public class PhieuDeXuatDungCu
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }

        public string TenDungCu { get; set; } = null!;
        public string DonViTinh { get; set; } = null!;
        public int SoLuong { get; set; }
        public decimal GiaTien { get; set; }
        public decimal TongTien { get; set; }
        public string LyDo { get; set; } = null!;

        public string TrangThai { get; set; } = "CHO_DUYET"; // CHO_DUYET | DA_DUYET | TU_CHOI
        public int? NguoiDuyetId { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public NvHoSo NvHoSo { get; set; } = null!;
        public TaiKhoan? NguoiDuyet { get; set; }
    }
}
