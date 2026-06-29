using System.ComponentModel.DataAnnotations;

namespace QLNS_BE.Models.Dtos.PhongBan
{
    /// <summary>
    /// DTO tạo mới phòng ban
    /// </summary>
    public class PhongBanCreateDto
    {
        [Required(ErrorMessage = "Mã phòng ban là bắt buộc")]
        [StringLength(20, ErrorMessage = "Mã phòng ban tối đa 20 ký tự")]
        public string MaPhongBan { get; set; } = null!;

        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên phòng ban tối đa 100 ký tự")]
        public string TenPhongBan { get; set; } = null!;

        public int? PhongBanChaId { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? GhiChu { get; set; }
    }
}
