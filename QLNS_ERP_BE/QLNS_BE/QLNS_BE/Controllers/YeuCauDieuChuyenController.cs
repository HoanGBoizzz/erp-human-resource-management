using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.PhongBan;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// API yêu cầu điều chuyển phòng ban - cần Giám đốc duyệt
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class YeuCauDieuChuyenController : ControllerBase
    {
        private readonly YeuCauDieuChuyenService _service;
        private readonly AuditLogService _auditLogService;

        public YeuCauDieuChuyenController(YeuCauDieuChuyenService service, AuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        /// <summary>
        /// Lấy danh sách tất cả yêu cầu điều chuyển
        /// GET api/yeucaudieuchuyen
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetAll([FromQuery] int? trangThai = null)
        {
            var result = await _service.GetAllAsync(trangThai);
            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách yêu cầu chờ duyệt (cho Giám đốc)
        /// GET api/yeucaudieuchuyen/pending
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "GIAM_DOC")]
        public async Task<IActionResult> GetPending()
        {
            var result = await _service.GetPendingAsync();
            return Ok(result);
        }

        /// <summary>
        /// HR tạo yêu cầu điều chuyển
        /// POST api/yeucaudieuchuyen
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Create([FromBody] TaoYeuCauDieuChuyenDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto, GetUserId());

                // Ghi audit log
                await _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Yêu cầu điều chuyển",
                    doiTuongId: result.Id,
                    tenDoiTuong: "",
                    hanhDong: "Tạo yêu cầu",
                    ghiChu: $"Tạo yêu cầu điều chuyển NV ID {dto.NvCongViecId} sang phòng ban ID {dto.PhongBanMoiId}"
                );

                return Ok(new { message = "Đã tạo yêu cầu điều chuyển, chờ Giám đốc duyệt", id = result.Id });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Giám đốc duyệt/từ chối yêu cầu
        /// POST api/yeucaudieuchuyen/duyet
        /// </summary>
        [HttpPost("duyet")]
        [Authorize(Roles = "GIAM_DOC")]
        public async Task<IActionResult> Duyet([FromBody] DuyetYeuCauDieuChuyenDto dto)
        {
            try
            {
                var approved = await _service.DuyetAsync(dto, GetUserId());

                // Ghi audit log
                await _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Yêu cầu điều chuyển",
                    doiTuongId: dto.YeuCauId,
                    tenDoiTuong: "",
                    hanhDong: approved ? "Duyệt" : "Từ chối",
                    ghiChu: $"{(approved ? "Duyệt" : "Từ chối")} yêu cầu điều chuyển ID {dto.YeuCauId}"
                );

                return Ok(new { message = approved ? "Đã duyệt yêu cầu điều chuyển" : "Đã từ chối yêu cầu điều chuyển" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// HR hủy yêu cầu điều chuyển (chỉ khi đang chờ duyệt)
        /// DELETE api/yeucaudieuchuyen/{id}
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _service.CancelAsync(id, GetUserId());

                // Ghi audit log
                await _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Yêu cầu điều chuyển",
                    doiTuongId: id,
                    tenDoiTuong: "",
                    hanhDong: "Hủy yêu cầu",
                    ghiChu: $"Hủy yêu cầu điều chuyển ID {id}"
                );

                return Ok(new { message = "Đã hủy yêu cầu điều chuyển" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
