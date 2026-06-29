namespace QLNS_BE.Models.Dtos.DeXuatGiamDoc
{
    /// <summary>Item trong danh sách đề xuất</summary>
    public class DeXuatGiamDocListItemDto
    {
        public int Id { get; set; }
        public string TenDeXuat { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayDeXuat { get; set; }
        public string TrangThai { get; set; } = null!;
        public string? TepTinUrl { get; set; }
        public string? TepTinTenGoc { get; set; }
        public string? TenNguoiTao { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
    }

    /// <summary>Chi tiết đề xuất</summary>
    public class DeXuatGiamDocDetailDto
    {
        public int Id { get; set; }
        public string TenDeXuat { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayDeXuat { get; set; }
        public string TrangThai { get; set; } = null!;

        public string? TepTinUrl { get; set; }
        public string? TepTinTenGoc { get; set; }
        public string? TepTinMime { get; set; }
        public long? TepTinSize { get; set; }

        public int TaiKhoanTaoId { get; set; }
        public string? TenNguoiTao { get; set; }
        public int? TaiKhoanDuyetId { get; set; }
        public string? TenNguoiDuyet { get; set; }
        public DateTime? NgayGuiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? LyDoTuChoi { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>Tạo mới đề xuất</summary>
    public class DeXuatGiamDocCreateDto
    {
        public string TenDeXuat { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayDeXuat { get; set; }
    }

    /// <summary>Cập nhật đề xuất (chỉ khi NHAP)</summary>
    public class DeXuatGiamDocUpdateDto
    {
        public string TenDeXuat { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayDeXuat { get; set; }
    }

    /// <summary>Giám đốc duyệt / từ chối</summary>
    public class DeXuatGiamDocApproveDto
    {
        public bool DongY { get; set; }
        public string? LyDoTuChoi { get; set; }
    }
}
