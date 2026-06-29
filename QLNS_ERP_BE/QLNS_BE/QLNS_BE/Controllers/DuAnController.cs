using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Dtos.DuAn;
using QLNS_BE.Services;
using System.IO;
using System.Security.Claims;
namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DuAnController : ControllerBase
    {
        private readonly DuAnService _service;
        private readonly AuditLogService _auditLogService;
        private readonly ThongBaoService _thongBaoService;

        public DuAnController(
            DuAnService service,
            AuditLogService auditLogService,
            ThongBaoService thongBaoService)
        {
            _service = service;
            _auditLogService = auditLogService;
            _thongBaoService = thongBaoService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue("userid")!);

        private string GetRole() =>
            User.FindFirstValue("role")!;

        // =====================================================================
        // 1) LẤY DANH SÁCH DỰ ÁN (HR & GIÁM ĐỐC mới được xem hết)
        // =====================================================================
        [HttpGet]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetList()
        {
            var list = await _service.GetListAsync();
            return Ok(list);
        }

        // =====================================================================
        // 2) XEM CHI TIẾT DỰ ÁN (Employee chỉ xem dự án có mình tham gia)
        // =====================================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var role = GetRole();
            var userId = GetUserId();

            var detail = await _service.GetDetailAsync(id);
            if (detail == null)
                return NotFound();

            // Nếu là nhân viên → CHỈ xem dự án mình tham gia
            if (role == "EMPLOYEE")
            {
                bool coThamGia = detail.ThanhViens.Any(x => x.NvHoSoId == userId);
                if (!coThamGia)
                    return Forbid();
            }

            return Ok(detail);
        }

        // =====================================================================
        // 3) TẠO DỰ ÁN (HR mới được tạo)
        // =====================================================================
        [HttpPost]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Create(DuAnCreateDto dto)
        {
            var userId = GetUserId();
            var newId = await _service.CreateAsync(dto, userId);

            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Dự án",
                doiTuongId: newId,
                tenDoiTuong: dto.TenDuAn,
                hanhDong: "Thêm mới",
                ghiChu: $"Thêm mới dự án: {dto.TenDuAn}"
            );
            // [REALTIME] Broadcast để các danh sách UI tự refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DU_AN",
                entityId: newId,
                action: "CREATED"
            );
            return Ok(new { id = newId });
        }

        // =====================================================================
        // 4) CẬP NHẬT DỰ ÁN (HR chỉnh sửa)
        // =====================================================================
        [HttpPut("{id}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Update(int id, DuAnUpdateDto dto)
        {
            var userId = GetUserId();

            // 1. Lấy dữ liệu cũ
            var oldData = await _service.GetDetailAsync(id);
            if (oldData == null) return NotFound();

            // 2. Thực hiện update
            var ok = await _service.UpdateAsync(id, dto);
            if (!ok) return NotFound();

            // 3. Lấy dữ liệu mới sau khi update
            var newData = await _service.GetDetailAsync(id);

            // 4. LOGGING (Sử dụng Helper Function)
            async Task LogChange(string tenTruong, object? giaTriCu, object? giaTriMoi)
            {
                string v1 = giaTriCu?.ToString() ?? "";
                string v2 = giaTriMoi?.ToString() ?? "";

                // Nếu là ngày tháng, format lại cho đẹp
                if (giaTriCu is DateTime d1) v1 = d1.ToString("dd/MM/yyyy");
                if (giaTriMoi is DateTime d2) v2 = d2.ToString("dd/MM/yyyy");

                if (v1 != v2)
                {
                    await _auditLogService.LogFieldChangeAsync(
                        taiKhoanId: userId,
                        bang: "Dự án",
                        doiTuongId: id,
                        tenDoiTuong: newData.TenDuAn,
                        truong: tenTruong,
                        giaTriCu: v1,
                        giaTriMoi: v2,
                        ghiChu: $"Cập nhật {tenTruong}"
                    );
                }
            }

            // --- Gọi hàm log cho từng trường ---
            await LogChange("Tên dự án", oldData.TenDuAn, newData.TenDuAn);
            await LogChange("Mô tả", oldData.MoTa, newData.MoTa);
            await LogChange("Ngày bắt đầu", oldData.NgayBatDau, newData.NgayBatDau);
            await LogChange("Ngày kết thúc", oldData.NgayKetThuc, newData.NgayKetThuc);
            await LogChange("Ngân sách", oldData.NganSach, newData.NganSach);
            await LogChange("Trạng thái", oldData.TrangThaiDuAn, newData.TrangThaiDuAn);

            // [REALTIME] Broadcast để các danh sách UI tự refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DU_AN",
                entityId: id,
                action: "UPDATED"
            );

            return Ok(new { message = "Cập nhật dự án thành công" });
        }

        // =====================================================================
        // 5) GỬI DUYỆT DỰ ÁN (HR gửi duyệt giám đốc)
        // =====================================================================
        [HttpPost("{id}/gui-duyet")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> GuiDuyet(int id, DuAnGuiDuyetRequestDto dto)
        {
            var userId = GetUserId();
            var ok = await _service.GuiDuyetAsync(id, dto, userId);
            if (!ok) return NotFound();

            // Lấy thông tin dự án để log
            var duAn = await _service.GetDetailAsync(id);

            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Dự án",
                doiTuongId: id,
                tenDoiTuong: duAn?.TenDuAn ?? "",
                hanhDong: "Gửi duyệt",
                ghiChu: $"Gửi duyệt dự án: {duAn?.TenDuAn}"
            );

            // [NOTIFICATION] Thông báo Giám đốc có dự án cần duyệt
            var gdAccounts = await _service.GetDirectorAccountIdsAsync();
            foreach (var gdId in gdAccounts)
            {
                await _thongBaoService.CreateAndPushAsync(
                    userId: gdId,
                    title: $"Dự án chờ duyệt",
                    message: $"Dự án: {duAn?.TenDuAn}",
                    type: "YEU_CAU_DUYET",
                    relatedEntity: "DU_AN",
                    relatedId: id,
                    link: "/gd/duyet-du-an",
                    senderId: userId
                );
            }
            // [REALTIME] Broadcast để các danh sách UI tự refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DU_AN",
                entityId: id,
                action: "GUI_DUYET",
                data: new { TrangThai = "CHO_DUYET_GIAM_DOC" }
            );
            return Ok(new { message = "Đã gửi duyệt dự án" });
        }

        // =====================================================================
        // 5.1) THU HỒI DỰ ÁN (HR thu hồi yêu cầu duyệt)
        // =====================================================================
        [HttpPost("{id}/thu-hoi")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> ThuHoi(int id)
        {
            try
            {
                var userId = GetUserId();
                var ok = await _service.ThuHoiAsync(id, userId);
                if (!ok) return NotFound();

                var duAn = await _service.GetDetailAsync(id);

                await _auditLogService.LogActionAsync(
                    taiKhoanId: userId,
                    bang: "Dự án",
                    doiTuongId: id,
                    tenDoiTuong: duAn?.TenDuAn ?? "",
                    hanhDong: "Thu hồi",
                    ghiChu: $"Thu hồi yêu cầu duyệt dự án: {duAn?.TenDuAn}"
                );
                // [REALTIME] Broadcast để các danh sách UI tự refresh
                await _thongBaoService.BroadcastEntityUpdateAsync(
                    entityType: "DU_AN",
                    entityId: id,
                    action: "THU_HOI",
                    data: new { TrangThai = "DANG_NHAP" }
                );
                return Ok(new { message = "Đã thu hồi yêu cầu duyệt thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        // =====================================================================
        // 6) GIÁM ĐỐC DUYỆT / TỪ CHỐI
        // =====================================================================
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "GIAM_DOC")]
        public async Task<IActionResult> Approve(int id, DuAnApproveRequestDto dto)
        {
            var userId = GetUserId();
            var ok = await _service.ApproveAsync(id, dto, userId);
            if (!ok) return NotFound();

            // Lấy thông tin dự án để log
            var duAn = await _service.GetDetailAsync(id);

            // Log audit
            string hanhDong = dto.DongY ? "Phê duyệt" : "Từ chối";
            string ghiChu = dto.DongY
                ? $"Phê duyệt dự án: {duAn?.TenDuAn}"
                : $"Từ chối dự án: {duAn?.TenDuAn}. Lý do: {dto.LyDoTuChoi}";

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Dự án",
                doiTuongId: id,
                tenDoiTuong: duAn?.TenDuAn ?? "",
                hanhDong: hanhDong,
                ghiChu: ghiChu
            );

            // [NOTIFICATION] Thông báo HR kết quả duyệt
            if (duAn?.TaiKhoanTaoId != null)
            {
                var notifType = dto.DongY ? "DA_DUYET" : "TU_CHOI";
                var notifTitle = dto.DongY
                    ? $"Dự án đã được duyệt"
                    : $"Dự án bị từ chối";
                var notifMsg = dto.DongY
                    ? $"Dự án {duAn.TenDuAn} đã được phê duyệt"
                    : $"Dự án {duAn.TenDuAn} bị từ chối: {dto.LyDoTuChoi}";

                await _thongBaoService.CreateAndPushAsync(
                    userId: duAn.TaiKhoanTaoId.Value,
                    title: notifTitle,
                    message: notifMsg,
                    type: notifType,
                    relatedEntity: "DU_AN",
                    relatedId: id,
                    link: "/hr/du-an",
                    senderId: userId
                );
            }

            // [REALTIME] Broadcast để các danh sách UI tự refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DU_AN",
                entityId: id,
                action: dto.DongY ? "APPROVED" : "REJECTED",
                data: new { TrangThai = dto.DongY ? "DA_DUYET" : "TU_CHOI" }
            );

            return Ok(new { message = "Xử lý duyệt dự án thành công" });
        }

        // =====================================================================
        // 7) GIÁM ĐỐC: LẤY DANH SÁCH DỰ ÁN TÔI ĐÃ DUYỆT
        // =====================================================================
        [HttpGet("my-approved")]
        [Authorize(Roles = "GIAM_DOC")]
        public async Task<IActionResult> MyApproved()
        {
            var userId = GetUserId();
            var data = await _service.GetMyApprovedListAsync(userId);
            return Ok(data);
        }
        /// <summary>
        /// Thêm thành viên
        /// </summary>
        /// <param name="duAnId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("{duAnId}/members")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> AddMember(int duAnId, DuAnAddMemberDto dto)
        {
            var userId = GetUserId();
            var ok = await _service.AddMemberAsync(duAnId, dto);
            if (!ok) return NotFound();

            // Lấy thông tin dự án để log
            var duAn = await _service.GetDetailAsync(duAnId);

            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Dự án - Thành viên",
                doiTuongId: duAnId,
                tenDoiTuong: duAn?.TenDuAn ?? "",
                hanhDong: "Thêm thành viên",
                ghiChu: $"Thêm thành viên vào dự án: {duAn?.TenDuAn}. Vai trò: {dto.VaiTroTrongDuAn}"
            );

            // [REALTIME] Broadcast
            await _thongBaoService.BroadcastEntityUpdateAsync("DU_AN", duAnId, "MEMBER_ADDED");

            return Ok(new { message = "Đã thêm thành viên vào dự án" });
        }

        /// <summary>
        /// Cập nhật vai trò thành viên
        /// </summary>
        /// <param name="duAnId"></param>
        /// <param name="nvId"></param>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPut("{duAnId}/members/{nvId}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> UpdateRole(int duAnId, int nvId, DuAnUpdateMemberRoleDto dto)
        {
            var userId = GetUserId();

            // Lấy thông tin cũ trước khi update
            var duAn = await _service.GetDetailAsync(duAnId);
            var oldMember = duAn?.ThanhViens.FirstOrDefault(x => x.NvHoSoId == nvId);
            string oldRole = oldMember?.VaiTroTrongDuAn ?? "";

            var ok = await _service.UpdateMemberRoleAsync(duAnId, nvId, dto.VaiTroTrongDuAn);
            if (!ok) return NotFound();

            // Log audit nếu vai trò thay đổi
            if (oldRole != dto.VaiTroTrongDuAn)
            {
                await _auditLogService.LogFieldChangeAsync(
                    taiKhoanId: userId,
                    bang: "Dự án - Thành viên",
                    doiTuongId: duAnId,
                    tenDoiTuong: duAn?.TenDuAn ?? "",
                    truong: "Vai trò thành viên",
                    giaTriCu: oldRole,
                    giaTriMoi: dto.VaiTroTrongDuAn,
                    ghiChu: $"Cập nhật vai trò thành viên trong dự án: {duAn?.TenDuAn}"
                );
            }

            return Ok(new { message = "Đã cập nhật vai trò thành viên" });
        }

        /// <summary>
        /// Cập nhật vai trò thành viên
        /// </summary>
        /// <param name="duAnId"></param>
        /// <param name="nvId"></param>
        /// <returns></returns>
        [HttpDelete("{duAnId}/members/{nvId}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> RemoveMember(int duAnId, int nvId)
        {
            var userId = GetUserId();

            // Lấy thông tin trước khi xóa
            var duAn = await _service.GetDetailAsync(duAnId);
            var member = duAn?.ThanhViens.FirstOrDefault(x => x.NvHoSoId == nvId);

            var ok = await _service.RemoveMemberAsync(duAnId, nvId);
            if (!ok) return NotFound();

            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Dự án - Thành viên",
                doiTuongId: duAnId,
                tenDoiTuong: duAn?.TenDuAn ?? "",
                hanhDong: "Xóa thành viên",
                ghiChu: $"Xóa thành viên khỏi dự án: {duAn?.TenDuAn}. Vai trò cũ: {member?.VaiTroTrongDuAn}"
            );
            // [REALTIME] Broadcast
            await _thongBaoService.BroadcastEntityUpdateAsync("DU_AN", duAnId, "MEMBER_REMOVED");
            return Ok(new { message = "Đã xóa thành viên khỏi dự án" });
        }
        // =====================================================================
        // 1.1) LẤY DANH SÁCH DỰ ÁN CỦA TÔI (EMPLOYEE)
        // =====================================================================
        [HttpGet("my")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC")]
        public async Task<IActionResult> GetMyProjects()
        {
            var userId = GetUserId();
            var list = await _service.GetMyListAsync(userId);
            return Ok(list);
        }
        // ADD
        [HttpPost("{id}/attachment")]
        [Authorize(Roles = "HR_ACC")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment(int id, [FromForm] UploadFileFormDto form)
        {
            var file = form.File; // CHANGE: lấy file từ DTO

            if (file == null || file.Length <= 0)
                return BadRequest(new { message = "File không hợp lệ" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Chỉ cho phép .pdf/.doc/.docx" });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "Dung lượng file tối đa 10MB" });

            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(root, "uploads", "duan", id.ToString());
            Directory.CreateDirectory(folder);

            var safeName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/duan/{id}/{safeName}";

            var ok = await _service.UpdateAttachmentAsync(
                id,
                publicUrl,
                file.FileName,
                file.ContentType ?? "",
                file.Length
            );

            if (!ok) return NotFound();
            return Ok(new { message = "Upload file đính kèm thành công", url = publicUrl });
        }

        // =====================================================================
        // [NEW] TẠO TASK TRONG DỰ ÁN (Trưởng phòng / HR)
        // =====================================================================
        [HttpPost("{duAnId}/tasks")]
        [Authorize(Roles = "TRUONG_PHONG,HR_ACC,ADMIN")]
        public async Task<IActionResult> CreateTask(int duAnId, [FromBody] QLNS_BE.Models.Dtos.Task.TaskCreateDto dto, [FromServices] TaskService taskService)
        {
            // Lấy employeeId từ token (người giao việc)
            var employeeIdClaim = User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeIdClaim))
                return BadRequest(new { message = "Không tìm thấy thông tin nhân viên" });

            int nguoiGiaoId = int.Parse(employeeIdClaim);
            var task = await taskService.CreateTaskAsync(duAnId, dto, nguoiGiaoId);

            // Log audit
            var userId = GetUserId();
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Task",
                doiTuongId: task.Id,
                tenDoiTuong: task.TieuDe,
                hanhDong: "Tạo task",
                ghiChu: $"Tạo task mới trong dự án: {task.TieuDe}"
            );

            return Ok(new { id = task.Id, message = "Tạo task thành công" });
        }

        // =====================================================================
        // [NEW] XEM DANH SÁCH TASK TRONG DỰ ÁN
        // =====================================================================
        [HttpGet("{duAnId}/tasks")]
        [Authorize(Roles = "TRUONG_PHONG,HR_ACC,ADMIN")]
        public async Task<IActionResult> GetProjectTasks(int duAnId, [FromServices] TaskService taskService)
        {
            var tasks = await taskService.GetTasksByProjectAsync(duAnId);
            return Ok(new { tasks });
        }

        // =====================================================================
        // MULTI-FILE MANAGEMENT
        // =====================================================================

        /// <summary>
        /// Upload nhiều file cho dự án
        /// </summary>
        [HttpPost("{id}/files")]
        [Authorize(Roles = "HR_ACC")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFiles(int id, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { message = "Không có file nào được chọn" });

            var userId = GetUserId();
            var uploadedFiles = new List<object>();

            foreach (var file in files)
            {
                // Validate file
                if (file.Length > 10 * 1024 * 1024) // 10MB max
                    return BadRequest(new { message = $"File {file.FileName} vượt quá 10MB" });

                // Generate unique filename
                var ext = Path.GetExtension(file.FileName);
                var uniqueName = $"{Guid.NewGuid()}{ext}";
                var uploadPath = Path.Combine("wwwroot", "uploads", "duan", id.ToString());

                // Ensure directory exists
                Directory.CreateDirectory(uploadPath);

                var filePath = Path.Combine(uploadPath, uniqueName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save to database
                var relativePath = $"/uploads/duan/{id}/{uniqueName}";
                var savedFile = await _service.AddFileAsync(
                    id,
                    file.FileName,
                    relativePath,
                    file.Length,
                    file.ContentType,
                    userId
                );

                if (savedFile != null)
                {
                    uploadedFiles.Add(new { savedFile.Id, savedFile.TenFile, savedFile.DuongDanFile });
                }
            }

            return Ok(new { message = $"Đã upload {uploadedFiles.Count} file", files = uploadedFiles });
        }

        /// <summary>
        /// Lấy danh sách file của dự án
        /// </summary>
        [HttpGet("{id}/files")]
        public async Task<IActionResult> GetFiles(int id)
        {
            var files = await _service.GetFilesAsync(id);
            return Ok(files);
        }

        /// <summary>
        /// Xóa file khỏi dự án
        /// </summary>
        [HttpDelete("{id}/files/{fileId}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> DeleteFile(int id, int fileId)
        {
            var userId = GetUserId();
            var result = await _service.DeleteFileAsync(fileId, userId);

            if (!result)
                return NotFound(new { message = "Không tìm thấy file" });

            return Ok(new { message = "Đã xóa file" });
        }

    }
}
