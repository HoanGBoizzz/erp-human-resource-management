using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.AccountWarning;

namespace QLNS_BE.Services
{
    public class AccountWarningService
    {
        private readonly AppDbContext _context;

        public AccountWarningService(AppDbContext context)
        {
            _context = context;
        }

        // Danh sách tài khoản bị cảnh báo (bao gồm cả bị khóa do login fail)
        public async Task<List<AccountWarningListItemDto>> GetWarnedAccountsAsync()
        {
            var accounts = await _context.TaiKhoans
                .Include(tk => tk.NvHoSo)
                .Include(tk => tk.TaiKhoanCanhBaoBoi_Nav)
                .Where(tk =>
                    tk.TrangThaiCanhBao != "BINH_THUONG"  // Tài khoản bị cảnh báo/cấm
                    || tk.ThoiGianKhoa != null            // Tài khoản bị khóa do login fail
                    || tk.SoLanDangNhapSai > 0            // Tài khoản đang có login fail
                )
                .OrderByDescending(tk => tk.NgayCanhBao ?? tk.ThoiGianKhoa)  // Sort theo thời gian gần nhất
                .Select(tk => new AccountWarningListItemDto
                {
                    Id = tk.Id,
                    TenDangNhap = tk.TenDangNhap,
                    HoTen = tk.NvHoSo != null ? tk.NvHoSo.HoTen : null,
                    TrangThaiCanhBao = tk.TrangThaiCanhBao,
                    LyDoCanhBao = tk.LyDoCanhBao,
                    NguoiCanhBao = tk.TaiKhoanCanhBaoBoi_Nav != null ? tk.TaiKhoanCanhBaoBoi_Nav.TenDangNhap : null,
                    NgayCanhBao = tk.NgayCanhBao,
                    SoLanDangNhapSai = tk.SoLanDangNhapSai,
                    ThoiGianKhoa = tk.ThoiGianKhoa
                })
                .ToListAsync();

            return accounts;
        }

        // Đánh cảnh báo/cấm tài khoản
        public async Task<bool> SetWarningAsync(int taiKhoanId, AccountWarningDto dto, int adminId)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(taiKhoanId);
            if (taiKhoan == null) return false;

            // Validate trạng thái
            if (dto.TrangThai != "CANH_BAO" && dto.TrangThai != "CAM")
                throw new Exception("Trạng thái không hợp lệ. Chỉ được CANH_BAO hoặc CAM");

            taiKhoan.TrangThaiCanhBao = dto.TrangThai;
            taiKhoan.LyDoCanhBao = dto.LyDo;
            taiKhoan.TaiKhoanCanhBaoBoiId = adminId;  // Changed: Added "Id" suffix
            taiKhoan.NgayCanhBao = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        // Mở khóa tài khoản và gỡ toàn bộ cảnh báo
        public async Task<bool> UnlockAccountAsync(int taiKhoanId, int adminId)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(taiKhoanId);
            if (taiKhoan == null) return false;

            // Reset toàn bộ: khóa + cảnh báo
            taiKhoan.SoLanDangNhapSai = 0;
            taiKhoan.ThoiGianKhoa = null;
            taiKhoan.TrangThaiCanhBao = "BINH_THUONG";
            taiKhoan.LyDoCanhBao = null;
            taiKhoan.TaiKhoanCanhBaoBoiId = null;
            taiKhoan.NgayCanhBao = null;

            await _context.SaveChangesAsync();
            return true;
        }

        // Gỡ cảnh báo hoàn toàn (reset tất cả warning data)
        public async Task<bool> ClearWarningAsync(int taiKhoanId)
        {
            var taiKhoan = await _context.TaiKhoans.FindAsync(taiKhoanId);
            if (taiKhoan == null) return false;

            // Reset toàn bộ warning data
            taiKhoan.TrangThaiCanhBao = "BINH_THUONG";
            taiKhoan.LyDoCanhBao = null;
            taiKhoan.TaiKhoanCanhBaoBoiId = null;
            taiKhoan.NgayCanhBao = null;
            // Cũng reset lock nếu có
            taiKhoan.SoLanDangNhapSai = 0;
            taiKhoan.ThoiGianKhoa = null;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
