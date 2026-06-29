namespace QLNS_BE.Models.Dtos.Admin.Account
{
    /// <summary>
    /// DTO tạo nhân viên nhanh (chỉ cần họ tên) để gán cho tài khoản
    /// </summary>
    public class QuickEmployeeCreateDto
    {
        /// <summary>
        /// Họ tên nhân viên (bắt buộc)
        /// </summary>
        public string HoTen { get; set; } = string.Empty;

        /// <summary>
        /// Mã nhân viên (tự sinh nếu null)
        /// </summary>
        public string? MaNhanVien { get; set; }
    }
}
