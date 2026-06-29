namespace QLNS_BE.Models.Dtos.Dashboard
{
    public class HrDashboardDto
    {
        // Nhân sự
        public int TongNhanVien { get; set; }
        public int DangLam { get; set; }
        public int DaNghi { get; set; }

        // Bảng công
        public int TongBangCong { get; set; }
        public int DangNhapLieu { get; set; }
        public int DaChotCong { get; set; }

        // Đơn phép
        public int ChoDuyet { get; set; }
        public int DaDuyet { get; set; }
        public int TuChoi { get; set; }

        // Lương
        public int CanTinh { get; set; }
        public int ChoDuyetLuong { get; set; }
        public int DaKhoa { get; set; }

        // Đề xuất lương
        public int DeXuatChoDuyet { get; set; }
        public int DeXuatDuyet { get; set; }
        public int DeXuatTuChoi { get; set; }
    }
}
