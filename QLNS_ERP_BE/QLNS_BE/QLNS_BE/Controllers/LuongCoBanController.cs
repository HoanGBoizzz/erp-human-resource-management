using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// Quản lý lương cơ bản (NvLuongHienTai) cho từng nhân viên
    /// </summary>
    [ApiController]
    [Route("api/luong-co-ban")]
    [Authorize(Roles = "HR_ACC")]
    public class LuongCoBanController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AuditLogService _audit;

        public LuongCoBanController(AppDbContext context, AuditLogService audit)
        {
            _context = context;
            _audit = audit;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        /// <summary>GET danh sách lương cơ bản của tất cả nhân viên (đang áp dụng)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.NvLuongHienTais
                .Where(x => x.DangApDung)
                .Include(x => x.NvHoSo)
                .OrderBy(x => x.NvHoSo.HoTen)
                .Select(x => new LuongCoBanDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    HoTen = x.NvHoSo.HoTen,
                    MaNhanVien = x.NvHoSo.MaNhanVien,
                    TenPhongBan = x.NvHoSo.CongViecs
                        .OrderByDescending(cv => cv.NgayVaoLam)
                        .Select(cv => cv.PhongBan.TenPhongBan)
                        .FirstOrDefault(),
                    LuongCoBan = x.LuongCoBan,
                    PhuCapCoDinh = x.PhuCapCoDinh,
                    SoTaiKhoanNganHang = x.SoTaiKhoanNganHang,
                    TenNganHang = x.TenNganHang,
                    ChiNhanhNganHang = x.ChiNhanhNganHang,
                    NgayBatDauHieuLuc = x.NgayBatDauHieuLuc,
                    NgayKetThucHieuLuc = x.NgayKetThucHieuLuc,
                    DangApDung = x.DangApDung
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>GET lương cơ bản của 1 nhân viên</summary>
        [HttpGet("nhan-vien/{nvId}")]
        public async Task<IActionResult> GetByNv(int nvId)
        {
            var data = await _context.NvLuongHienTais
                .Where(x => x.NvHoSoId == nvId)
                .OrderByDescending(x => x.NgayBatDauHieuLuc)
                .Select(x => new LuongCoBanDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    LuongCoBan = x.LuongCoBan,
                    PhuCapCoDinh = x.PhuCapCoDinh,
                    SoTaiKhoanNganHang = x.SoTaiKhoanNganHang,
                    TenNganHang = x.TenNganHang,
                    ChiNhanhNganHang = x.ChiNhanhNganHang,
                    NgayBatDauHieuLuc = x.NgayBatDauHieuLuc,
                    NgayKetThucHieuLuc = x.NgayKetThucHieuLuc,
                    DangApDung = x.DangApDung
                })
                .ToListAsync();
            return Ok(data);
        }

        /// <summary>Cập nhật lương cơ bản (tạo bản ghi mới, deactivate cũ)</summary>
        [HttpPost("nhan-vien/{nvId}")]
        public async Task<IActionResult> UpsertLuong(int nvId, [FromBody] LuongCoBanUpdateDto dto)
        {
            // Deactivate bản ghi cũ đang áp dụng
            var existing = await _context.NvLuongHienTais
                .Where(x => x.NvHoSoId == nvId && x.DangApDung)
                .ToListAsync();

            foreach (var old in existing)
            {
                old.DangApDung = false;
                old.NgayKetThucHieuLuc = dto.NgayBatDauHieuLuc.AddDays(-1);
            }

            // Tạo bản ghi mới
            var newRecord = new QLNS_BE.Models.Entities.NvLuongHienTai
            {
                NvHoSoId = nvId,
                LuongCoBan = dto.LuongCoBan,
                PhuCapCoDinh = dto.PhuCapCoDinh,
                SoTaiKhoanNganHang = dto.SoTaiKhoanNganHang,
                TenNganHang = dto.TenNganHang,
                ChiNhanhNganHang = dto.ChiNhanhNganHang,
                NgayBatDauHieuLuc = dto.NgayBatDauHieuLuc,
                DangApDung = true
            };
            _context.NvLuongHienTais.Add(newRecord);

            // ─── Sync STK bank account back to NvHoSo (nguồn gốc) ───
            var nv = await _context.NvHoSos.FindAsync(nvId);
            if (nv != null)
                nv.SoTaiKhoanNganHang = dto.SoTaiKhoanNganHang;

            await _context.SaveChangesAsync();

            await _audit.LogActionAsync(GetUserId(), "NV_LUONG_HIEN_TAI", nvId,
                nv?.HoTen ?? "", "Cập nhật lương cơ bản",
                $"Cập nhật lương cơ bản: {dto.LuongCoBan:N0} VND, hiệu lực từ {dto.NgayBatDauHieuLuc:dd/MM/yyyy}, STK: {dto.SoTaiKhoanNganHang} - {dto.TenNganHang}");

            return Ok(new { message = "Đã cập nhật lương cơ bản", id = newRecord.Id });
        }
    }
}
