namespace QLNS_BE.Models.Entities
{
    /// <summary>
    /// Entity lưu thông báo realtime
    /// </summary>
    public class ThongBao
    {
        public int Id { get; set; }
        
        /// <summary>Người nhận thông báo</summary>
        public int UserId { get; set; }
        
        /// <summary>Người gửi (optional)</summary>
        public int? SenderId { get; set; }
        
        /// <summary>Tiêu đề thông báo</summary>
        public string Title { get; set; } = null!;
        
        /// <summary>Nội dung chi tiết</summary>
        public string? Message { get; set; }
        
        /// <summary>Loại: YEU_CAU_DUYET, DA_DUYET, TU_CHOI, THONG_BAO</summary>
        public string Type { get; set; } = "THONG_BAO";
        
        /// <summary>Entity liên quan: DON_PHEP, BANG_LUONG, DU_AN, DE_XUAT</summary>
        public string? RelatedEntity { get; set; }
        
        /// <summary>ID của entity liên quan</summary>
        public int? RelatedId { get; set; }
        
        /// <summary>URL để navigate khi click</summary>
        public string? Link { get; set; }
        
        /// <summary>Đã đọc chưa</summary>
        public bool IsRead { get; set; } = false;
        
        /// <summary>Thời gian tạo</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public TaiKhoan User { get; set; } = null!;
        public TaiKhoan? Sender { get; set; }
    }
}
