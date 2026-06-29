namespace QLNS_BE.Models.Entities
{
    public class DuAnThanhVien
    {
        public int Id { get; set; }
        public int DuAnId { get; set; }
        public int NvHoSoId { get; set; }
        public string? VaiTroTrongDuAn { get; set; }   // Leader, Dev, Tester,...
        public DateTime? NgayThamGia { get; set; }
        public DateTime? NgayRoiDi { get; set; }
        public string? GhiChu { get; set; }

        // Navigation
        public DuAn DuAn { get; set; } = null!;
        public NvHoSo NvHoSo { get; set; } = null!;
    }
}
