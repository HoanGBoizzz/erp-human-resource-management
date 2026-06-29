using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// API tra cứu các danh mục dùng cho Dropdown 
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LookupController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LookupController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lấy danh sách phòng ban cho dropdown
        /// GET api/lookup/phong-ban
        /// </summary>
        [HttpGet("phong-ban")]
        public async Task<IActionResult> GetPhongBanList()
        {
            var items = await _context.PhongBans
                .Where(x => x.TrangThai)
                .OrderBy(x => x.TenPhongBan)
                .Select(x => new
                {
                    id = x.Id,
                    maPhongBan = x.MaPhongBan,
                    tenPhongBan = x.TenPhongBan
                })
                .ToListAsync();

            return Ok(items);
        }

        /// <summary>
        /// Lấy danh sách chức vụ cho dropdown
        /// GET api/lookup/chuc-vu
        /// </summary>
        [HttpGet("chuc-vu")]
        public async Task<IActionResult> GetChucVuList()
        {
            var items = await _context.ChucVus
                .Where(x => x.TrangThai)
                .OrderBy(x => x.TenChucVu)
                .Select(x => new
                {
                    id = x.Id,
                    maChucVu = x.MaChucVu,
                    tenChucVu = x.TenChucVu
                })
                .ToListAsync();

            return Ok(items);
        }
    }
}
