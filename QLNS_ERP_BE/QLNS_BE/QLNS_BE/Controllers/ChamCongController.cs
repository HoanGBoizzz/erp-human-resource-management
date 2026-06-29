using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.ChamCong;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "HR_ACC,GIAM_DOC")]
    public class ChamCongController : ControllerBase
    {
        private readonly ChamCongService _service;
        private readonly AuditLogService _auditLogService;
        private readonly AppDbContext _context;

        public ChamCongController(ChamCongService service, AuditLogService auditLogService, AppDbContext context)
        {
            _service = service;
            _auditLogService = auditLogService;
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        // ============================================================
        // 1. Danh sách bảng công tháng
        // ============================================================
        [HttpGet("bang-cong")]
        public async Task<IActionResult> GetBangCongThang([FromQuery] int nam)
        {
            var data = await _service.GetBangCongThangAsync(nam);
            return Ok(data);
        }

        // ============================================================
        // 2. Chi tiết bảng công tháng
        // ============================================================
        [HttpGet("bang-cong/{id}")]
        public async Task<IActionResult> GetBangCongDetail(int id)
        {
            var data = await _service.GetBangCongThangDetailAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // ============================================================
        // 3. Lấy chấm công của nhân viên 1 ngày
        // ============================================================
        [HttpGet("nhan-vien/{nvId}/ngay")]
        public async Task<IActionResult> GetChamCongNhanVien(int nvId, [FromQuery] DateTime ngay)
        {
            var data = await _service.GetChamCongCuaNhanVienAsync(nvId, ngay);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // ============================================================
        // 4. Cập nhật chấm công 1 ngày
        // ============================================================
        [HttpPut("cap-nhat/{chamCongId}")]
        public async Task<IActionResult> UpdateChamCong(int chamCongId, [FromBody] UpdateChamCongRequestDto dto)
        {
            try
            {
                // Load snapshot trước khi update để ghi audit log
                var snapshot = await _context.ChamCongs
                    .Include(x => x.NvHoSo)
                    .Where(x => x.Id == chamCongId)
                    .Select(x => new { x.Ngay, HoTen = x.NvHoSo.HoTen, x.NvHoSoId })
                    .FirstOrDefaultAsync();

                await _service.UpdateChamCongNgayAsync(chamCongId, dto);

                // Ghi audit log - fire-and-forget
                if (snapshot != null)
                {
                    var ngayStr = snapshot.Ngay.ToString("dd/MM/yyyy");
                    var ghiChuLog = $"Cập nhật chấm công ngày {ngayStr} của {snapshot.HoTen}";
                    if (!string.IsNullOrWhiteSpace(dto.GhiChu))
                        ghiChuLog += $". Ghi chú: {dto.GhiChu}";

                    _ = _auditLogService.LogActionAsync(
                        taiKhoanId: GetUserId(),
                        bang: "Chấm công",
                        doiTuongId: chamCongId,
                        tenDoiTuong: snapshot.HoTen ?? "",
                        hanhDong: "Cập nhật",
                        ghiChu: ghiChuLog
                    );
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ============================================================
        // 4B. Xóa chấm công 1 ngày
        // ============================================================
        [HttpDelete("cap-nhat/{chamCongId}")]
        public async Task<IActionResult> DeleteChamCong(int chamCongId)
        {
            try
            {
                await _service.DeleteChamCongAsync(chamCongId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ============================================================
        // 5. Khoá / Mở khoá bảng công tháng
        // ============================================================
        //[HttpPut("lock")]
        //public async Task<IActionResult> LockBangCong([FromBody] LockBangCongRequestDto dto)
        //{
        //    var taiKhoanId = int.Parse(User.Claims.First(x => x.Type == "userid").Value);
        //    await _service.LockBangCongAsync(dto, taiKhoanId);
        //    return NoContent();
        //}
        [HttpPut("lock")]
        public async Task<IActionResult> LockBangCong([FromBody] LockBangCongRequestDto dto)
        {
            try
            {
                var taiKhoanId = int.Parse(User.Claims.First(x => x.Type == "userid").Value);
                await _service.LockBangCongAsync(dto, taiKhoanId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                // Business logic error - validation failed
                return BadRequest(new { message = ex.Message });
            }
        }
        // ============================================================
        // 6. [NEW] EMPLOYEE – Lấy danh sách tháng có chấm công
        // GET /api/ChamCong/me/thang-list?nam=2025
        // ============================================================
        [HttpGet("me/thang-list")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyMonths([FromQuery] int nam)
        {
            // Lấy nvHoSoId từ token
            var nvIdClaim = User.Claims.FirstOrDefault(x => x.Type == "nvHoSoId");
            if (nvIdClaim == null || !int.TryParse(nvIdClaim.Value, out int nvId))
                return BadRequest(new { message = "Không tìm thấy thông tin nhân viên." });

            var data = await _service.GetMyMonthsAsync(nvId, nam);
            return Ok(data);
        }

        // ============================================================
        // 7. [NEW] EMPLOYEE – Xem chi tiết chấm công tháng của tôi
        // GET /api/ChamCong/me/chi-tiet?thang=12&nam=2025
        // ============================================================
        [HttpGet("me/chi-tiet")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> GetMyTimesheetMonth([FromQuery] int thang, [FromQuery] int nam)
        {
            var nvIdClaim = User.Claims.FirstOrDefault(x => x.Type == "nvHoSoId");
            if (nvIdClaim == null || !int.TryParse(nvIdClaim.Value, out int nvId))
                return BadRequest(new { message = "Không tìm thấy thông tin nhân viên." });

            var data = await _service.GetMyTimesheetMonthAsync(nvId, thang, nam);
            return Ok(data);
        }

        // ============================================================
        // 8. [NEW] HR/GIAM_DOC – Phân trang server-side
        // GET /api/ChamCong/bang-cong-paged?bangCongThangId=1&pageIndex=1&pageSize=20&keyword=nguyen&trangThai=DI_LAM
        // ============================================================
        [HttpGet("bang-cong-paged")]
        public async Task<ActionResult<ChamCongPagedResponseDto>> GetBangCongPaged([FromQuery] ChamCongPagedRequestDto request)
        {
            try
            {
                var result = await _service.GetBangCongPagedAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        // ============================================================
        // 9. CẤU HÌNH GIỜ LÀM VIỆC & THÔNG BÁO
        // ============================================================
        [HttpGet("config")]
        public async Task<IActionResult> GetWorkHoursConfig()
        {
            var config = await _service.GetWorkHoursConfigAsync();
            return Ok(config);
        }

        [HttpPost("config")]
        public async Task<IActionResult> UpdateWorkHoursConfig([FromBody] ChamCongConfigDto dto)
        {
            var taiKhoanId = int.Parse(User.Claims.First(x => x.Type == "userid").Value);
            await _service.UpdateWorkHoursConfigAsync(dto, taiKhoanId);
            return Ok(new { message = "Cập nhật cấu hình thành công" });
        }

        [HttpPost("notify-checkin")]
        [AllowAnonymous] // Hoặc verify token đặc biệt nếu cần bảo mật
        public async Task<IActionResult> NotifyUpcomingCheck()
        {
            // Endpoint này nên được gọi bởi CronJob/Task Scheduler mỗi 1-5 phút
            await _service.NotifyUpcomingCheckInOutAsync();
            return Ok(new { message = "Notification check completed" });
        }
    }
}
