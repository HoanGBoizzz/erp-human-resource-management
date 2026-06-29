namespace QLNS_BE.Models.Dtos.Dashboard
{
    public class EmployeeDashboardDto
    {
        public int EmployeeId { get; set; }
        public string HoTen { get; set; } = null!;

        // Chấm công
        public int SoNgayChamCong { get; set; }
        public decimal TongOt { get; set; }
        public int SoNgayVang { get; set; }

        // Lương
        public decimal TongLuong { get; set; }
        public string TrangThaiLuong { get; set; } = null!;

        // Nghỉ phép
        public int DonChoDuyet { get; set; }
        public int DonDaDuyet { get; set; }
        public int DonTuChoi { get; set; }

        // Dự án
        public int SoDuAnThamGia { get; set; }
    }
}
