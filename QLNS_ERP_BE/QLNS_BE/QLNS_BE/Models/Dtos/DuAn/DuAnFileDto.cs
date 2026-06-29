namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnFileDto
    {
        public int Id { get; set; }
        public int DuAnId { get; set; }
        public string TenFile { get; set; } = null!;
        public string DuongDanFile { get; set; } = null!;
        public long? KichThuoc { get; set; }
        public string? LoaiFile { get; set; }
        public DateTime NgayTao { get; set; }
        public int? TaiKhoanTaoId { get; set; }
        public string? TenNguoiTao { get; set; }
    }
}
