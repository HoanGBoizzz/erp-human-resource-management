using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Auth;
using QLNS_BE.Security;

namespace QLNS_BE.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtTokenService _jwtTokenService;
        private readonly PasswordHasher _passwordHasher;

        public AuthService(
            AppDbContext context,
            JwtTokenService jwtTokenService,
            PasswordHasher passwordHasher)
        {
            _context = context;
            _jwtTokenService = jwtTokenService;
            _passwordHasher = passwordHasher;
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            // 1. Tìm user
            var user = await _context.TaiKhoans
                .Include(t => t.VaiTro)
                .FirstOrDefaultAsync(t => t.TenDangNhap == request.Username && t.TrangThai == true);

            if (user == null) throw new UnauthorizedAccessException("Sai tài khoản hoặc mật khẩu");

            // 2. Kiểm tra tài khoản bị CẤM
            if (user.TrangThaiCanhBao == "CAM")
            {
                throw new UnauthorizedAccessException("Tài khoản đã bị cấm. Vui lòng liên hệ HR/Admin.");
            }

            // 3. Kiểm tra tài khoản bị KHÓA do login sai
            if (user.ThoiGianKhoa != null)
            {
                var minutesLocked = (DateTime.UtcNow - user.ThoiGianKhoa.Value).TotalMinutes;
                if (minutesLocked < 15)
                {
                    int remainingMinutes = 15 - (int)minutesLocked;
                    throw new UnauthorizedAccessException($"Tài khoản bị khóa. Vui lòng thử lại sau {remainingMinutes} phút.");
                }
                else
                {
                    // Đã hết thời gian khóa, reset
                    user.SoLanDangNhapSai = 0;
                    user.ThoiGianKhoa = null;
                }
            }

            // 4. Check password
            var ok = _passwordHasher.Verify(request.Password, user.MatKhauHash, user.MatKhauSalt);
            if (!ok)
            {
                // Tăng số lần đăng nhập sai
                user.SoLanDangNhapSai++;

                if (user.SoLanDangNhapSai >= 3)
                {
                    user.ThoiGianKhoa = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    throw new UnauthorizedAccessException("Tài khoản đã bị khóa 15 phút do đăng nhập sai quá 3 lần.");
                }

                await _context.SaveChangesAsync();
                int remainingAttempts = 3 - user.SoLanDangNhapSai;
                throw new UnauthorizedAccessException($"Sai mật khẩu. Còn {remainingAttempts} lần thử.");
            }

            // 5. Đăng nhập thành công - Reset counter VÀ xóa mật khẩu tạm
            user.SoLanDangNhapSai = 0;
            user.ThoiGianKhoa = null;
            user.LanDangNhapCuoi = DateTime.Now;
            user.MatKhauTam = null; // Xóa mật khẩu tạm sau lần đăng nhập đầu
            await _context.SaveChangesAsync();

            // 6. Tạo Token
            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // 7. Trả về kết quả
            return new LoginResponseDto
            {
                Username = user.TenDangNhap,
                Role = user.VaiTro.MaVaiTro,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.Now.AddMinutes(60),
                EmployeeId = user.NvHoSoId
            };
        }

        /// <summary>
        /// Người dùng tự đổi mật khẩu của mình
        /// </summary>
        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Id == userId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản");

            // Verify mật khẩu cũ
            var ok = _passwordHasher.Verify(oldPassword, user.MatKhauHash, user.MatKhauSalt);
            if (!ok)
                throw new InvalidOperationException("Mật khẩu cũ không đúng");

            // Validate mật khẩu mới
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                throw new InvalidOperationException("Mật khẩu mới phải có ít nhất 6 ký tự");

            // Hash mật khẩu mới
            _passwordHasher.CreatePasswordHash(newPassword, out string hash, out string salt);
            user.MatKhauHash = hash;
            user.MatKhauSalt = salt;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// HR xem mật khẩu tạm của tài khoản chưa đăng nhập lần nào
        /// Lưu ý: Chỉ xem được khi LanDangNhapCuoi == null VÀ MatKhauTam còn tồn tại
        /// </summary>
        public async Task<string?> GetPasswordForNewAccountAsync(int taiKhoanId)
        {
            var user = await _context.TaiKhoans.FirstOrDefaultAsync(t => t.Id == taiKhoanId);
            if (user == null)
                throw new KeyNotFoundException("Không tìm thấy tài khoản");

            // Chỉ cho xem nếu tài khoản chưa đăng nhập lần nào
            if (user.LanDangNhapCuoi != null)
                throw new InvalidOperationException("Tài khoản đã đăng nhập, không thể xem mật khẩu");

            // Trả về mật khẩu tạm
            if (string.IsNullOrEmpty(user.MatKhauTam))
                throw new InvalidOperationException("Mật khẩu tạm không tồn tại");

            return user.MatKhauTam;
        }
    }
}
