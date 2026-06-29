using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Dashboard;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class DashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        // ============= DASHBOARD NHÂN VIÊN =============
        public async Task<EmployeeDashboardDto> GetEmployeeDashboardAsync(int userId)
        {
            var nv = await _context.NvHoSos
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (nv == null)
                throw new Exception("Không tìm thấy nhân viên!");

            var month = DateTime.Now.Month;
            var year = DateTime.Now.Year;

            // Tối ưu: Chỉ lấy dữ liệu tháng hiện tại thay vì load hết
            var chamCongStats = await _context.ChamCongs
                .Where(x => x.NvHoSoId == userId && x.Ngay.Month == month && x.Ngay.Year == year)
                .Select(x => new { x.SoGioOt })
                .ToListAsync();

            var luong = await _context.BangLuongThangs
                .Where(x => x.NvHoSoId == userId && x.Thang == month && x.Nam == year)
                .Select(x => new { x.TongLuong, x.TrangThai })
                .FirstOrDefaultAsync();

            // Tối ưu: Đếm trực tiếp từ DB
            var donChoDuyet = await _context.DonXinPheps.CountAsync(x => x.NvHoSoId == userId && x.TrangThai == "CHO_DUYET");
            var donDaDuyet = await _context.DonXinPheps.CountAsync(x => x.NvHoSoId == userId && x.TrangThai == "DA_DUYET");
            var donTuChoi = await _context.DonXinPheps.CountAsync(x => x.NvHoSoId == userId && x.TrangThai == "TU_CHOI");

            var soDuAn = await _context.DuAnThanhViens.CountAsync(x => x.NvHoSoId == userId);

            return new EmployeeDashboardDto
            {
                EmployeeId = nv.Id,
                HoTen = nv.HoTen,
                SoNgayChamCong = chamCongStats.Count,
                TongOt = chamCongStats.Sum(x => x.SoGioOt),
                SoNgayVang = DateTime.DaysInMonth(year, month) - chamCongStats.Count, // Logic tạm thời
                TongLuong = luong?.TongLuong ?? 0,
                TrangThaiLuong = luong?.TrangThai ?? "CHUA_CO",
                DonChoDuyet = donChoDuyet,
                DonDaDuyet = donDaDuyet,
                DonTuChoi = donTuChoi,
                SoDuAnThamGia = soDuAn
            };
        }

        // ============= DASHBOARD HR_ACC =============
        public async Task<HrDashboardDto> GetHrDashboardAsync()
        {
            var totalNV = await _context.NvHoSos.CountAsync();
            var dangLam = await _context.NvCongViecs.CountAsync(x => x.TrangThaiLamViec == 1);
            var nghi = totalNV - dangLam;

            // Tối ưu: Sử dụng CountAsync thay vì ToListAsync() load toàn bộ bảng
            var tongBangCong = await _context.BangCongThangs.CountAsync();
            var dangNhapLieu = await _context.BangCongThangs.CountAsync(x => x.TrangThaiCong == "DANG_NHAP_LIEU");
            var daChotCong = await _context.BangCongThangs.CountAsync(x => x.TrangThaiCong == "DA_CHOT_CONG");

            var choDuyetPhep = await _context.DonXinPheps.CountAsync(x => x.TrangThai == "CHO_DUYET");
            var daDuyetPhep = await _context.DonXinPheps.CountAsync(x => x.TrangThai == "DA_DUYET");
            var tuChoiPhep = await _context.DonXinPheps.CountAsync(x => x.TrangThai == "TU_CHOI");

            var canTinhLuong = await _context.BangLuongThangs.CountAsync(x => x.TrangThai == "CAN_TINH_LAI");
            var choDuyetLuong = await _context.BangLuongThangs.CountAsync(x => x.TaiKhoanDuyetId == null);
            var daKhoaLuong = await _context.BangLuongThangs.CountAsync(x => x.NgayKhoaLuong != null);

            var deXuatChoDuyet = await _context.NvLuongDeXuats.CountAsync(x => x.TrangThai.Contains("CHO_DUYET"));
            var deXuatDuyet = await _context.NvLuongDeXuats.CountAsync(x => x.TrangThai == "DUOC_DUYET");
            var deXuatTuChoi = await _context.NvLuongDeXuats.CountAsync(x => x.TrangThai == "TU_CHOI");

            return new HrDashboardDto
            {
                TongNhanVien = totalNV,
                DangLam = dangLam,
                DaNghi = nghi,

                TongBangCong = tongBangCong,
                DangNhapLieu = dangNhapLieu,
                DaChotCong = daChotCong,

                ChoDuyet = choDuyetPhep,
                DaDuyet = daDuyetPhep,
                TuChoi = tuChoiPhep,

                CanTinh = canTinhLuong,
                ChoDuyetLuong = choDuyetLuong,
                DaKhoa = daKhoaLuong,

                DeXuatChoDuyet = deXuatChoDuyet,
                DeXuatDuyet = deXuatDuyet,
                DeXuatTuChoi = deXuatTuChoi,
            };
        }

        // ============= DASHBOARD GIAM DOC =============
        public async Task<DirectorDashboardDto> GetDirectorDashboardAsync()
        {
            var now = DateTime.Now;

            var nv = await _context.NvHoSos.CountAsync();

            var nghiThang = await _context.NvCongViecs
                .CountAsync(x => x.NgayNghiViec != null &&
                                 x.NgayNghiViec.Value.Month == now.Month &&
                                 x.NgayNghiViec.Value.Year == now.Year);

            // Tối ưu: Tính tổng lương & tổng OT trực tiếp trên DB
            var salaryStats = await _context.BangLuongThangs
                 .Where(x => x.Thang == now.Month && x.Nam == now.Year)
                 .GroupBy(x => 1) // Fake group key để aggregate toàn bộ
                 .Select(g => new
                 {
                     TongLuong = g.Sum(x => x.TongLuong),
                     TongOt = g.Sum(x => x.TongOt),
                     CountChoDuyet = g.Count(x => x.TaiKhoanDuyetId == null)
                 })
                 .FirstOrDefaultAsync();

            var tongDuAn = await _context.DuAns.CountAsync();
            var duAnChoDuyet = await _context.DuAns.CountAsync(x => x.TrangThaiDuAn == "CHO_DUYET_GIAM_DOC");
            var duAnDaDuyet = await _context.DuAns.CountAsync(x => x.TrangThaiDuAn == "DA_DUYET");
            var duAnTuChoi = await _context.DuAns.CountAsync(x => x.TrangThaiDuAn == "TU_CHOI");

            var log = await _context.DuAnNhatKyTrangThais.CountAsync();

            return new DirectorDashboardDto
            {
                TongNhanVien = nv,
                NghiViecTrongThang = nghiThang,

                TongLuongThang = salaryStats?.TongLuong ?? 0,
                TongOtThang = salaryStats?.TongOt ?? 0,
                BangLuongChoDuyet = salaryStats?.CountChoDuyet ?? 0,

                TongDuAn = tongDuAn,
                DuAnChoDuyet = duAnChoDuyet,
                DuAnDaDuyet = duAnDaDuyet,
                DuAnTuChoi = duAnTuChoi,

                NhatKyGanNhat = log
            };
        }
    }
}
