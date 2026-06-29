namespace QLNS_BE.Models.Dtos.Luong
{
    public class LuongTongLuongThangDto
    {
        public int Thang { get; set; }
        public int Nam { get; set; }

        public int SoBangLuong { get; set; }

        // Tổng tất cả (mọi trạng thái)
        public decimal TongLuongTatCa { get; set; }

        // Breakdown theo trạng thái (tiện cho dashboard)
        public decimal TongLuongTamTinh { get; set; }
        public decimal TongLuongChoDuyet { get; set; }
        public decimal TongLuongDaDuyet { get; set; }
        public decimal TongLuongTuChoi { get; set; }
        public decimal TongLuongDaKhoa { get; set; }
        public decimal TongLuongKhac { get; set; }
    }
}
