namespace QLNS_BE.Models.Dtos.Luong
{
    public class LuongCuaToiDto
    {
        public int Thang { get; set; }
        public int Nam { get; set; }

        public decimal LuongCoBan { get; set; }
        public decimal PhuCapCoDinh { get; set; }

        public decimal TongCong { get; set; }
        public decimal TongOt { get; set; }

        public decimal Thuong { get; set; }
        public decimal KhauTru { get; set; }

        public decimal TongLuong { get; set; }
        public string TrangThai { get; set; } = null!;
    }
}
