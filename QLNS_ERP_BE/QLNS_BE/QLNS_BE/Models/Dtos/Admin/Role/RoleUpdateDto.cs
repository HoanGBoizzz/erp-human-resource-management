using System.ComponentModel.DataAnnotations;

namespace QLNS_BE.Models.Dtos.Admin.Role
{
    public class RoleUpdateDto
    {
        [Required(ErrorMessage = "Mã vai trò không được để trống")]
        [MaxLength(50)]
        public string MaVaiTro { get; set; } = null!;

        [Required(ErrorMessage = "Tên vai trò không được để trống")]
        [MaxLength(100)]
        public string TenVaiTro { get; set; } = null!;

        public string? MoTa { get; set; }
        public int MucDoUuTien { get; set; }
        public bool TrangThai { get; set; }
    }
}
