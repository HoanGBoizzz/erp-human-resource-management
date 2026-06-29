using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.DeXuatGiamDoc;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DeXuatGiamDocController : ControllerBase
    {
        private readonly DeXuatGiamDocService _service;
        private readonly ThongBaoService _thongBaoService;
        private readonly AuditLogService _auditLogService;

        public DeXuatGiamDocController(
            DeXuatGiamDocService service,
            ThongBaoService thongBaoService,
            AuditLogService auditLogService)
        {
            _service = service;
            _thongBaoService = thongBaoService;
            _auditLogService = auditLogService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid")!);
        private string GetRole() => User.FindFirstValue("role")!;

        // ──────────────────────────────────────────────────────────────────────
        // 1) DANH SÁCH
        //    HR: xem đề xuất của mình
        //    GD: xem tất cả
        // ──────────────────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetList()
        {
            var role = GetRole();
            var userId = GetUserId();

            // HR chỉ xem đề xuất của chính mình
            int? filterById = role == "HR_ACC" ? userId : null;
            var list = await _service.GetListAsync(filterById);
            return Ok(list);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 2) CHI TIẾT
        // ──────────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var detail = await _service.GetDetailAsync(id);
            if (detail == null) return NotFound();

            // HR chỉ xem đề xuất của mình
            var role = GetRole();
            if (role == "HR_ACC" && detail.TaiKhoanTaoId != GetUserId())
                return Forbid();

            return Ok(detail);
        }

        // ──────────────────────────────────────────────────────────────────────
        // 3) TẠO MỚI (HR)
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Create([FromBody] DeXuatGiamDocCreateDto dto)
        {
            var userId = GetUserId();
            var newId = await _service.CreateAsync(dto, userId);

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: newId,
                tenDoiTuong: dto.TenDeXuat,
                hanhDong: "Tạo mới",
                ghiChu: $"Tạo đề xuất: {dto.TenDeXuat}"
            );

            return Ok(new { id = newId, message = "Tạo đề xuất thành công" });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 4) CẬP NHẬT (HR - chỉ khi NHAP hoặc DA_THU_HOI)
        // ──────────────────────────────────────────────────────────────────────
        [HttpPut("{id:int}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Update(int id, [FromBody] DeXuatGiamDocUpdateDto dto)
        {
            var userId = GetUserId();
            var (ok, msg) = await _service.UpdateAsync(id, dto, userId);
            if (!ok) return BadRequest(new { message = msg });

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: id,
                tenDoiTuong: dto.TenDeXuat,
                hanhDong: "Cập nhật",
                ghiChu: $"Cập nhật đề xuất #{id}: {dto.TenDeXuat}"
            );

            return Ok(new { message = msg });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 5) XÓA (HR - chỉ khi NHAP hoặc DA_THU_HOI)
        // ──────────────────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            // Lấy tên trước khi xóa
            var detail = await _service.GetDetailAsync(id);

            var (ok, msg) = await _service.DeleteAsync(id, userId);
            if (!ok) return BadRequest(new { message = msg });

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: id,
                tenDoiTuong: detail?.TenDeXuat,
                hanhDong: "Xóa",
                ghiChu: $"Xóa đề xuất #{id}: {detail?.TenDeXuat}"
            );

            return Ok(new { message = msg });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 6) GỬI DUYỆT (HR: NHAP → CHO_DUYET)
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost("{id:int}/gui-duyet")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> GuiDuyet(int id)
        {
            var userId = GetUserId();
            var (ok, msg) = await _service.GuiDuyetAsync(id, userId);
            if (!ok) return BadRequest(new { message = msg });

            // Lấy thông tin đề xuất
            var detail = await _service.GetDetailAsync(id);

            // [NOTIFICATION] Thông báo tất cả Giám đốc có đề xuất cần duyệt
            var gdIds = await _service.GetDirectorAccountIdsAsync();
            foreach (var gdId in gdIds)
            {
                await _thongBaoService.CreateAndPushAsync(
                    userId: gdId,
                    title: "Đề xuất chờ duyệt",
                    message: detail?.TenDeXuat ?? "",
                    type: "YEU_CAU_DUYET",
                    relatedEntity: "DE_XUAT_GIAM_DOC",
                    relatedId: id,
                    link: "/gd/duyet-de-xuat",
                    senderId: userId
                );
            }

            // [REALTIME] Trigger list refresh cho GD
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DE_XUAT_GIAM_DOC",
                entityId: id,
                action: "GUI_DUYET",
                data: new { TrangThai = "CHO_DUYET" }
            );

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: id,
                tenDoiTuong: detail?.TenDeXuat,
                hanhDong: "Gửi duyệt",
                ghiChu: $"Gửi đề xuất #{id} '{detail?.TenDeXuat}' để giám đốc duyệt"
            );

            return Ok(new { message = msg });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 7) THU HỒI (HR: CHO_DUYET → NHAP)
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost("{id:int}/thu-hoi")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> ThuHoi(int id)
        {
            var userId = GetUserId();
            var detail = await _service.GetDetailAsync(id);

            var (ok, msg) = await _service.ThuHoiAsync(id, userId);
            if (!ok) return BadRequest(new { message = msg });

            // [REALTIME] Trigger list refresh cho GD (đề xuất bị thu hồi biến mất khỏi danh sách chờ)
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DE_XUAT_GIAM_DOC",
                entityId: id,
                action: "THU_HOI",
                data: new { TrangThai = "NHAP" }
            );

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: id,
                tenDoiTuong: detail?.TenDeXuat,
                hanhDong: "Thu hồi",
                ghiChu: $"Thu hồi đề xuất #{id} '{detail?.TenDeXuat}'"
            );

            return Ok(new { message = msg });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 8) DUYỆT / TỪ CHỐI (Giám đốc)
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost("{id:int}/duyet")]
        [Authorize(Roles = "GIAM_DOC")]
        public async Task<IActionResult> Duyet(int id, [FromBody] DeXuatGiamDocApproveDto dto)
        {
            var userId = GetUserId();
            var (ok, msg) = await _service.DuyetAsync(id, dto, userId);
            if (!ok) return BadRequest(new { message = msg });

            // Lấy thông tin để thông báo lại HR
            var detail = await _service.GetDetailAsync(id);
            if (detail != null)
            {
                var title = dto.DongY ? "Đề xuất đã được duyệt" : "Đề xuất bị từ chối";
                var message = dto.DongY
                    ? $"Đề xuất '{detail.TenDeXuat}' đã được giám đốc phê duyệt"
                    : $"Đề xuất '{detail.TenDeXuat}' bị từ chối: {dto.LyDoTuChoi}";

                await _thongBaoService.CreateAndPushAsync(
                    userId: detail.TaiKhoanTaoId,
                    title: title,
                    message: message,
                    type: dto.DongY ? "DUYET" : "TU_CHOI",
                    relatedEntity: "DE_XUAT_GIAM_DOC",
                    relatedId: id,
                    link: "/hr/de-xuat-giam-doc",
                    senderId: userId
                );
            }

            // [REALTIME] Trigger list refresh
            await _thongBaoService.BroadcastEntityUpdateAsync(
                entityType: "DE_XUAT_GIAM_DOC",
                entityId: id,
                action: "DUYET",
                data: new { TrangThai = dto.DongY ? "DA_DUYET" : "TU_CHOI" }
            );

            var hanhDong = dto.DongY ? "Duyệt" : "Từ chối";
            var ghiChu = dto.DongY
                ? $"Giám đốc duyệt đề xuất #{id} '{detail?.TenDeXuat}'"
                : $"Giám đốc từ chối đề xuất #{id} '{detail?.TenDeXuat}'. Lý do: {dto.LyDoTuChoi}";

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: id,
                tenDoiTuong: detail?.TenDeXuat,
                hanhDong: hanhDong,
                ghiChu: ghiChu
            );

            return Ok(new { message = msg });
        }

        // ──────────────────────────────────────────────────────────────────────
        // 9) UPLOAD FILE ĐÍNH KÈM (HR)
        // ──────────────────────────────────────────────────────────────────────
        [HttpPost("{id:int}/upload")]
        [Authorize(Roles = "HR_ACC")]
        public async Task<IActionResult> UploadFile(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Vui lòng chọn file" });

            var userId = GetUserId();
            var detail = await _service.GetDetailAsync(id);

            var (ok, msg, url, tenGoc) = await _service.UploadFileAsync(id, file, userId);
            if (!ok) return BadRequest(new { message = msg });

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đề xuất giám đốc",
                doiTuongId: id,
                tenDoiTuong: detail?.TenDeXuat,
                hanhDong: "Upload file",
                ghiChu: $"Upload file '{file.FileName}' cho đề xuất #{id} '{detail?.TenDeXuat}'"
            );

            return Ok(new { message = msg, url, tenGoc });
        }
    }
}
