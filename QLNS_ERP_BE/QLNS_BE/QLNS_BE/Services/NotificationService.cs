using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Notification;

namespace QLNS_BE.Services
{
    /// <summary>
    /// Service đếm số thông báo cho sidebar badges
    /// </summary>
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy số lượng thông báo dựa theo role và userId
        /// </summary>
        public async Task<NotificationCountsDto> GetCountsAsync(int userId, string roleCode)
        {
            var counts = new NotificationCountsDto();
            var role = roleCode?.Trim().ToUpper() ?? "";
            counts.DebugRole = $"{roleCode} => {role}";

            if (role == "GIAM_DOC" || role == "GIAMDOC" || role == "GIAM_DOC_1")
            {
                await LoadGiamDocCounts(counts);
            }
            else if (role == "HR_ACC" || role == "HR_KETOAN" || role == "KETOAN" || role == "HR")
            {
                await LoadHrAccCounts(counts, userId);
            }
            else if (role == "EMPLOYEE" || role == "NHANVIEN" || role == "NHAN_VIEN")
            {
                await LoadEmployeeCounts(counts, userId);
            }

            return counts;
        }

        /// <summary>
        /// Load counts for GIAM_DOC - pending approvals
        /// </summary>
        private async Task LoadGiamDocCounts(NotificationCountsDto counts)
        {
            // Bảng lương chờ duyệt
            counts.BangLuongChoDuyet = await _context.BangLuongThangs
                .CountAsync(b => b.TrangThai == "CHO_DUYET_GIAM_DOC");

            // Đề xuất giám đốc chờ duyệt
            counts.DeXuatChoDuyet = await _context.DeXuatGiamDocs
                .CountAsync(d => d.TrangThai == "CHO_DUYET");

            // Dự án chờ duyệt
            counts.DuAnChoDuyet = await _context.DuAns
                .CountAsync(d => d.TrangThaiDuAn == "CHO_DUYET_GIAM_DOC");

            // Điều chuyển chờ duyệt
            counts.DieuChuyenChoDuyet = await _context.YeuCauDieuChuyens
                .CountAsync(y => y.TrangThai == 0);
        }

        /// <summary>
        /// Load counts for HR_ACC
        /// </summary>
        private async Task LoadHrAccCounts(NotificationCountsDto counts, int userId)
        {
            // Đơn nghỉ phép mới (chờ HR xử lý)
            counts.DonPhepMoi = await _context.DonXinPheps
                .CountAsync(d => d.TrangThai == "CHO_DUYET");

            // Dự án đã được GĐ duyệt trong 7 ngày gần đây
            var lastWeek = DateTime.Now.AddDays(-7);
            counts.DuAnDaDuyet = await _context.DuAns
                .CountAsync(d => d.TrangThaiDuAn == "DA_DUYET" && d.NgayDuyet >= lastWeek);

            // Điều chuyển đã được xử lý (do HR tạo)
            counts.DieuChuyenDaXuLy = await _context.YeuCauDieuChuyens
                .CountAsync(y => y.TaiKhoanTaoId == userId && y.TrangThai != 0 && y.NgayDuyet >= lastWeek);

            // Bảng lương đã được duyệt (trong 7 ngày)
            counts.BangLuongDaDuyet = await _context.BangLuongThangs
                .CountAsync(b => b.TrangThai == "DA_DUYET" && b.NgayDuyetGiamDoc >= lastWeek);

            // Tài khoản bị cảnh báo/cấm hoặc đang còn bị khóa (chưa hết 15 phút)
            var lockExpiry = DateTime.Now.AddMinutes(-15);
            counts.TaiKhoanCanhBao = await _context.TaiKhoans
                .CountAsync(tk =>
                    tk.TrangThaiCanhBao != "BINH_THUONG"
                    || (tk.ThoiGianKhoa != null && tk.ThoiGianKhoa > lockExpiry)
                );

            // Đơn yêu cầu nơi làm việc chờ duyệt
            var deXuatCnt = await _context.PhieuDeXuatDungCus.CountAsync(x => x.TrangThai == "CHO_DUYET");
            var tamUngCnt = await _context.PhieuTamUngs.CountAsync(x => x.TrangThai == "CHO_DUYET");
            var diMuonCnt = await _context.DonDiMuons.CountAsync(x => x.TrangThai == "CHO_DUYET");
            counts.YeuCauChoDuyet = deXuatCnt + tamUngCnt + diMuonCnt;
        }

        /// <summary>
        /// Load counts for EMPLOYEE
        /// </summary>
        private async Task LoadEmployeeCounts(NotificationCountsDto counts, int userId)
        {
            // Lấy NvHoSoId từ TaiKhoan
            var taiKhoan = await _context.TaiKhoans
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == userId);

            if (taiKhoan?.NvHoSoId == null) return;
            var nvHoSoId = taiKhoan.NvHoSoId.Value;

            var lastWeek = DateTime.Now.AddDays(-7);

            // Đơn nghỉ phép đã xử lý (trong 7 ngày)
            counts.DonPhepDaXuLy = await _context.DonXinPheps
                .CountAsync(d => d.NvHoSoId == nvHoSoId && d.TrangThai != "CHO_DUYET" && d.UpdatedAt >= lastWeek);

            // Dự án mới được gán (trong 7 ngày)
            counts.DuAnMoiGan = await _context.DuAnThanhViens
                .CountAsync(tv => tv.NvHoSoId == nvHoSoId && tv.NgayThamGia >= lastWeek);

            // Task mới được giao (trong 7 ngày)
            counts.TaskMoi = await _context.DuAnTasks
                .CountAsync(t => t.NhanVienId == nvHoSoId && t.CreatedAt >= lastWeek);

            // Bảng công đã chốt tháng này (TrangThaiCong = "DA_CHOT_CONG")
            var thangNay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            counts.BangCongDaChot = await _context.BangCongThangs
                .CountAsync(b => b.TrangThaiCong == "DA_CHOT_CONG" && b.Thang == thangNay.Month && b.Nam == thangNay.Year);

            // Bảng lương đã tính trong 7 ngày
            counts.BangLuongDaTinh = await _context.BangLuongThangs
                .CountAsync(b => b.NvHoSoId == nvHoSoId && b.TrangThai != "CAN_TINH_LAI" && b.NgayTinhLuong >= lastWeek);
        }
    }
}
