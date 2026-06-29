using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Admin.Account;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Entities;
using QLNS_BE.Security;

namespace QLNS_BE.Services
{
    public class AccountService
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher _passwordHasher;
        private readonly AuditLogService _auditLogService;

        public AccountService(AppDbContext context, PasswordHasher passwordHasher, AuditLogService auditLogService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _auditLogService = auditLogService;
        }

        // ============================
        // 1) DANH SÁCH + PHÂN TRANG
        // ============================
        public async Task<PagedResultDto<AccountListItemDto>> GetPagedAsync(PagingRequestDto request)
        {
            if (request.PageIndex <= 0) request.PageIndex = 1;
            if (request.PageSize <= 0) request.PageSize = 20;

            var query =
                from tk in _context.TaiKhoans
                join vt in _context.VaiTros on tk.VaiTroId equals vt.Id
                join nv in _context.NvHoSos on tk.NvHoSoId equals nv.Id into nvJoin
                from nv in nvJoin.DefaultIfEmpty()
                select new AccountListItemDto
                {
                    Id = tk.Id,
                    TenDangNhap = tk.TenDangNhap,
                    VaiTroId = tk.VaiTroId,
                    MaVaiTro = vt.MaVaiTro,
                    TenVaiTro = vt.TenVaiTro,
                    TrangThai = tk.TrangThai,
                    NvHoSoId = tk.NvHoSoId,
                    HoTen = nv != null ? nv.HoTen : null,
                    LanDangNhapCuoi = tk.LanDangNhapCuoi
                };

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                string keyword = request.Keyword.Trim();
                query = query.Where(x =>
                    x.TenDangNhap.Contains(keyword) ||
                    (x.HoTen != null && x.HoTen.Contains(keyword)) ||
                    x.MaVaiTro.Contains(keyword));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderBy(x => x.TenDangNhap)
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResultDto<AccountListItemDto>
            {
                Items = items,
                TotalCount = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };
        }

        // ============================
        // 2) LẤY CHI TIẾT (Cập nhật Realtime)
        // ============================
        public async Task<AccountDetailDto?> GetByIdAsync(int id)
        {
            // Sử dụng AsNoTracking() cho các bảng để bỏ qua cache của EF Core context,
            // giúp lấy được dữ liệu LanDangNhapCuoi mới nhất từ Database ngay lập tức.
            var query =
                from tk in _context.TaiKhoans.AsNoTracking()
                where tk.Id == id
                join vt in _context.VaiTros.AsNoTracking() on tk.VaiTroId equals vt.Id
                join nv in _context.NvHoSos.AsNoTracking() on tk.NvHoSoId equals nv.Id into nvJoin
                from nv in nvJoin.DefaultIfEmpty()
                select new AccountDetailDto
                {
                    Id = tk.Id,
                    TenDangNhap = tk.TenDangNhap,
                    VaiTroId = tk.VaiTroId,
                    MaVaiTro = vt.MaVaiTro,
                    TenVaiTro = vt.TenVaiTro,
                    TrangThai = tk.TrangThai,
                    NvHoSoId = tk.NvHoSoId,
                    HoTen = nv != null ? nv.HoTen : null,

                    // Trường quan trọng cần xem realtime
                    LanDangNhapCuoi = tk.LanDangNhapCuoi,

                    CreatedAt = tk.CreatedAt,
                    UpdatedAt = tk.UpdatedAt,

                    // Có thể xem mật khẩu tạm nếu chưa đăng nhập và có MatKhauTam
                    CanViewPassword = tk.LanDangNhapCuoi == null && tk.MatKhauTam != null
                };

            return await query.FirstOrDefaultAsync();
        }

        // ============================
        // 3) TẠO TÀI KHOẢN
        // ============================
        public async Task<AccountDetailDto> CreateAsync(AccountCreateDto dto, int currentUserId)
        {
            var exists = await _context.TaiKhoans
                .AnyAsync(x => x.TenDangNhap == dto.TenDangNhap);

            if (exists)
                throw new InvalidOperationException("Tên đăng nhập đã tồn tại!");

            var roleExists = await _context.VaiTros
                .AnyAsync(r => r.Id == dto.VaiTroId && r.TrangThai == true);

            if (!roleExists)
                throw new InvalidOperationException("Vai trò không hợp lệ!");

            // ❗ SỬA: dùng CreatePasswordHash
            _passwordHasher.CreatePasswordHash(dto.MatKhau, out string hash, out string salt);

            var entity = new TaiKhoan
            {
                TenDangNhap = dto.TenDangNhap,
                MatKhauHash = hash,
                MatKhauSalt = salt,
                MatKhauTam = dto.MatKhau, // Lưu mật khẩu tạm để HR có thể xem
                VaiTroId = dto.VaiTroId,
                NvHoSoId = dto.NvHoSoId,
                TrangThai = dto.TrangThai,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.TaiKhoans.Add(entity);
            await _context.SaveChangesAsync();

            // Ghi audit log
            await _auditLogService.LogActionAsync(
                currentUserId,
                "TAI_KHOAN",
                entity.Id,
                "INSERT",
                $"Tạo tài khoản mới: {entity.TenDangNhap}"
            );

            return (await GetByIdAsync(entity.Id))!;
        }

        // ============================
        // 4) CẬP NHẬT
        // ============================
        public async Task<AccountDetailDto> UpdateAsync(int id, AccountUpdateDto dto, int currentUserId)
        {
            var entity = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản!");

            var roleExists = await _context.VaiTros
                .AnyAsync(r => r.Id == dto.VaiTroId && r.TrangThai == true);

            if (!roleExists)
                throw new InvalidOperationException("Vai trò không hợp lệ!");

            // Lưu giá trị cũ để audit
            var oldVaiTroId = entity.VaiTroId;
            var oldNvHoSoId = entity.NvHoSoId;
            var oldTrangThai = entity.TrangThai;

            entity.VaiTroId = dto.VaiTroId;
            entity.NvHoSoId = dto.NvHoSoId;
            entity.TrangThai = dto.TrangThai;
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Ghi audit log cho từng trường thay đổi
            if (oldVaiTroId != dto.VaiTroId)
            {
                await _auditLogService.LogFieldChangeAsync(
                    currentUserId,
                    "TAI_KHOAN",
                    id,
                    "VaiTroId",
                    oldVaiTroId.ToString(),
                    dto.VaiTroId.ToString(),
                    "Thay đổi vai trò"
                );
            }

            if (oldNvHoSoId != dto.NvHoSoId)
            {
                await _auditLogService.LogFieldChangeAsync(
                    currentUserId,
                    "TAI_KHOAN",
                    id,
                    "NvHoSoId",
                    oldNvHoSoId?.ToString(),
                    dto.NvHoSoId?.ToString(),
                    "Thay đổi nhân viên liên kết"
                );
            }

            if (oldTrangThai != dto.TrangThai)
            {
                await _auditLogService.LogFieldChangeAsync(
                    currentUserId,
                    "TAI_KHOAN",
                    id,
                    "TrangThai",
                    oldTrangThai.ToString(),
                    dto.TrangThai.ToString(),
                    "Thay đổi trạng thái tài khoản"
                );
            }

            return (await GetByIdAsync(entity.Id))!;
        }

        // ============================
        // 5) RESET PASSWORD
        // ============================
        public async Task ResetPasswordAsync(int id, ResetPasswordRequestDto dto, int currentUserId)
        {
            var entity = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản!");

            _passwordHasher.CreatePasswordHash(dto.NewPassword, out string hash, out string salt);

            entity.MatKhauHash = hash;
            entity.MatKhauSalt = salt;
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Ghi audit log
            await _auditLogService.LogActionAsync(
                currentUserId,
                "TAI_KHOAN",
                id,
                "UPDATE",
                $"Reset mật khẩu cho tài khoản: {entity.TenDangNhap}"
            );
        }

        // ============================
        // 6) XÓA
        // ============================
        public async Task DeleteAsync(int id, int currentUserId)
        {
            var entity = await _context.TaiKhoans.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản!");

            var tenDangNhap = entity.TenDangNhap;

            _context.TaiKhoans.Remove(entity);
            await _context.SaveChangesAsync();

            // Ghi audit log
            await _auditLogService.LogActionAsync(
                currentUserId,
                "TAI_KHOAN",
                id,
                "DELETE",
                $"Xóa tài khoản: {tenDangNhap}"
            );
        }
    }
}
