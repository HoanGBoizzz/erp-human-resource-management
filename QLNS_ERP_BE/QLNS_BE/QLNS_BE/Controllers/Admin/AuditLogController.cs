using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Admin.AuditLog;
using QLNS_BE.Services;

namespace QLNS_BE.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize(Roles = "GIAM_DOC,HR_ACC")]
    public class AuditLogController : ControllerBase
    {
        private readonly AuditLogService _auditLogService;

        public AuditLogController(AuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        /// <summary>
        /// Lấy danh sách audit logs với filter và phân trang
        /// GET api/admin/auditlog?pageIndex=1&pageSize=20&taiKhoanId=1&bang=TAI_KHOAN&hanhDong=INSERT&tuNgay=2024-01-01&denNgay=2024-12-31
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] AuditLogFilterDto filter)
        {
            var result = await _auditLogService.GetPagedAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết một audit log
        /// GET api/admin/auditlog/{id}
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _auditLogService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }
    }
}
