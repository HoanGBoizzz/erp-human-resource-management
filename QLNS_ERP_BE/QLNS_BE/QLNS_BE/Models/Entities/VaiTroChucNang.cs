namespace QLNS_BE.Models.Entities
{
    public class VaiTroChucNang
    {
        public int Id { get; set; }
        public int VaiTroId { get; set; }
        public int ChucNangId { get; set; }
        public bool QuyenXem { get; set; }
        public bool QuyenThem { get; set; }
        public bool QuyenSua { get; set; }
        public bool QuyenXoa { get; set; }
        public bool QuyenDuyet { get; set; }

        // Navigation
        public VaiTro VaiTro { get; set; } = null!;
        public ChucNang ChucNang { get; set; } = null!;
    }
}
