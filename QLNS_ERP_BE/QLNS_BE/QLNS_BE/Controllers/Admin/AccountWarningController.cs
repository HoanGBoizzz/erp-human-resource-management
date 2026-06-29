using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.AccountWarning;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/accounts")]
    [Authorize(Roles = "HR_ACC,ADMIN")]
    public class AccountWarningController : ControllerBase
    {
        private readonly AccountWarningService _service;
        private readonly AuditLogService _auditLogService;

        public AccountWarningController(AccountWarningService service, AuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);

        // GET /api/admin/accounts/canh-bao - Danh sách tài khoản bị cảnh báo
        [HttpGet("canh-bao")]
        public async Task<IActionResult> GetWarnedAccounts()
        {
            var accounts = await _service.GetWarnedAccountsAsync();
            return Ok(new { accounts });
        }

        // POST /api/admin/accounts/{id}/canh-bao - Đánh cảnh báo/cấm tài khoản
        [HttpPost("{id}/canh-bao")]
        public async Task<IActionResult> SetWarning(int id, [FromBody] AccountWarningDto dto)
        {
            try
            {
                var adminId = GetUserId();
                var success = await _service.SetWarningAsync(id, dto, adminId);

                if (!success)
                    return NotFound(new { message = "Không tìm thấy tài khoản" });

                // Log audit
                await _auditLogService.LogActionAsync(
                    taiKhoanId: adminId,
                    bang: "TaiKhoan",
                    doiTuongId: id,
                    tenDoiTuong: $"ID:{id}",
                    hanhDong: dto.TrangThai == "CAM" ? "Cấm tài khoản" : "Cảnh báo tài khoản",
                    ghiChu: $"Lý do: {dto.LyDo}"
                );

                return Ok(new { message = $"Đã {(dto.TrangThai == "CAM" ? "cấm" : "cảnh báo")} tài khoản" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/admin/accounts/{id}/mo-khoa - Mở khóa tài khoản (giữ lịch sử)
        [HttpPost("{id}/mo-khoa")]
        public async Task<IActionResult> UnlockAccount(int id)
        {
            try
            {
                var adminId = GetUserId();
                var success = await _service.UnlockAccountAsync(id, adminId);

                if (!success)
                    return NotFound(new { message = "Không tìm thấy tài khoản" });

                // Log audit
                await _auditLogService.LogActionAsync(
                    taiKhoanId: adminId,
                    bang: "TaiKhoan",
                    doiTuongId: id,
                    tenDoiTuong: $"ID:{id}",
                    hanhDong: "Mở khóa tài khoản",
                    ghiChu: "Mở khóa và lưu lịch sử"
                );

                return Ok(new { message = "Đã mở khóa tài khoản" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST /api/admin/accounts/{id}/clear-warning - Gỡ cảnh báo hoàn toàn
        [HttpPost("{id}/clear-warning")]
        public async Task<IActionResult> ClearWarning(int id)
        {
            try
            {
                var adminId = GetUserId();
                var success = await _service.ClearWarningAsync(id);

                if (!success)
                    return NotFound(new { message = "Không tìm thấy tài khoản" });

                // Log audit
                await _auditLogService.LogActionAsync(
                    taiKhoanId: adminId,
                    bang: "TaiKhoan",
                    doiTuongId: id,
                    tenDoiTuong: $"ID:{id}",
                    hanhDong: "Gỡ cảnh báo tài khoản",
                    ghiChu: "Xóa toàn bộ warning history và reset lock"
                );

                return Ok(new { message = "Đã gỡ cảnh báo và mở khóa tài khoản hoàn toàn" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
