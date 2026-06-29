using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Common;
using QLNS_BE.Models.Dtos.NhanVien;
using QLNS_BE.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IO;
namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "HR_ACC,GIAM_DOC,EMPLOYEE")]
    public class NhanVienController: ControllerBase
    {
        private readonly NhanVienService _nhanVienService;
        private readonly AuditLogService _auditLogService;

        public NhanVienController(NhanVienService nhanVienService, AuditLogService auditLogService)
        {
            _nhanVienService = nhanVienService;
            _auditLogService = auditLogService;
        }
        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);
        private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? "";
        // ==========================================
        // [NEW] Endpoint lấy hồ sơ cá nhân (Realtime từ Token)
        // ==========================================
        /// <summary>
        /// [EMPLOYEE/ALL] Lấy hồ sơ của chính mình dựa trên Token.
        /// GET api/nhanvien/me/profile
        /// </summary>
        [HttpGet("me/profile")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC,EMPLOYEE")]
        public async Task<IActionResult> GetMyProfile()
        {
            // 1. Lấy EmployeeId từ Token (đã được add ở bước sửa JwtTokenService)
            var empIdClaim = User.FindFirst("EmployeeId");

            if (empIdClaim == null || !int.TryParse(empIdClaim.Value, out int employeeId))
            {
                return BadRequest(new { message = "Không tìm thấy liên kết nhân viên trong tài khoản này." });
            }

            // 2. Tái sử dụng Service lấy chi tiết (không cần viết lại query)
            var result = await _nhanVienService.GetHoSoCaNhanAsync(employeeId);

            if (result == null)
                return NotFound(new { message = "Hồ sơ nhân viên không tồn tại." });

            return Ok(result);
        }
        /// <summary>
        /// Danh sách nhân viên có phân trang.
        /// GET api/nhanvien?pageIndex=1&pageSize=20&keyword=abc
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPaged([FromQuery] PagingRequestDto request)
        {
            var result = await _nhanVienService.GetPagedAsync(request);
            return Ok(result);
        }

        /// <summary>
        /// Chi tiết 1 nhân viên.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _nhanVienService.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        /// <summary>
        /// Tạo mới nhân viên (hồ sơ + công việc hiện tại).
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] NhanVienCreateDto dto)
        {
            var userId = GetUserId();
            var result = await _nhanVienService.CreateAsync(dto);
            
            //return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang:"Hồ sơ nhân viên",
                doiTuongId: result.Id,
                tenDoiTuong: result.HoTen,
                hanhDong:"Thêm mới",
                ghiChu: $"Thêm mới nhân viên:{ result.HoTen}"
                );
            return Ok(result);
        }

        /// <summary>
        /// Cập nhật hồ sơ + công việc hiện tại.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] NhanVienUpdateDto dto)
        {
            var userId = GetUserId();

            // 1. Lấy dữ liệu cũ
            var oldData = await _nhanVienService.GetByIdAsync(id);
            if (oldData == null) return NotFound();

            // 2. Thực hiện update
            var result = await _nhanVienService.UpdateAsync(id, dto);

            // 3. LOGGING (Sử dụng Helper Function)
            // Hàm này sẽ tự so sánh, nếu khác nhau mới ghi log
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
                        bang: "Hồ sơ nhân viên",
                        doiTuongId: result.Id,
                        tenDoiTuong: result.HoTen, // Luôn lấy tên mới nhất
                        truong: tenTruong,
                        giaTriCu: v1,
                        giaTriMoi: v2,
                        ghiChu: $"Cập nhật {tenTruong}"
                    );
                }
            }

            // --- Gọi hàm log cho từng trường ---

            // Thông tin cá nhân
            await LogChange("Họ tên", oldData.HoTen, result.HoTen);
            await LogChange("Ngày sinh", oldData.NgaySinh, result.NgaySinh);
            await LogChange("Giới tính", oldData.GioiTinh == 1 ? "Nam" : "Nữ", result.GioiTinh == 1 ? "Nam" : "Nữ");
            await LogChange("Địa chỉ", oldData.DiaChi, result.DiaChi);
            await LogChange("Số điện thoại", oldData.SoDienThoai, result.SoDienThoai);
            await LogChange("Email", oldData.EmailCaNhan, result.EmailCaNhan);

            // Thông tin công việc (Giả sử result có trả về object CongViecHienTai chứa tên)
            // Nếu result chỉ trả về ID, bạn có thể log ID hoặc cần lookup tên để log đẹp hơn
            if (oldData.NvCongViecId != null && result.NvCongViecId != null)
            {
                await LogChange("Phòng ban", oldData.TenPhongBan, result.TenPhongBan);
                await LogChange("Chức vụ", oldData.TenChucVu, result.TenChucVu);
                await LogChange("Loại hợp đồng", oldData.LoaiHopDong, result.LoaiHopDong);
                await LogChange("Ngày vào làm", oldData.NgayVaoLam, result.NgayVaoLam);
                await LogChange("Trạng thái làm việc",
                    oldData.TrangThaiLamViec == 1 ? "Đang làm" : "Nghỉ việc",
                    result.TrangThaiLamViec == 1 ? "Đang làm" : "Nghỉ việc");
            }

            return Ok(result);
        }

        /// <summary>
        /// Cho nhân viên nghỉ việc (soft-delete logic).
        /// </summary>
        [HttpPut("{id:int}/nghi-viec")]
        public async Task<IActionResult> MarkAsResigned(int id, [FromQuery] DateTime? ngayNghiViec)
        {
            await _nhanVienService.MarkAsResignedAsync(id, ngayNghiViec);
            return NoContent();
        }

        /// <summary>
        /// Xóa cứng nhân viên
        /// </summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var nhanVien = await _nhanVienService.GetByIdAsync(id);
            await _nhanVienService.DeleteAsync(id);
            //log
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang:"Hồ sơ nhân viên",
                doiTuongId:id,
                tenDoiTuong:nhanVien.HoTen,
                hanhDong:"Xóa",
                ghiChu:$"Xóa nhân viên: {nhanVien.MaNhanVien}"
                );

            return NoContent();
        }
        //// ============================================================
        //// 1) LẤY HỒ SƠ CỦA TÔI
        //// GET: /api/Me/ho-so-ca-nhan
        //// ============================================================
        //[HttpGet("ho-so-ca-nhan")]
        //public async Task<IActionResult> GetMyProfile()
        //{
        //    var userId = GetUserId();
        //    var data = await _nhanVienService.GetHoSoCaNhanAsync(userId);
        //    if (data == null) return NotFound();

        //    return Ok(data);
        //}

        // ============================================================
        // 2) LẤY HỒ SƠ THEO NV_HO_SO_ID (HR/GĐ xem được tất cả)
        // GET: /api/Me/ho-so-ca-nhan/{nvHoSoId}
        // ============================================================
        [HttpGet("ho-so-ca-nhan/{nvHoSoId:int}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC,EMPLOYEE")]
        public async Task<IActionResult> GetProfileById(int nvHoSoId)
        {
            var role = GetRole();
            var userId = GetUserId();

            //if (role == "EMPLOYEE" && userId != nvHoSoId)
            //    return Forbid();

            var data = await _nhanVienService.GetHoSoCaNhanAsync(nvHoSoId);
            if (data == null) return NotFound();

            return Ok(data);
        }
        [HttpPut("ho-so-ca-nhan/so-tai-khoan")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> UpdateMyBankAccount([FromBody] UpdateMyBankAccountDto dto)
        {
            var userId = GetUserId();
            var ok = await _nhanVienService.UpdateMyBankAccountAsync(userId, dto.SoTaiKhoanNganHang);
            if (!ok) return NotFound();
            return Ok(new { message = "Cập nhật số tài khoản thành công" });
        }
        // ADD
        [HttpPost("ho-so-ca-nhan/avatar")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> UploadMyAvatar([FromForm] UploadFileFormDto form)
        {
            var file = form.File;
            if (file == null || file.Length <= 0)
                return BadRequest(new { message = "File không hợp lệ" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Chỉ cho phép ảnh .jpg/.jpeg/.png" });

            if (file.Length > 2 * 1024 * 1024)
                return BadRequest(new { message = "Dung lượng ảnh tối đa 2MB" });

            var userId = GetUserId();



            // lưu: wwwroot/uploads/avatars/{userid}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(root, "uploads", "avatars", userId.ToString());
            Directory.CreateDirectory(folder);

            var safeName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }



            var publicUrl = $"/uploads/avatars/{userId}/{safeName}";
            var ok = await _nhanVienService.UpdateMyAvatarUrlAsync(userId, publicUrl);
            if (!ok) return NotFound();

            return Ok(new { message = "Upload avatar thành công", url = publicUrl });
        }

        // =====================================================================
        // [NEW] CẬP NHẬT ẢNH URL TỪ WEB - Nhân viên nhập URL trực tiếp
        // =====================================================================
        [HttpPut("ho-so-ca-nhan/avatar-url")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> UpdateMyAvatarUrl([FromBody] UpdateAvatarUrlDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.AvatarUrl))
                return BadRequest(new { message = "URL ảnh không được để trống" });

            // Validate URL format
            if (!Uri.TryCreate(dto.AvatarUrl, UriKind.Absolute, out _))
                return BadRequest(new { message = "URL ảnh không hợp lệ" });

            var employeeIdClaim = User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeIdClaim))
                return BadRequest(new { message = "Không tìm thấy thông tin nhân viên" });

            int employeeId = int.Parse(employeeIdClaim);

            var ok = await _nhanVienService.UpdateMyAvatarUrlAsync(employeeId, dto.AvatarUrl);
            if (!ok) return NotFound(new { message = "Không tìm thấy nhân viên" });

            // Log audit
            var userId = GetUserId();
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Hồ sơ nhân viên",
                doiTuongId: employeeId,
                tenDoiTuong: "",
                hanhDong: "Cập nhật ảnh đại diện",
                ghiChu: "Nhân viên cập nhật URL ảnh đại diện từ web"
            );

            return Ok(new { message = "Cập nhật ảnh đại diện thành công", avatarUrl = dto.AvatarUrl });
        }

        // =====================================================================
        // [NEW] UPLOAD ẢNH STK - Nhân viên upload
        // =====================================================================
        [HttpPost("ho-so-ca-nhan/upload-anh-stk")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadAnhStk([FromForm] UploadFileFormDto form)
        {
            var file = form.File;
            if (file == null || file.Length <= 0)
                return BadRequest(new { message = "File không hợp lệ" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Chỉ cho phép ảnh (.jpg, .jpeg, .png) hoặc .pdf" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { message = "Dung lượng file tối đa 5MB" });

            var employeeIdClaim = User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeIdClaim))
                return BadRequest(new { message = "Không tìm thấy thông tin nhân viên" });

            int employeeId = int.Parse(employeeIdClaim);

            // Lưu: wwwroot/uploads/stk/{employeeId}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(root, "uploads", "stk", employeeId.ToString());
            Directory.CreateDirectory(folder);

            var safeName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/stk/{employeeId}/{safeName}";
            var ok = await _nhanVienService.UpdateAnhStkAsync(employeeId, publicUrl);
            if (!ok) return NotFound();

            // Log audit
            var userId = GetUserId();
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Hồ sơ nhân viên",
                doiTuongId: employeeId,
                tenDoiTuong: "",
                hanhDong: "Upload ảnh STK",
                ghiChu: "Nhân viên upload ảnh sao kê tài khoản"
            );

            return Ok(new { anhStkUrl = publicUrl, message = "Upload thành công" });
        }

        // =====================================================================
        // [NEW] UPLOAD HỢP ĐỒNG LAO ĐỘNG - HR upload khi tạo NV
        // POST api/nhanvien/{id}/hop-dong
        // =====================================================================
        [HttpPost("{id:int}/hop-dong")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC,HR_KETOAN")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> UploadHopDong(int id, [FromForm] UploadFileFormDto form)
        {
            var file = form.File;
            if (file == null || file.Length <= 0)
                return BadRequest(new { message = "File không hợp lệ" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = new[] { ".pdf", ".doc", ".docx" };
            if (!allowed.Contains(ext))
                return BadRequest(new { message = "Chỉ cho phép file PDF/Word (.pdf, .doc, .docx)" });

            if (file.Length > 10 * 1024 * 1024)
                return BadRequest(new { message = "Dung lượng file tối đa 10MB" });

            var nhanVien = await _nhanVienService.GetByIdAsync(id);
            if (nhanVien == null) return NotFound(new { message = "Không tìm thấy nhân viên" });

            // Lưu: wwwroot/uploads/hop-dong/{id}/
            var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder = Path.Combine(root, "uploads", "hop-dong", id.ToString());
            Directory.CreateDirectory(folder);

            var safeName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, safeName);

            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                await file.CopyToAsync(stream);
            }

            var publicUrl = $"/uploads/hop-dong/{id}/{safeName}";
            var ok = await _nhanVienService.UpdateHopDongAsync(id, publicUrl);
            if (!ok) return NotFound(new { message = "Không tìm thấy nhân viên" });

            // Audit log
            _ = _auditLogService.LogActionAsync(
                taiKhoanId: GetUserId(),
                bang: "Hồ sơ nhân viên",
                doiTuongId: id,
                tenDoiTuong: nhanVien.HoTen,
                hanhDong: "Upload hợp đồng",
                ghiChu: "HR upload file hợp đồng lao động"
            );

            return Ok(new { hopDongUrl = publicUrl, message = "Upload hợp đồng thành công" });
        }

        // =====================================================================
        // [NEW] XEM ẢNH STK - HR xem
        // =====================================================================
        [HttpGet("{id}/anh-stk")]
        [Authorize(Roles = "HR_ACC,ADMIN")]
        public async Task<IActionResult> GetAnhStk(int id)
        {
            var nhanVien = await _nhanVienService.GetByIdAsync(id);
            if (nhanVien == null)
                return NotFound(new { message = "Không tìm thấy nhân viên" });

            return Ok(new
            {
                anhStkUrl = nhanVien.AnhStkUrl,
                soTaiKhoan = nhanVien.SoTaiKhoanNganHang,
                hoTen = nhanVien.HoTen
            });
        }
    }
}
