namespace QLNS_BE.Models.Entities
{
    public class DuAnFile
    {
        public int Id { get; set; }
        public int DuAnId { get; set; }
        public string TenFile { get; set; } = null!;
        public string DuongDanFile { get; set; } = null!;
        public long? KichThuoc { get; set; }
        public string? LoaiFile { get; set; }
        public DateTime NgayTao { get; set; }
        public int? TaiKhoanTaoId { get; set; }

        // Navigation
        public DuAn DuAn { get; set; } = null!;
        public TaiKhoan? TaiKhoanTao { get; set; }
    }
}
