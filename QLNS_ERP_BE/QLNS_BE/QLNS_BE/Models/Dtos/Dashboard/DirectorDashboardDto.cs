namespace QLNS_BE.Models.Dtos.Dashboard
{
    public class DirectorDashboardDto
    {
        // Nhân sự
        public int TongNhanVien { get; set; }
        public int NghiViecTrongThang { get; set; }

        // Lương
        public decimal TongLuongThang { get; set; }
        public decimal TongOtThang { get; set; }
        public int BangLuongChoDuyet { get; set; }

        // Dự án
        public int TongDuAn { get; set; }
        public int DuAnChoDuyet { get; set; }
        public int DuAnDaDuyet { get; set; }
        public int DuAnTuChoi { get; set; }

        // Nhật ký
        public int NhatKyGanNhat { get; set; }
    }
}
