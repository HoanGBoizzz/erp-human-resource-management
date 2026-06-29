using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // giữ authorize chung
    public class MeController : ControllerBase
    {
        private readonly ChamCongService _chamCongService;

        public MeController(ChamCongService chamCongService /* + các service khác nếu có */)
        {
            _chamCongService = chamCongService;
        }

        private async Task<int?> GetCurrentEmployeeIdAsync()
        {
            // Ưu tiên claim EmployeeId nếu token đã đính kèm
            var empClaim = User.FindFirstValue("EmployeeId") ?? User.FindFirstValue("employeeId");
            if (int.TryParse(empClaim, out var empId))
            {
                return empId;
            }

            // Fallback: map từ tài khoản (userid) sang NvHoSoId
            var userIdClaim = User.FindFirstValue("userid");
            if (int.TryParse(userIdClaim, out var taiKhoanId))
            {
                return await _chamCongService.GetNvHoSoIdByTaiKhoanAsync(taiKhoanId);
            }

            return null;
        }

        // ============================================================
        // 1) EMPLOYEE – DS tháng có chấm công trong năm
        // GET /api/Me/cham-cong/thang-list?nam=2025
        // ============================================================
        [HttpGet("cham-cong/nam-list")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyYears()
        {
            var nvHoSoId = await GetCurrentEmployeeIdAsync();
            if (!nvHoSoId.HasValue)
                return Unauthorized(new { message = "Không xác định được nhân viên từ token" });

            var data = await _chamCongService.GetMyYearsAsync(nvHoSoId.Value);
            return Ok(data);
        }

        [HttpGet("cham-cong/thang-list")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyMonths([FromQuery] int nam)
        {
            var nvHoSoId = await GetCurrentEmployeeIdAsync();
            if (!nvHoSoId.HasValue)
                return Unauthorized(new { message = "Không xác định được nhân viên từ token" });

            var data = await _chamCongService.GetMyMonthsAsync(nvHoSoId.Value, nam);
            return Ok(data);
        }

        // ============================================================
        // 2) EMPLOYEE – chấm công theo tháng của tôi
        // GET /api/Me/cham-cong/thang?thang=12&nam=2025
        // ============================================================
        [HttpGet("cham-cong/thang")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyTimesheetMonth([FromQuery] int thang, [FromQuery] int nam)
        {
            var nvHoSoId = await GetCurrentEmployeeIdAsync();
            if (!nvHoSoId.HasValue)
                return Unauthorized(new { message = "Không xác định được nhân viên từ token" });

            var data = await _chamCongService.GetMyTimesheetMonthAsync(nvHoSoId.Value, thang, nam);
            return Ok(data);
        }

        // ============================================================
        // 3) EMPLOYEE – chấm công trong 1 ngày của tôi
        // GET /api/Me/cham-cong/ngay?ngay=2025-12-21
        // ============================================================
        [HttpGet("cham-cong/ngay")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyTimesheetDay([FromQuery] DateTime ngay)
        {
            var nvHoSoId = await GetCurrentEmployeeIdAsync();
            if (!nvHoSoId.HasValue)
                return Unauthorized(new { message = "Không xác định được nhân viên từ token" });

            var data = await _chamCongService.GetChamCongCuaNhanVienAsync(nvHoSoId.Value, ngay);
            if (data == null) return NotFound();
            return Ok(data);
        }
    }
}
