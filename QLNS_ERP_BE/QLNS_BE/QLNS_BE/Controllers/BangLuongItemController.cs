using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.Luong;
using QLNS_BE.Models.Entities;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// Quản lý chi tiết thưởng / khấu trừ trong bảng lương tháng
    /// </summary>
    [ApiController]
    [Route("api/luong/{bangLuongId}/items")]
    [Authorize(Roles = "HR_ACC")]
    public class BangLuongItemController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly LuongService _luongService;
        private readonly AuditLogService _audit;

        public BangLuongItemController(AppDbContext context, LuongService luongService, AuditLogService audit)
        {
            _context = context;
            _luongService = luongService;
            _audit = audit;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        /// <summary>Lấy danh sách items (thưởng/khấu trừ) của bảng lương</summary>
        [HttpGet]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetItems(int bangLuongId)
        {
            var items = await _context.BangLuongItems
                .Where(x => x.BangLuongThangId == bangLuongId)
                .OrderBy(x => x.Loai).ThenBy(x => x.CreatedAt)
                .Select(x => new BangLuongItemDto
                {
                    Id = x.Id,
                    BangLuongThangId = x.BangLuongThangId,
                    Loai = x.Loai,
                    LyDo = x.LyDo,
                    SoTien = x.SoTien,
                    CreatedAt = x.CreatedAt
                }).ToListAsync();
            return Ok(items);
        }

        /// <summary>Thêm thưởng hoặc khấu trừ vào bảng lương</summary>
        [HttpPost]
        public async Task<IActionResult> AddItem(int bangLuongId, [FromBody] BangLuongItemCreateDto dto)
        {
            var bl = await _context.BangLuongThangs
                .Include(x => x.NvHoSo)
                .FirstOrDefaultAsync(x => x.Id == bangLuongId);
            if (bl == null) return NotFound();

            if (bl.TrangThai == "DA_KHOA")
                return BadRequest(new { message = "Bảng lương đã khóa, không thể chỉnh sửa" });

            var item = new BangLuongItem
            {
                BangLuongThangId = bangLuongId,
                Loai = dto.Loai.ToUpper(),
                LyDo = dto.LyDo,
                SoTien = dto.SoTien,
                TaiKhoanTaoId = GetUserId()
            };
            _context.BangLuongItems.Add(item);

            // Cập nhật tổng thưởng / khấu trừ trong bảng lương
            await RecalcTotalsAsync(bl);
            await _context.SaveChangesAsync();

            var loaiLabel = dto.Loai.ToUpper() == "THUONG" ? "Thưởng" : "Khấu trừ";
            await _audit.LogActionAsync(GetUserId(), "BANG_LUONG_ITEM", bangLuongId,
                bl.NvHoSo.HoTen, "Thêm " + loaiLabel,
                $"{loaiLabel}: {dto.SoTien:N0} VND - {dto.LyDo} (Lương T{bl.Thang}/{bl.Nam})");

            return Ok(new BangLuongItemDto
            {
                Id = item.Id,
                BangLuongThangId = item.BangLuongThangId,
                Loai = item.Loai,
                LyDo = item.LyDo,
                SoTien = item.SoTien,
                CreatedAt = item.CreatedAt
            });
        }

        /// <summary>Xóa một item thưởng/khấu trừ</summary>
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> DeleteItem(int bangLuongId, int itemId)
        {
            var item = await _context.BangLuongItems
                .FirstOrDefaultAsync(x => x.Id == itemId && x.BangLuongThangId == bangLuongId);
            if (item == null) return NotFound();

            var bl = await _context.BangLuongThangs
                .Include(x => x.NvHoSo)
                .FirstOrDefaultAsync(x => x.Id == bangLuongId);
            if (bl?.TrangThai == "DA_KHOA")
                return BadRequest(new { message = "Bảng lương đã khóa" });

            _context.BangLuongItems.Remove(item);
            if (bl != null) await RecalcTotalsAsync(bl);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa" });
        }

        /// <summary>Tính lại tổng thưởng / khấu trừ trong BangLuongThang từ BangLuongItems</summary>
        private async Task RecalcTotalsAsync(BangLuongThang bl)
        {
            var items = await _context.BangLuongItems
                .Where(x => x.BangLuongThangId == bl.Id)
                .ToListAsync();

            bl.Thuong = items.Where(x => x.Loai == "THUONG").Sum(x => x.SoTien);
            bl.KhauTru = items.Where(x => x.Loai == "KHAU_TRU").Sum(x => x.SoTien);

            // Tính lại TongLuong
            decimal luongNgay = bl.LuongCoBanTinh / 26m;
            decimal luongOt = bl.TongOt * (luongNgay / 8m) * 1.5m;
            bl.TongLuong = (luongNgay * bl.TongCong) + bl.PhuCapTinh + luongOt + bl.Thuong - bl.KhauTru;
            if (bl.TongLuong < 0) bl.TongLuong = 0;
        }
    }
}
