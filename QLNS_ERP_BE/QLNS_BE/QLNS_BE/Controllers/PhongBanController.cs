using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.PhongBan;
using QLNS_BE.Services;
using System.IO;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// API quản lý phòng ban
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "HR_ACC,GIAM_DOC,HR_KETOAN")]
    public class PhongBanController : ControllerBase
    {
        private readonly PhongBanService _service;
        private readonly AuditLogService _auditLogService;
        private readonly AppDbContext _context;

        public PhongBanController(PhongBanService service, AuditLogService auditLogService, AppDbContext context)
        {
            _service = service;
            _auditLogService = auditLogService;
            _context = context;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        /// <summary>
        /// Lấy danh sách phòng ban
        /// GET api/phongban
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        /// <summary>
        /// Lấy chi tiết phòng ban kèm danh sách nhân viên
        /// GET api/phongban/{id}
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null)
                return NotFound(new { message = "Không tìm thấy phòng ban" });

            return Ok(result);
        }

        /// <summary>
        /// Thêm mới phòng ban
        /// POST api/phongban
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> Create([FromBody] PhongBanCreateDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);

                // Ghi log
                await _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Phòng ban",
                    doiTuongId: result.Id,
                    tenDoiTuong: result.TenPhongBan,
                    hanhDong: "Thêm mới",
                    ghiChu: $"Thêm phòng ban: {result.MaPhongBan} - {result.TenPhongBan}"
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật phòng ban
        /// PUT api/phongban/{id}
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> Update(int id, [FromBody] PhongBanUpdateDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                if (result == null)
                    return NotFound(new { message = "Không tìm thấy phòng ban" });

                // Ghi log
                await _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Phòng ban",
                    doiTuongId: result.Id,
                    tenDoiTuong: result.TenPhongBan,
                    hanhDong: "Cập nhật",
                    ghiChu: $"Cập nhật phòng ban: {result.MaPhongBan}"
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa phòng ban
        /// DELETE api/phongban/{id}
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // Lấy thông tin trước khi xóa để log
                var pb = await _service.GetByIdAsync(id);
                if (pb == null)
                    return NotFound(new { message = "Không tìm thấy phòng ban" });

                var deleted = await _service.DeleteAsync(id);
                if (!deleted)
                    return NotFound(new { message = "Không tìm thấy phòng ban" });

                // Ghi log
                await _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Phòng ban",
                    doiTuongId: id,
                    tenDoiTuong: pb.TenPhongBan,
                    hanhDong: "Xóa",
                    ghiChu: $"Xóa phòng ban: {pb.MaPhongBan} - {pb.TenPhongBan}"
                );

                return Ok(new { message = "Đã xóa phòng ban thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Điều chuyển nhân viên sang phòng ban khác
        /// POST api/phongban/chuyen-nhan-vien
        /// </summary>
        [HttpPost("chuyen-nhan-vien")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ChuyenNhanVien([FromForm] ChuyenPhongBanDto dto, IFormFile? quyetDinh = null)
        {
            try
            {
                // Kiểm tra phân quyền theo cấp bậc: HR_KETOAN chỉ được điều chuyển NV cùng cấp hoặc thấp hơn
                if (User.IsInRole("HR_KETOAN"))
                {
                    var currentLevel = await GetCurrentUserRoleLevelAsync();
                    var targetLevel = await GetEmployeeRoleLevelByNvCongViecAsync(dto.NvCongViecId);
                    if (targetLevel > currentLevel)
                        return StatusCode(403, new { message = "Bạn không có quyền điều chuyển nhân viên có cấp bậc cao hơn" });
                }

                // Validate extension trước (sync, không I/O)
                string? pendingFilePath = null;
                string? quyetDinhUrl = null;
                if (quyetDinh != null && quyetDinh.Length > 0)
                {
                    var ext = Path.GetExtension(quyetDinh.FileName).ToLowerInvariant();
                    if (!new[] { ".pdf", ".doc", ".docx" }.Contains(ext))
                        return BadRequest(new { message = "Quyết định chỉ cho phép file PDF/Word" });

                    var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "dieu-chuyen");
                    Directory.CreateDirectory(folder);
                    var safeName = $"{Guid.NewGuid():N}{ext}";
                    pendingFilePath = Path.Combine(folder, safeName);
                    quyetDinhUrl = $"/uploads/dieu-chuyen/{safeName}";
                }

                // Chạy DB và file I/O song song
                var dbTask = _service.ChuyenNhanVienAsync(dto, GetUserId());
                var fileTask = pendingFilePath != null
                    ? SaveFileAsync(quyetDinh!, pendingFilePath)
                    : Task.CompletedTask;

                await Task.WhenAll(dbTask, fileTask);

                // Fire-and-forget audit log – không chặn response
                var userId = GetUserId();
                var ghiChu = $"Chuyển nhân viên sang phòng ban ID: {dto.PhongBanMoiId}";
                if (quyetDinhUrl != null) ghiChu += " | kèm quyết định điều chuyển";
                _ = _auditLogService.LogActionAsync(
                    taiKhoanId: userId,
                    bang: "Nhân viên",
                    doiTuongId: dto.NvCongViecId,
                    tenDoiTuong: "",
                    hanhDong: "Điều chuyển",
                    ghiChu: ghiChu
                );

                return Ok(new { message = "Đã điều chuyển nhân viên thành công", quyetDinhUrl });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa nhân viên nghỉ việc khỏi danh sách phòng ban
        /// DELETE api/phongban/nhan-vien/{nvCongViecId}
        /// </summary>
        [HttpDelete("nhan-vien/{nvCongViecId:int}")]
        public async Task<IActionResult> XoaNhanVienKhoiPhongBan(int nvCongViecId)
        {
            try
            {
                // Kiểm tra phân quyền theo cấp bậc
                if (User.IsInRole("HR_KETOAN"))
                {
                    var currentLevel = await GetCurrentUserRoleLevelAsync();
                    var targetLevel = await GetEmployeeRoleLevelByNvCongViecAsync(nvCongViecId);
                    if (targetLevel > currentLevel)
                        return StatusCode(403, new { message = "Bạn không có quyền xóa nhân viên có cấp bậc cao hơn" });
                }

                await _service.XoaNhanVienKhoiPhongBanAsync(nvCongViecId);
                _ = _auditLogService.LogActionAsync(
                    taiKhoanId: GetUserId(),
                    bang: "Nhân viên",
                    doiTuongId: nvCongViecId,
                    tenDoiTuong: "",
                    hanhDong: "Xóa khỏi phòng ban",
                    ghiChu: $"Xóa nhân viên nghỉ việc khỏi phòng ban"
                );
                return Ok(new { message = "Đã xóa nhân viên khỏi phòng ban" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>Lấy mức độ ưu tiên vai trò của người dùng hiện tại</summary>
        private Task<int> GetCurrentUserRoleLevelAsync()
        {
            var userId = GetUserId();
            return _context.TaiKhoans
                .Where(t => t.Id == userId)
                .Select(t => t.VaiTro.MucDoUuTien)
                .FirstOrDefaultAsync();
        }

        /// <summary>Lấy mức độ ưu tiên vai trò của nhân viên qua NvCongViecId</summary>
        private Task<int> GetEmployeeRoleLevelByNvCongViecAsync(int nvCongViecId)
        {
            return (
                from cv in _context.NvCongViecs
                join t in _context.TaiKhoans on cv.NvHoSoId equals t.NvHoSoId
                join v in _context.VaiTros on t.VaiTroId equals v.Id
                where cv.Id == nvCongViecId
                select v.MucDoUuTien
            ).FirstOrDefaultAsync();
        }

        private static async Task SaveFileAsync(IFormFile file, string fullPath)
        {
            using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, useAsync: true);
            await file.CopyToAsync(stream);
        }
    }
}
