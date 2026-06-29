namespace QLNS_BE.Models.Entities
{
    /// <summary>Danh mục loại phụ cấp (master)</summary>
    public class PhuCapLoai
    {
        public int Id { get; set; }
        public string TenPhuCap { get; set; } = null!;
        public string? MoTa { get; set; }
        /// <summary>1 = cố định hàng tháng, 0 = biến đổi</summary>
        public bool LaCoDinh { get; set; } = true;
        public string DonVi { get; set; } = "VND";
        public int ThuTu { get; set; } = 0;
        public bool DangHoatDong { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<NvPhuCap> NvPhuCaps { get; set; } = new List<NvPhuCap>();
    }
}
