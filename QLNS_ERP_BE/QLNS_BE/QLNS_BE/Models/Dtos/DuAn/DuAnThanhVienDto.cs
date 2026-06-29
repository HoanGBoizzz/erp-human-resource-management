namespace QLNS_BE.Models.Dtos.DuAn
{
    public class DuAnThanhVienDto
    {
        public int Id { get; set; }
        public int NvHoSoId { get; set; }
        public string HoTen { get; set; } = null!;
        public string? VaiTroTrongDuAn { get; set; }
        public DateTime? NgayThamGia { get; set; }
        public DateTime? NgayRoiDi { get; set; }
    }
}
