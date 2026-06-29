namespace QLNS_BE.Models.Entities
{
    /// <summary>Chi tiết thưởng / khấu trừ cho một bảng lương tháng</summary>
    public class BangLuongItem
    {
        public int Id { get; set; }
        public int BangLuongThangId { get; set; }
        /// <summary>THUONG = cộng vào lương | KHAU_TRU = trừ vào lương</summary>
        public string Loai { get; set; } = null!;
        public string LyDo { get; set; } = null!;
        public decimal SoTien { get; set; }
        public int? TaiKhoanTaoId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public BangLuongThang BangLuongThang { get; set; } = null!;
    }
}
