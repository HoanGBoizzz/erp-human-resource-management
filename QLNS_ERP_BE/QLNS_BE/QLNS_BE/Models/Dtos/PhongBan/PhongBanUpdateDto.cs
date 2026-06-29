using System.ComponentModel.DataAnnotations;

namespace QLNS_BE.Models.Dtos.PhongBan
{
    /// <summary>
    /// DTO cập nhật phòng ban
    /// </summary>
    public class PhongBanUpdateDto
    {
        [Required(ErrorMessage = "Tên phòng ban là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên phòng ban tối đa 100 ký tự")]
        public string TenPhongBan { get; set; } = null!;

        public int? PhongBanChaId { get; set; }

        public bool TrangThai { get; set; } = true;

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
        public string? GhiChu { get; set; }
    }
}
