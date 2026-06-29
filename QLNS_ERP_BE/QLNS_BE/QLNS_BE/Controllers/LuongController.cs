using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LuongController : ControllerBase
    {
        private readonly LuongService _service;
        private readonly AuditLogService _auditLogService;
        private readonly ThongBaoService _thongBaoService;

        public LuongController(
            LuongService service,
            AuditLogService auditLogService,
            ThongBaoService thongBaoService)
        {
            _service = service;
            _auditLogService = auditLogService;
            _thongBaoService = thongBaoService;
        }

        // ============================================================
        // 0) HR & GIÁM ĐỐC – DS BẢNG LƯƠNG
        // GET api/luong
        // ============================================================
        [HttpGet]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetList()
        {
            var data = await _service.GetListAsync();
            return Ok(data);
        }
        [HttpGet("tong-luong-thang")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetTongLuongThang([FromQuery] int thang, [FromQuery] int nam)
        {
            var data = await _service.GetTongLuongTheoThangAsync(thang, nam);
            return Ok(data);
        }
        [HttpGet("thong-ke-trang-thai")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetThongKeTrangThai([FromQuery] int thang, [FromQuery] int nam)
        {
            var data = await _service.GetThongKeTrangThaiAsync(thang, nam);
            return Ok(data);
        }
        // ============================================================
        // 1) EMPLOYEE – XEM LƯƠNG CỦA TÔI
        // GET api/luong/me
        // ============================================================
        [HttpGet("me")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")]  // mở rộng cho tất cả roles
        public async Task<IActionResult> GetMySalary()
        {
            // Dùng EmployeeId claim (= NvHoSoId) — đúng hơn TaiKhoanId
            var empClaim = User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(empClaim))
                return Forbid();

            int nvHoSoId = int.Parse(empClaim);
            var data = await _service.GetLuongCuaToiAsync(nvHoSoId);
            return Ok(data);
        }

        // ============================================================
        // 2) HR – TÍNH LƯƠNG CHO 1 NV
        // POST api/luong/tinh
        // ============================================================
        [HttpPost("tinh")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> TinhLuong([FromBody] TinhLuongRequestDto dto)
        {
            try
            {
                int taiKhoanTinhId = int.Parse(User.FindFirstValue("userid")!);

                var result = await _service.TinhLuongAsync(dto, taiKhoanTinhId);

                // Log audit
                if (result != null)
                {
                    await _auditLogService.LogActionAsync(
                        taiKhoanId: taiKhoanTinhId,
                        bang: "Bảng lương tháng",
                        doiTuongId: result.Id,
                        tenDoiTuong: $"Lương T{dto.Thang}/{dto.Nam} - {result.NvHoSo ?? "N/A"}",
                        hanhDong: "Tính lương",
                        ghiChu: $"HR thực hiện tính lương tháng {dto.Thang}/{dto.Nam} cho nhân viên {result.NvHoSo}"
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ============================================================
        // 3) HR – GỬI GIÁM ĐỐC DUYỆT
        // POST api/luong/{id}/gui-duyet
        // ============================================================
        [HttpPost("{id}/gui-duyet")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> GuiDuyet(int id, [FromBody] GuiDuyetLuongRequestDto dto)
        {
            int userId = int.Parse(User.FindFirstValue("userid")!);

            // Lấy thông tin bảng lương trước khi gửi duyệt
            var bangLuong = await _service.GetDetailAsync(id);

            var ok = await _service.GuiDuyetLuongAsync(id, dto, userId);
            if (!ok) return NotFound();

            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Bảng lương tháng",
                doiTuongId: id,
                tenDoiTuong: $"Lương T{bangLuong?.Thang}/{bangLuong?.Nam} - {bangLuong?.HoTen}",
                hanhDong: "Gửi duyệt",
                ghiChu: $"Gửi giám đốc duyệt bảng lương tháng {bangLuong?.Thang}/{bangLuong?.Nam} của {bangLuong?.HoTen}"
            );

            // [NOTIFICATION] Thông báo Giám đốc có bảng lương cần duyệt
            var gdAccounts = await _service.GetDirectorAccountIdsAsync();
            foreach (var gdId in gdAccounts)
            {
                await _thongBaoService.CreateAndPushAsync(
                    userId: gdId,
                    title: $"Bảng lương chờ duyệt",
                    message: $"Lương T{bangLuong?.Thang}/{bangLuong?.Nam} - {bangLuong?.HoTen}",
                    type: "YEU_CAU_DUYET",
                    relatedEntity: "BANG_LUONG",
                    relatedId: id,
                    link: "/gd/duyet-bang-luong",
                    senderId: userId
                );
            }

            return Ok(new { message = "Đã gửi giám đốc duyệt bảng lương" });
        }

        // ============================================================
        // 3.5) HR – THU HỒI LƯƠNG ĐÃ GỬI DUYỆT
        // POST api/luong/{id}/thu-hoi
        // ============================================================
        [HttpPost("{id}/thu-hoi")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> ThuHoi(int id)
        {
            int userId = int.Parse(User.FindFirstValue("userid")!);

            // Lấy thông tin bảng lương trước khi thu hồi
            var bangLuong = await _service.GetDetailAsync(id);

            var ok = await _service.ThuHoiLuongAsync(id, userId);
            if (!ok) return NotFound();

            // Log audit
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Bảng lương tháng",
                doiTuongId: id,
                tenDoiTuong: $"Lương T{bangLuong?.Thang}/{bangLuong?.Nam} - {bangLuong?.HoTen}",
                hanhDong: "Thu hồi",
                ghiChu: $"Thu hồi bảng lương tháng {bangLuong?.Thang}/{bangLuong?.Nam} của {bangLuong?.HoTen} về trạng thái tạm tính"
            );

            return Ok(new { message = "Đã thu hồi bảng lương về trạng thái tạm tính" });
        }

        // ============================================================
        // 4) GIÁM ĐỐC – DUYỆT / TỪ CHỐI
        // POST api/luong/{id}/duyet
        // ============================================================
        [HttpPost("{id}/duyet")]
        [Authorize(Roles = "GIAM_DOC")]
        public async Task<IActionResult> DuyetLuong(int id, [FromBody] DuyetLuongRequestDto dto)
        {
            int userId = int.Parse(User.FindFirstValue("userid")!);

            // Lấy thông tin bảng lương trước khi duyệt
            var bangLuong = await _service.GetDetailAsync(id);

            var ok = await _service.DuyetLuongAsync(id, dto, userId);
            if (!ok) return NotFound();

            // Log audit
            string hanhDong = dto.DongY ? "Phê duyệt" : "Từ chối";
            string ghiChu = dto.DongY
                ? $"Phê duyệt bảng lương tháng {bangLuong?.Thang}/{bangLuong?.Nam} của {bangLuong?.HoTen}"
                : $"Từ chối bảng lương tháng {bangLuong?.Thang}/{bangLuong?.Nam} của {bangLuong?.HoTen}. Lý do: {dto.LyDoTuChoi}";

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Bảng lương tháng",
                doiTuongId: id,
                tenDoiTuong: $"Lương T{bangLuong?.Thang}/{bangLuong?.Nam} - {bangLuong?.HoTen}",
                hanhDong: hanhDong,
                ghiChu: ghiChu
            );

            // [NOTIFICATION] Thông báo HR kết quả duyệt
            if (bangLuong?.TaiKhoanGuiDuyetId != null)
            {
                var notifType = dto.DongY ? "DA_DUYET" : "TU_CHOI";
                var notifTitle = dto.DongY
                    ? $"Bảng lương đã duyệt"
                    : $"Bảng lương bị từ chối";
                var notifMsg = dto.DongY
                    ? $"Lương T{bangLuong.Thang}/{bangLuong.Nam} - {bangLuong.HoTen} đã được phê duyệt"
                    : $"Lương T{bangLuong.Thang}/{bangLuong.Nam} - {bangLuong.HoTen} bị từ chối: {dto.LyDoTuChoi}";

                await _thongBaoService.CreateAndPushAsync(
                    userId: bangLuong.TaiKhoanGuiDuyetId.Value,
                    title: notifTitle,
                    message: notifMsg,
                    type: notifType,
                    relatedEntity: "BANG_LUONG",
                    relatedId: id,
                    link: "/hr/bang-luong",
                    senderId: userId
                );
            }

            // [REALTIME] Broadcast để các danh sách UI tự refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "BANG_LUONG",
                entityId: id,
                action: dto.DongY ? "APPROVED" : "REJECTED",
                data: new { TrangThai = dto.DongY ? "DA_DUYET" : "TU_CHOI" }
            );

            return Ok(new { message = dto.DongY ? "Đã duyệt lương" : "Đã từ chối bảng lương" });
        }

        // ============================================================
        // 5) HR & GIÁM ĐỐC – XEM CHI TIẾT BẢNG LƯƠNG
        // GET api/luong/{id}
        // ============================================================
        [HttpGet("{id}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var data = await _service.GetDetailAsync(id);
            if (data == null) return NotFound();

            return Ok(data);
        }
    }
}
