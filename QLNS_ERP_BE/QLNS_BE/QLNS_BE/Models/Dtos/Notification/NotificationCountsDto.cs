namespace QLNS_BE.Models.Dtos.Notification
{
    /// <summary>
    /// DTO chứa số lượng thông báo cho từng mục sidebar
    /// </summary>
    public class NotificationCountsDto
    {
        // ===== GIAM_DOC =====
        /// <summary>Bảng lương chờ duyệt</summary>
        public int BangLuongChoDuyet { get; set; }

        /// <summary>Đề xuất lương chờ duyệt</summary>
        public int DeXuatChoDuyet { get; set; }

        /// <summary>Dự án chờ duyệt</summary>
        public int DuAnChoDuyet { get; set; }

        /// <summary>Yêu cầu điều chuyển chờ duyệt</summary>
        public int DieuChuyenChoDuyet { get; set; }

        // ===== HR_ACC =====
        /// <summary>Đơn nghỉ phép mới cần xử lý</summary>
        public int DonPhepMoi { get; set; }

        /// <summary>Dự án đã được GĐ duyệt (thông báo cho HR)</summary>
        public int DuAnDaDuyet { get; set; }

        /// <summary>Điều chuyển đã được duyệt/từ chối</summary>
        public int DieuChuyenDaXuLy { get; set; }

        /// <summary>Bảng lương đã được GĐ duyệt (thông báo cho HR)</summary>
        public int BangLuongDaDuyet { get; set; }

        /// <summary>Tài khoản bị cảnh báo/khóa do đăng nhập sai</summary>
        public int TaiKhoanCanhBao { get; set; }

        /// <summary>Đơn yêu cầu nơi làm việc đang chờ duyệt (phiếu đề xuất + tạm ứng + đơn đi muộn)</summary>
        public int YeuCauChoDuyet { get; set; }

        // ===== EMPLOYEE =====
        /// <summary>Đơn nghỉ phép đã được xử lý (duyệt/từ chối)</summary>
        public int DonPhepDaXuLy { get; set; }

        /// <summary>Dự án mới được gán</summary>
        public int DuAnMoiGan { get; set; }

        /// <summary>Task mới được giao</summary>
        public int TaskMoi { get; set; }

        /// <summary>Bảng công đã chốt</summary>
        public int BangCongDaChot { get; set; }

        /// <summary>Bảng lương đã tính</summary>
        public int BangLuongDaTinh { get; set; }

        /// <summary>Debug: Role actual seen by backend</summary>
        public string? DebugRole { get; set; }
    }
}
