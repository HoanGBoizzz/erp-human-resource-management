//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using QLNS_BE.Models.Entities;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//namespace QLNS_BE.Security
//{
//    public class JwtTokenService
//    {
//        private readonly JwtSettings _jwt;

//        public JwtTokenService(IOptions<JwtSettings> jwtOptions)
//        {
//            _jwt = jwtOptions.Value;

//            if (string.IsNullOrWhiteSpace(_jwt.SecretKey))
//                throw new Exception("SecretKey is NULL. Check appsettings.json!");

//            if (_jwt.SecretKey.Length < 32)
//                throw new Exception("SecretKey must be at least 32 characters!");
//        }

//        public string GenerateAccessToken(TaiKhoan user)
//        {
//            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
//            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

//            //var claims = new List<Claim>
//            //{
//            //    new Claim(JwtRegisteredClaimNames.Sub, user.TenDangNhap),
//            //    new Claim("userid", user.Id.ToString()),

//            //    // DÙNG CHUẨN ClaimTypes.Role
//            //    new Claim(ClaimTypes.Role, user.VaiTro.MaVaiTro)
//            //};
//            var claims = new[]
//{
//    new Claim(JwtRegisteredClaimNames.Sub, user.TenDangNhap),
//    new Claim("userid", user.Id.ToString()),
//    new Claim(ClaimTypes.Role, user.VaiTro.MaVaiTro)   // FIX
//};


//            var token = new JwtSecurityToken(
//                issuer: _jwt.Issuer,
//                audience: _jwt.Audience,
//                claims: claims,
//                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
//                signingCredentials: creds
//            );

//            return new JwtSecurityTokenHandler().WriteToken(token);
//        }
//    }
//}
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using QLNS_BE.Models.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace QLNS_BE.Security
{
    public class JwtTokenService
    {
        private readonly JwtSettings _jwt;

        public JwtTokenService(IOptions<JwtSettings> jwtOptions)
        {
            _jwt = jwtOptions.Value;

            if (string.IsNullOrWhiteSpace(_jwt.SecretKey))
                throw new Exception("SecretKey is NULL. Check appsettings.json!");

            if (_jwt.SecretKey.Length < 32)
                throw new Exception("SecretKey must be at least 32 characters!");
        }

        /// <summary>
        /// Tạo Access Token (thời hạn ngắn, chứa thông tin User)
        /// </summary>
        public string GenerateAccessToken(TaiKhoan user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // [CẬP NHẬT] Sử dụng List<Claim> thay vì mảng cố định để có thể thêm EmployeeId động
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.TenDangNhap),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // ID duy nhất cho token
                new Claim("userid", user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.VaiTro.MaVaiTro) // Chuẩn Role
            };

            // [QUAN TRỌNG] Kiểm tra và thêm EmployeeId nếu tài khoản có liên kết nhân viên
            if (user.NvHoSoId.HasValue)
            {
                claims.Add(new Claim("EmployeeId", user.NvHoSoId.Value.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// [MỚI] Tạo Refresh Token (thời hạn dài, dùng để cấp lại AccessToken)
        /// </summary>
        public string GenerateRefreshToken()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Refresh token thường không cần chứa nhiều thông tin user, chỉ cần tính hợp lệ
            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                expires: DateTime.UtcNow.AddDays(7), // Mặc định 7 ngày (hoặc lấy từ config nếu có)
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}