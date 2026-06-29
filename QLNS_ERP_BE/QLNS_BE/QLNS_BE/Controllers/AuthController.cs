using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Auth;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        public AuthController(AuthService authService) 
        {
            _authService = authService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Trả về JSON response thay vì exception
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Người dùng tự đổi mật khẩu của mình
        /// PUT api/auth/change-password
        /// </summary>
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = GetUserId();
                await _authService.ChangePasswordAsync(userId, dto.OldPassword, dto.NewPassword);
                return Ok(new { message = "Đổi mật khẩu thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// [HR_ACC] Xem mật khẩu của tài khoản chưa đăng nhập lần nào
        /// GET api/auth/view-password/{taiKhoanId}
        /// </summary>
        [HttpGet("view-password/{taiKhoanId:int}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> ViewPasswordForNewAccount(int taiKhoanId)
        {
            try
            {
                var password = await _authService.GetPasswordForNewAccountAsync(taiKhoanId);
                return Ok(new { 
                    message = "Tài khoản chưa đăng nhập, nhưng mật khẩu đã được mã hóa không thể xem plaintext",
                    canViewPassword = password != null
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
