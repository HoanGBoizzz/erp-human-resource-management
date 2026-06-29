namespace QLNS_BE.Models.Entities
{
    public class VaiTro
    {
        public int Id { get; set; }
        public string MaVaiTro { get; set; } = null!;  // EMPLOYEE, HR_ACC, GIAM_DOC, SUPER_ADMIN
        public string TenVaiTro { get; set; } = null!;
        public string? MoTa { get; set; }
        public int MucDoUuTien { get; set; }
        public bool TrangThai { get; set; }

        // Navigation
        public ICollection<TaiKhoan> TaiKhoans { get; set; } = new List<TaiKhoan>();
        public ICollection<VaiTroChucNang> VaiTroChucNangs { get; set; } = new List<VaiTroChucNang>();
    }
}
