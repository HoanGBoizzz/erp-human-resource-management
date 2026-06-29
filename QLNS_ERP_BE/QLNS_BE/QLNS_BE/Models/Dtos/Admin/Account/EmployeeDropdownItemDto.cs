namespace QLNS_BE.Models.Dtos.Admin.Account
{
    /// <summary>
    /// DTO trả về thông tin nhân viên cho dropdown (bao gồm trạng thái đã có TK hay chưa)
    /// </summary>
    public class EmployeeDropdownItemDto
    {
        public int Id { get; set; }
        public string MaNhanVien { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;

        /// <summary>
        /// true nếu NV đã được gán cho một tài khoản
        /// </summary>
        public bool DaCoTaiKhoan { get; set; }

        /// <summary>
        /// ID tài khoản đã gán (null nếu chưa có)
        /// </summary>
        public int? TaiKhoanId { get; set; }

        /// <summary>
        /// Tên đăng nhập của tài khoản đã gán (null nếu chưa có)
        /// </summary>
        public string? TenDangNhap { get; set; }
    }
}
