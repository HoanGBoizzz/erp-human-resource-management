namespace QLNS_BE.Models.Dtos.Task
{
    public class TaskCreateDto
    {
        public string TieuDe { get; set; } = null!;
        public string? MoTa { get; set; }
        public int NhanVienId { get; set; }
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string UuTien { get; set; } = "BINH_THUONG";  // THAP, BINH_THUONG, CAO, KHAN_CAP
    }
}
