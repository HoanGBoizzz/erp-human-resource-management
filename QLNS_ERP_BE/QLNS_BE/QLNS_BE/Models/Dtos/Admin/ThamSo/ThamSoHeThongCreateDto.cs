using System.ComponentModel.DataAnnotations;

namespace QLNS_BE.Models.Dtos.Admin.ThamSo
{
    public class ThamSoHeThongCreateDto
    {
        [Required]
        public string MaThamSo { get; set; } = null!;
        [Required]
        public string GiaTri { get; set; } = null!;
        public string? MoTa { get; set; }
        public DateTime NgayBatDauHieuLuc { get; set; } = DateTime.Today;
        public DateTime? NgayKetThucHieuLuc { get; set; }
    }
}
