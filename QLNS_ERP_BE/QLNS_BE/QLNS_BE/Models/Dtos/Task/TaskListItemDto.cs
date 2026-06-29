namespace QLNS_BE.Models.Dtos.Task
{
    public class TaskListItemDto
    {
        public int Id { get; set; }
        public int DuAnId { get; set; }
        public string DuAnTen { get; set; } = null!;
        public string TieuDe { get; set; } = null!;
        public string? MoTa { get; set; }
        public int NhanVienId { get; set; }
        public string NhanVienTen { get; set; } = null!;
        public int NguoiGiaoId { get; set; }
       public string NguoiGiaoTen { get; set; } = null!;
        public DateTime? NgayBatDau { get; set; }
        public DateTime? NgayKetThuc { get; set; }
        public string UuTien { get; set; } = null!;
        public string TrangThai { get; set; } = null!;
        public int PhanTramHoanThanh { get; set; }
        public string? GhiChu { get; set; }
        public DateTime? NgayHoanThanh { get; set; }
    }
}
