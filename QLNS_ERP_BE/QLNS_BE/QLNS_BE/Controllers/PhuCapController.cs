using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "HR_ACC")]
    public class PhuCapController : ControllerBase
    {
        private readonly PhuCapService _service;
        private readonly AuditLogService _audit;

        public PhuCapController(PhuCapService service, AuditLogService audit)
        {
            _service = service;
            _audit = audit;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        // ─── LOẠI PHỤ CẤP ────────────────────────────

        [HttpGet("loai")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLoai()
            => Ok(await _service.GetAllLoaiAsync());

        [HttpPost("loai")]
        public async Task<IActionResult> CreateLoai([FromBody] PhuCapLoaiCreateDto dto)
        {
            var result = await _service.CreateLoaiAsync(dto);
            await _audit.LogActionAsync(GetUserId(), "PHU_CAP_LOAI", result.Id, result.TenPhuCap, "Tạo mới", $"Tạo loại phụ cấp: {result.TenPhuCap}");
            return Ok(result);
        }

        [HttpPut("loai/{id}")]
        public async Task<IActionResult> UpdateLoai(int id, [FromBody] PhuCapLoaiCreateDto dto)
        {
            var ok = await _service.UpdateLoaiAsync(id, dto);
            if (!ok) return NotFound();
            await _audit.LogActionAsync(GetUserId(), "PHU_CAP_LOAI", id, dto.TenPhuCap, "Cập nhật", $"Sửa loại phụ cấp ID={id}");
            return Ok(new { message = "Đã cập nhật" });
        }

        [HttpPatch("loai/{id}/toggle")]
        public async Task<IActionResult> ToggleLoai(int id)
        {
            var ok = await _service.ToggleLoaiAsync(id);
            if (!ok) return NotFound();
            return Ok(new { message = "Đã thay đổi trạng thái" });
        }

        // ─── PHỤ CẤP NHÂN VIÊN ────────────────────────

        /// <summary>GET tất cả phụ cấp (dùng cho trang quản lý phụ cấp)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _service.GetAllAsync());

        /// <summary>GET phụ cấp của 1 nhân viên</summary>
        [HttpGet("nhan-vien/{nvId}")]
        public async Task<IActionResult> GetByNv(int nvId)
            => Ok(await _service.GetByNvAsync(nvId));

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NvPhuCapCreateDto dto)
        {
            var result = await _service.CreateAsync(dto, GetUserId());
            await _audit.LogActionAsync(GetUserId(), "NV_PHU_CAP", result.Id, result.HoTen, "Tạo mới",
                $"Thêm phụ cấp '{result.TenPhuCap}' = {result.SoTien:N0} VND cho {result.HoTen}");
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NvPhuCapUpdateDto dto)
        {
            var ok = await _service.UpdateAsync(id, dto);
            if (!ok) return NotFound();
            await _audit.LogActionAsync(GetUserId(), "NV_PHU_CAP", id, "", "Cập nhật", $"Sửa phụ cấp ID={id}");
            return Ok(new { message = "Đã cập nhật" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _service.DeleteAsync(id);
            if (!ok) return NotFound();
            await _audit.LogActionAsync(GetUserId(), "NV_PHU_CAP", id, "", "Xóa", $"Xóa phụ cấp ID={id}");
            return Ok(new { message = "Đã xóa" });
        }
    }
}
