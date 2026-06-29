using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.NoiLamViec;
using QLNS_BE.Services;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/noi-lam-viec")]
    [Authorize]
    public class NoiLamViecController : ControllerBase
    {
        private readonly NoiLamViecService _service;
        private readonly AuditLogService _auditLogService;

        public NoiLamViecController(NoiLamViecService service, AuditLogService auditLogService)
        {
            _service = service;
            _auditLogService = auditLogService;
        }

        private int GetUserId()
            => int.Parse(User.Claims.First(x => x.Type == "userid").Value);

        private bool IsHr()
            => User.IsInRole("HR_ACC") || User.IsInRole("GIAMDOC");

        // ===========================================================
        // THỐNG KÊ
        // ===========================================================

        [HttpGet("thong-ke")]
        public async Task<IActionResult> GetThongKe()
        {
            var userId = GetUserId();
            var result = await _service.GetThongKeAsync(userId);
            return Ok(result);
        }

        // ===========================================================
        // PHIẾU ĐỀ XUẤT DỤNG CỤ
        // ===========================================================

        [HttpGet("de-xuat")]
        public async Task<IActionResult> GetDeXuatList()
        {
            var userId = GetUserId();
            var result = await _service.GetDeXuatListAsync(userId, IsHr());
            return Ok(result);
        }

        [HttpPost("de-xuat")]
        public async Task<IActionResult> CreateDeXuat([FromBody] CreatePhieuDeXuatDto dto)
        {
            var userId = GetUserId();
            var id = await _service.CreateDeXuatAsync(userId, dto);

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Phiếu đề xuất dụng cụ",
                doiTuongId: id,
                tenDoiTuong: dto.TenDungCu,
                hanhDong: "Tạo mới",
                ghiChu: $"Tạo phiếu đề xuất: {dto.TenDungCu} x{dto.SoLuong}"
            );

            return Ok(new { id });
        }

        [HttpPut("de-xuat/duyet")]
        [Authorize(Roles = "HR_ACC,GIAMDOC")]
        public async Task<IActionResult> DuyetDeXuat([FromBody] DuyetPhieuDeXuatDto dto)
        {
            var userId = GetUserId();
            await _service.DuyetDeXuatAsync(dto, userId);

            string hanhDong = dto.ChapNhan ? "Phê duyệt" : "Từ chối";
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Phiếu đề xuất dụng cụ",
                doiTuongId: dto.PhieuId,
                tenDoiTuong: $"Phiếu #{dto.PhieuId}",
                hanhDong: hanhDong,
                ghiChu: dto.ChapNhan ? "Phê duyệt phiếu" : $"Từ chối: {dto.LyDoTuChoi}"
            );

            return Ok();
        }

        [HttpDelete("de-xuat/{id}")]
        public async Task<IActionResult> DeleteDeXuat(int id)
        {
            var userId = GetUserId();
            await _service.DeleteDeXuatAsync(id, userId);
            return Ok();
        }

        [HttpPut("de-xuat/{id}")]
        public async Task<IActionResult> UpdateDeXuat(int id, [FromBody] UpdatePhieuDeXuatDto dto)
        {
            var userId = GetUserId();
            await _service.UpdateDeXuatAsync(id, userId, dto);
            return Ok();
        }

        // ===========================================================
        // PHIẾU TẠM ỨNG
        // ===========================================================

        [HttpGet("tam-ung")]
        public async Task<IActionResult> GetTamUngList()
        {
            var userId = GetUserId();
            var result = await _service.GetTamUngListAsync(userId, IsHr());
            return Ok(result);
        }

        [HttpPost("tam-ung")]
        public async Task<IActionResult> CreateTamUng([FromBody] CreatePhieuTamUngDto dto)
        {
            var userId = GetUserId();
            var id = await _service.CreateTamUngAsync(userId, dto);

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Phiếu tạm ứng",
                doiTuongId: id,
                tenDoiTuong: dto.MucDich,
                hanhDong: "Tạo mới",
                ghiChu: $"Tạo phiếu tạm ứng: {dto.MucDich} - {dto.SoTien:N0}đ"
            );

            return Ok(new { id });
        }

        [HttpPut("tam-ung/duyet")]
        [Authorize(Roles = "HR_ACC,GIAMDOC")]
        public async Task<IActionResult> DuyetTamUng([FromBody] DuyetPhieuTamUngDto dto)
        {
            var userId = GetUserId();
            await _service.DuyetTamUngAsync(dto, userId);

            string hanhDong = dto.ChapNhan ? "Phê duyệt" : "Từ chối";
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Phiếu tạm ứng",
                doiTuongId: dto.PhieuId,
                tenDoiTuong: $"Phiếu #{dto.PhieuId}",
                hanhDong: hanhDong,
                ghiChu: dto.ChapNhan ? "Phê duyệt phiếu tạm ứng" : $"Từ chối: {dto.LyDoTuChoi}"
            );

            return Ok();
        }

        [HttpDelete("tam-ung/{id}")]
        public async Task<IActionResult> DeleteTamUng(int id)
        {
            var userId = GetUserId();
            await _service.DeleteTamUngAsync(id, userId);
            return Ok();
        }

        [HttpPut("tam-ung/{id}")]
        public async Task<IActionResult> UpdateTamUng(int id, [FromBody] UpdatePhieuTamUngDto dto)
        {
            var userId = GetUserId();
            await _service.UpdateTamUngAsync(id, userId, dto);
            return Ok();
        }

        // ===========================================================
        // ĐƠN ĐI MUỘN / VỀ SỚM
        // ===========================================================

        [HttpGet("don-di-muon")]
        public async Task<IActionResult> GetDiMuonList()
        {
            var userId = GetUserId();
            var result = await _service.GetDiMuonListAsync(userId, IsHr());
            return Ok(result);
        }

        [HttpPost("don-di-muon")]
        public async Task<IActionResult> CreateDiMuon([FromBody] CreateDonDiMuonDto dto)
        {
            var userId = GetUserId();
            var id = await _service.CreateDiMuonAsync(userId, dto);

            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đơn đi muộn/về sớm",
                doiTuongId: id,
                tenDoiTuong: dto.Loai,
                hanhDong: "Tạo mới",
                ghiChu: $"Tạo đơn {dto.Loai} ngày {dto.NgayApDung:dd/MM/yyyy}"
            );

            return Ok(new { id });
        }

        [HttpPut("don-di-muon/duyet")]
        [Authorize(Roles = "HR_ACC,GIAMDOC")]
        public async Task<IActionResult> DuyetDiMuon([FromBody] DuyetDonDiMuonDto dto)
        {
            var userId = GetUserId();
            await _service.DuyetDiMuonAsync(dto, userId);

            string hanhDong = dto.ChapNhan ? "Phê duyệt" : "Từ chối";
            await _auditLogService.LogActionAsync(
                taiKhoanId: userId,
                bang: "Đơn đi muộn/về sớm",
                doiTuongId: dto.DonId,
                tenDoiTuong: $"Đơn #{dto.DonId}",
                hanhDong: hanhDong,
                ghiChu: dto.ChapNhan ? "Phê duyệt đơn" : $"Từ chối: {dto.LyDoTuChoi}"
            );

            return Ok();
        }

        [HttpDelete("don-di-muon/{id}")]
        public async Task<IActionResult> DeleteDiMuon(int id)
        {
            var userId = GetUserId();
            await _service.DeleteDiMuonAsync(id, userId);
            return Ok();
        }

        [HttpPut("don-di-muon/{id}")]
        public async Task<IActionResult> UpdateDiMuon(int id, [FromBody] UpdateDonDiMuonDto dto)
        {
            var userId = GetUserId();
            await _service.UpdateDiMuonAsync(id, userId, dto);
            return Ok();
        }
    }
}
