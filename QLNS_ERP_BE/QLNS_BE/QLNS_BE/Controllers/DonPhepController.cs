using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.DonPhep;
using QLNS_BE.Services;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DonPhepController :ControllerBase
    {
        private readonly DonPhepService _service;
        private readonly AuditLogService _auditLogService;
        private readonly ThongBaoService _thongBaoService;

        public DonPhepController(
            DonPhepService service, 
            AuditLogService auditLogService,
            ThongBaoService thongBaoService)
        {
            _service = service;
            _auditLogService = auditLogService;
            _thongBaoService = thongBaoService;
        }
        
        private int GetUserId() => int.Parse(User.Claims.First(x => x.Type == "userid").Value);
        // ============================================================
        // DS ĐƠN PHÉP
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var data = await _service.GetListAsync();
            return Ok(data);
        }

        // ============================================================
        // DS LOẠI PHÉP
        // ============================================================
        [HttpGet("loai-phep")]
        public async Task<IActionResult> GetLoaiPheps()
        {
            var data = await _service.GetLoaiPhepsAsync();
            return Ok(data);
        }

        // ============================================================
        // CHI TIẾT
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _service.GetDetailAsync(id);
            if (data == null) return NotFound();
            return Ok(data);
        }

        // ============================================================
        // TẠO MỚI
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DonPhepCreateDto dto)
        {
            var userId = GetUserId();
            var id = await _service.CreateAsync(dto);
            
            // Lấy thông tin đơn phép để log
            var donPhep = await _service.GetDetailAsync(id);
            
            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đơn phép",
                doiTuongId: id,
                tenDoiTuong: $"{donPhep?.HoTen} - {donPhep?.TenLoaiPhep}",
                hanhDong: "Tạo đơn",
                ghiChu: $"Tạo đơn phép: {donPhep?.TenLoaiPhep} từ {dto.TuNgay:dd/MM/yyyy} đến {dto.DenNgay:dd/MM/yyyy}"
            );
            
            // [NOTIFICATION] Thông báo HR có đơn mới cần duyệt
            // Lấy tất cả HR accounts
            var hrAccounts = await _service.GetHrAccountIdsAsync();
            foreach (var hrId in hrAccounts)
            {
                await _thongBaoService.CreateAndPushAsync(
                    userId: hrId,
                    title: $"Đơn phép mới: {donPhep?.HoTen}",
                    message: $"{donPhep?.TenLoaiPhep} từ {dto.TuNgay:dd/MM} - {dto.DenNgay:dd/MM}",
                    type: "YEU_CAU_DUYET",
                    relatedEntity: "DON_PHEP",
                    relatedId: id,
                    link: $"/hr/don-phep",
                    senderId: userId
                );
            }
            
            return CreatedAtAction(nameof(GetDetail), new { id }, null);
        }

        // ============================================================
        // DUYỆT / TỪ CHỐI
        // ============================================================
        [HttpPut("duyet")]
        [Authorize(Roles = "HR_ACC,GIAMDOC")]
        public async Task<IActionResult> DuyetDon([FromBody] DuyetDonPhepRequestDto dto)
        {
            var userId = GetUserId();
            
            // Lấy thông tin đơn phép trước khi duyệt
            var donPhep = await _service.GetDetailAsync(dto.DonPhepId);
            
            await _service.DuyetDonAsync(dto, userId);
            
            // Log audit
            string hanhDong = dto.ChapNhan ? "Phê duyệt" : "Từ chối";
            string ghiChu = dto.ChapNhan 
                ? $"Phê duyệt đơn phép: {donPhep?.TenLoaiPhep} của {donPhep?.HoTen}" 
                : $"Từ chối đơn phép: {donPhep?.TenLoaiPhep} của {donPhep?.HoTen}. Lý do: {dto.LyDoTuChoi}";
            
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đơn phép",
                doiTuongId: dto.DonPhepId,
                tenDoiTuong: $"{donPhep?.HoTen} - {donPhep?.TenLoaiPhep}",
                hanhDong: hanhDong,
                ghiChu: ghiChu
            );
            
            // [NOTIFICATION] Thông báo nhân viên kết quả duyệt
            if (donPhep?.TaiKhoanId != null)
            {
                var notifType = dto.ChapNhan ? "DA_DUYET" : "TU_CHOI";
                var notifTitle = dto.ChapNhan 
                    ? $"Đơn phép đã được duyệt" 
                    : $"Đơn phép bị từ chối";
                var notifMsg = dto.ChapNhan 
                    ? $"Đơn {donPhep.TenLoaiPhep} đã được phê duyệt"
                    : $"Đơn {donPhep.TenLoaiPhep} bị từ chối: {dto.LyDoTuChoi}";
                
                await _thongBaoService.CreateAndPushAsync(
                    userId: donPhep.TaiKhoanId.Value,
                    title: notifTitle,
                    message: notifMsg,
                    type: notifType,
                    relatedEntity: "DON_PHEP",
                    relatedId: dto.DonPhepId,
                    link: "/employee/nghi-phep",
                    senderId: userId
                );
            }
            
            // [REALTIME] Broadcast để các danh sách UI tự refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DON_PHEP",
                entityId: dto.DonPhepId,
                action: dto.ChapNhan ? "APPROVED" : "REJECTED",
                data: new { TrangThai = dto.ChapNhan ? "DA_DUYET" : "TU_CHOI" }
            );
            
            return NoContent();
        }
        // CHANGE (chỉ trong phần ADD trước đó)
        [HttpPut("{id}/employee")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> UpdateByEmployee(int id, [FromBody] DonPhepEmployeeUpdateDto dto)
        {
            var userId = GetUserId();
            
            // ADD - nếu token có claim nvhosoId thì ép trùng
            var claimNv = User.Claims.FirstOrDefault(x => x.Type == "nvhosoId" || x.Type == "nvHoSoId");
            if (claimNv != null && int.TryParse(claimNv.Value, out var nvFromToken))
            {
                if (dto.NvHoSoId != nvFromToken)
                    throw new Exception("NvHoSoId không khớp với tài khoản đăng nhập.");
            }
            
            // 1. Lấy dữ liệu cũ
            var oldData = await _service.GetDetailAsync(id);
            if (oldData == null) return NotFound();

            // 2. Thực hiện update
            await _service.UpdateByEmployeeAsync(id, dto);
            
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
                        bang: "Đơn phép",
                        doiTuongId: id,
                        tenDoiTuong: $"{newData.HoTen} - {newData.TenLoaiPhep}",
                        truong: tenTruong,
                        giaTriCu: v1,
                        giaTriMoi: v2,
                        ghiChu: $"Cập nhật {tenTruong}"
                    );
                }
            }
            
            // --- Gọi hàm log cho từng trường ---
            await LogChange("Loại phép", oldData.TenLoaiPhep, newData.TenLoaiPhep);
            await LogChange("Từ ngày", oldData.TuNgay, newData.TuNgay);
            await LogChange("Đến ngày", oldData.DenNgay, newData.DenNgay);
            await LogChange("Lý do", oldData.LyDo, newData.LyDo);
            
            return NoContent();
        }

        // CHANGE (chỉ trong phần ADD trước đó)
        [HttpDelete("{id}/employee")]
        [Authorize(Roles = "EMPLOYEE")]
        public async Task<IActionResult> DeleteByEmployee(int id, [FromQuery] int nvHoSoId)
        {
            var userId = GetUserId();
            
            // ADD - nếu token có claim nvhosoId thì ép trùng
            var claimNv = User.Claims.FirstOrDefault(x => x.Type == "nvhosoId" || x.Type == "nvHoSoId");
            if (claimNv != null && int.TryParse(claimNv.Value, out var nvFromToken))
            {
                if (nvHoSoId != nvFromToken)
                    throw new Exception("NvHoSoId không khớp với tài khoản đăng nhập.");
            }
            
            // Lấy thông tin trước khi xóa
            var donPhep = await _service.GetDetailAsync(id);

            await _service.DeleteByEmployeeAsync(id, nvHoSoId);
            
            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đơn phép",
                doiTuongId: id,
                tenDoiTuong: $"{donPhep?.HoTen} - {donPhep?.TenLoaiPhep}",
                hanhDong: "Xóa đơn",
                ghiChu: $"Xóa đơn phép: {donPhep?.TenLoaiPhep} từ {donPhep?.TuNgay:dd/MM/yyyy} đến {donPhep?.DenNgay:dd/MM/yyyy}"
            );
            
            return NoContent();
        }

    }
}
