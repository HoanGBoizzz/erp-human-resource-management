using QLNS_BE.Models.Dtos.Common;

namespace QLNS_BE.Models.Dtos.Admin.AuditLog
{
    /// <summary>
    /// DTO cho filter và phân trang audit log
    /// </summary>
    public class AuditLogFilterDto : PagingRequestDto
    {
        /// <summary>
        /// Lọc theo ID tài khoản thực hiện hành động
        /// </summary>
        public int? TaiKhoanId { get; set; }

        /// <summary>
        /// Lọc theo tên bảng (VD: TAI_KHOAN, NV_HO_SO)
        /// </summary>
        public string? Bang { get; set; }

        /// <summary>
        /// Lọc theo loại hành động (INSERT, UPDATE, DELETE, ACTION)
        /// </summary>
        public string? HanhDong { get; set; }

        /// <summary>
        /// Lọc từ ngày
        /// </summary>
        public DateTime? TuNgay { get; set; }

        /// <summary>
        /// Lọc đến ngày
        /// </summary>
        public DateTime? DenNgay { get; set; }
    }
}
