using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Admin.Account;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Entities;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "GIAM_DOC,HR_ACC")]
    public class AccountsController : ControllerBase
    {
        private readonly AccountService _accountService;
        private readonly AuthService _authService;
        private readonly AppDbContext _context;

        public AccountsController(AccountService accountService, AuthService authService, AppDbContext context)
        {
            _accountService = accountService;
            _authService = authService;
            _context = context;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue("userid")!);

        /// <summary>
        /// Danh sách tài khoản (có phân trang + keyword).
        /// GET api/admin/accounts?pageIndex=1&pageSize=20&keyword=abc
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] PagingRequestDto request)
        {
            var result = await _accountService.GetPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Chi tiết 1 tài khoản.
        /// </summary>
        [HttpGet("{id:int}")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _accountService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Tạo tài khoản mới.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountCreateDto dto)
        {
            var currentUserId = GetCurrentUserId();

            // Validation: Kiểm tra NV đã được gán cho TK khác chưa
            if (dto.NvHoSoId.HasValue)
            {
                var existingAccount = await _context.TaiKhoans
                    .AnyAsync(t => t.NvHoSoId == dto.NvHoSoId.Value);
                if (existingAccount)
                    return BadRequest(new { message = "Nhân viên này đã được gán cho tài khoản khác!" });
            }

            var result = await _accountService.CreateAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        /// <summary>
        /// Cập nhật vai trò / gán nhân viên / trạng thái.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] AccountUpdateDto dto)
        {
            var currentUserId = GetCurrentUserId();

            // Validation: Kiểm tra NV đã được gán cho TK khác chưa (trừ TK hiện tại)
            if (dto.NvHoSoId.HasValue)
            {
                var existingAccount = await _context.TaiKhoans
                    .AnyAsync(t => t.NvHoSoId == dto.NvHoSoId.Value && t.Id != id);
                if (existingAccount)
                    return BadRequest(new { message = "Nhân viên này đã được gán cho tài khoản khác!" });
            }

            var result = await _accountService.UpdateAsync(id, dto, currentUserId);
            return Ok(result);
        }

        /// <summary>
        /// Reset mật khẩu.
        /// </summary>
        [HttpPut("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequestDto dto)
        {
            var currentUserId = GetCurrentUserId();
            await _accountService.ResetPasswordAsync(id, dto, currentUserId);
            return NoContent();
        }

        /// <summary>
        /// Xóa tài khoản.
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUserId = GetCurrentUserId();
            await _accountService.DeleteAsync(id, currentUserId);
            return NoContent();
        }

        /// <summary>
        /// [HR_ACC, HR_KETOAN] Xem mật khẩu tạm của tài khoản chưa đăng nhập
        /// GET api/admin/accounts/{id}/temp-password
        /// </summary>
        [HttpGet("{id:int}/temp-password")]
        [Authorize(Roles = "HR_ACC,HR_KETOAN")]
        public async Task<IActionResult> GetTempPassword(int id)
        {
            try
            {
                var account = await _accountService.GetByIdAsync(id);
                if (account == null)
                    return NotFound(new { message = "Không tìm thấy tài khoản" });

                var password = await _authService.GetPasswordForNewAccountAsync(id);
                return Ok(new
                {
                    tenDangNhap = account.TenDangNhap,
                    matKhauTam = password
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

        /// <summary>
        /// Lấy danh sách nhân viên cho dropdown (hiển thị NV nào đã có TK, NV nào chưa)
        /// GET api/admin/accounts/employees-for-dropdown
        /// </summary>
        [HttpGet("employees-for-dropdown")]
        [Authorize(Roles = "HR_ACC,HR_KETOAN,GIAM_DOC")]
        public async Task<IActionResult> GetEmployeesForDropdown()
        {
            var employees = await (
                from nv in _context.NvHoSos.AsNoTracking()
                where nv.TrangThaiLamViec == 1
                join tk in _context.TaiKhoans.AsNoTracking() on nv.Id equals tk.NvHoSoId into tkJoin
                from tk in tkJoin.DefaultIfEmpty()
                select new EmployeeDropdownItemDto
                {
                    Id = nv.Id,
                    MaNhanVien = nv.MaNhanVien,
                    HoTen = nv.HoTen,
                    DaCoTaiKhoan = tk != null,
                    TaiKhoanId = tk != null ? (int?)tk.Id : null,
                    TenDangNhap = tk != null ? tk.TenDangNhap : null
                }
            ).OrderBy(x => x.HoTen).ToListAsync();

            return Ok(employees);
        }

        /// <summary>
        /// Tạo nhân viên nhanh (chỉ cần họ tên) để gán cho tài khoản
        /// POST api/admin/accounts/quick-employee
        /// </summary>
        [HttpPost("quick-employee")]
        [Authorize(Roles = "HR_ACC,HR_KETOAN")]
        public async Task<IActionResult> CreateQuickEmployee([FromBody] QuickEmployeeCreateDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.HoTen))
                return BadRequest(new { message = "Họ tên không được để trống" });

            // Tự sinh mã nếu không có
            var maNhanVien = dto.MaNhanVien;
            if (string.IsNullOrWhiteSpace(maNhanVien))
            {
                maNhanVien = $"NV{DateTime.Now:yyMMddHHmmss}";
            }

            // Kiểm tra mã NV trùng
            var exists = await _context.NvHoSos.AnyAsync(x => x.MaNhanVien == maNhanVien);
            if (exists)
                return BadRequest(new { message = "Mã nhân viên đã tồn tại" });

            // Tạo nhân viên với thông tin tối thiểu
            var nv = new NvHoSo
            {
                MaNhanVien = maNhanVien,
                HoTen = dto.HoTen.Trim(),
                TrangThaiLamViec = 1,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.NvHoSos.Add(nv);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                id = nv.Id,
                maNhanVien = nv.MaNhanVien,
                hoTen = nv.HoTen,
                message = "Tạo nhân viên nhanh thành công"
            });
        }
    }
}


