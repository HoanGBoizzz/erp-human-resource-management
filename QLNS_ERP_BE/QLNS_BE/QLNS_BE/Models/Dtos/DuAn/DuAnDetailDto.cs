namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnDetailDto
    {
        public int Id { get; set; }
        public string MaDuAn { get; set; } = null!;
        public string TenDuAn { get; set; } = null!;
        public string? MoTa { get; set; }
        public decimal? NganSach { get; set; }
        public string TrangThaiDuAn { get; set; } = null!;
        public int? NvPhuTrachId { get; set; }
        public string? TenNvPhuTrach { get; set; }

        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }

        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        // ADD
        public string? TepTinDinhKemUrl { get; set; }
        // ADD
        public string? TepTinDinhKemTenGoc { get; set; }
        // ADD
        public string? TepTinDinhKemMime { get; set; }
        // ADD
        public long? TepTinDinhKemSize { get; set; }


        public List<DuAnThanhVienDto> ThanhViens { get; set; } = new();
        public List<DuAnNhatKyTrangThaiDto> NhatKyTrangThais { get; set; } = new();
        public List<DuAnFileDto> Files { get; set; } = new();
        
        // ID tài khoản đã tạo dự án (cho notification)
        public int? TaiKhoanTaoId { get; set; }
    }
}
