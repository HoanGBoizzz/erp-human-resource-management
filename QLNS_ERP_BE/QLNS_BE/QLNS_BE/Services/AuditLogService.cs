using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Admin.AuditLog;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Entities;

namespace QLNS_BE.Services
{
    public class AuditLogService
    {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context)
        {
            _context = context;
        }

        // ============================
        // 1) LẤY DANH SÁCH CÓ LỌC + PHÂN TRANG
        // ============================
        public async Task<PagedResultDto<AuditLogDto>> GetPagedAsync(AuditLogFilterDto filter)
        {
            if (filter.PageIndex <= 0) filter.PageIndex = 1;
            if (filter.PageSize <= 0) filter.PageSize = 20;

            var query =
                from log in _context.AuditLogs
                join tk in _context.TaiKhoans on log.TaiKhoanId equals tk.Id
                select new AuditLogDto
                {
                    Id = log.Id,
                    TaiKhoanId = log.TaiKhoanId,
                    TenDangNhap = tk.TenDangNhap,
                    ThoiGian = log.ThoiGian,
                    Bang = log.Bang,
                    TenDoiTuong = log.TenDoiTuong,
                    DoiTuongId = log.DoiTuongId,
                    Truong = log.Truong,
                    GiaTriCu = log.GiaTriCu,
                    GiaTriMoi = log.GiaTriMoi,
                    HanhDong = log.HanhDong,
                    GhiChu = log.GhiChu
                };

            // Áp dụng các bộ lọc
            if (filter.TaiKhoanId.HasValue)
            {
                query = query.Where(x => x.TaiKhoanId == filter.TaiKhoanId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Bang))
            {
                query = query.Where(x => x.Bang == filter.Bang);
            }

            if (!string.IsNullOrWhiteSpace(filter.HanhDong))
            {
                query = query.Where(x => x.HanhDong == filter.HanhDong);
            }

            if (filter.TuNgay.HasValue)
            {
                query = query.Where(x => x.ThoiGian >= filter.TuNgay.Value);
            }

            if (filter.DenNgay.HasValue)
            {
                // Thêm 1 ngày để bao gồm cả ngày DenNgay
                var denNgayEnd = filter.DenNgay.Value.AddDays(1);
                query = query.Where(x => x.ThoiGian < denNgayEnd);
            }

            // Keyword search (tìm trong ghi chú hoặc tên đăng nhập)
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                string keyword = filter.Keyword.Trim();
                query = query.Where(x =>
                    x.TenDangNhap.Contains(keyword) ||
                    (x.GhiChu != null && x.GhiChu.Contains(keyword)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.ThoiGian)  // Mới nhất trước
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResultDto<AuditLogDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize
            };
        }

        // ============================
        // 2) LẤY CHI TIẾT
        // ============================
        public async Task<AuditLogDto?> GetByIdAsync(int id)
        {
            var query =
                from log in _context.AuditLogs
                where log.Id == id
                join tk in _context.TaiKhoans on log.TaiKhoanId equals tk.Id
                select new AuditLogDto
                {
                    Id = log.Id,
                    TaiKhoanId = log.TaiKhoanId,
                    TenDangNhap = tk.TenDangNhap,
                    ThoiGian = log.ThoiGian,
                    Bang = log.Bang,
                    DoiTuongId = log.DoiTuongId,
                    Truong = log.Truong,
                    GiaTriCu = log.GiaTriCu,
                    GiaTriMoi = log.GiaTriMoi,
                    HanhDong = log.HanhDong,
                    GhiChu = log.GhiChu
                };

            return await query.FirstOrDefaultAsync();
        }

        // ============================
        // 3) HELPER: GHI LOG HÀNH ĐỘNG ĐƠN GIẢN
        // ============================
        /// <summary>
        /// Ghi log cho các hành động đơn giản (INSERT, DELETE, ACTION)
        /// </summary>
        public async Task LogActionAsync(
            int taiKhoanId,
            string bang,
            int? doiTuongId,
            string? tenDoiTuong,
            string hanhDong,
            string? ghiChu = null)
        {
            var log = new AuditLog
            {
                TaiKhoanId = taiKhoanId,
                ThoiGian = DateTime.Now,
                Bang = bang,
                TenDoiTuong= tenDoiTuong,
                DoiTuongId = doiTuongId,
                HanhDong = hanhDong,
                GhiChu = ghiChu
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        // ============================
        // 4) HELPER: GHI LOG THAY ĐỔI TRƯỜNG
        // ============================
        /// <summary>
        /// Ghi log cho việc thay đổi giá trị của một trường cụ thể (UPDATE)
        /// </summary>
        public async Task LogFieldChangeAsync(
            int taiKhoanId,
            string bang,
            int doiTuongId,
            string truong,
            string? tenDoiTuong,
            string? giaTriCu,
            string? giaTriMoi,
            string? ghiChu = null)
        {
            var log = new AuditLog
            {
                TaiKhoanId = taiKhoanId,
                ThoiGian = DateTime.Now,
                Bang = bang,
                TenDoiTuong = tenDoiTuong,
                DoiTuongId = doiTuongId,
                Truong = truong,
                GiaTriCu = giaTriCu,
                GiaTriMoi = giaTriMoi,
                HanhDong = "UPDATE",
                GhiChu = ghiChu
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

    }
}
