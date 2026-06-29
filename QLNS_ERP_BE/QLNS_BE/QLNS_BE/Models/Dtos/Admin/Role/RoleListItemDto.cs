namespace QLNS_BE.Models.Dtos.Admin.Role
{
    public class RoleListItemDto
    {
        public int Id { get; set; }
        public string MaVaiTro { get; set; } = null!;
        public string TenVaiTro { get; set; } = null!;
        public string? MoTa { get; set; }
        public int MucDoUuTien { get; set; } 
        public bool TrangThai { get; set; }  
    }
}
