using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// API quản lý thông báo realtime
    /// </summary>
    [Route("api/thong-bao")]
    [ApiController]
    [Authorize]
    public class ThongBaoController : ControllerBase
    {
        private readonly ThongBaoService _thongBaoService;

        public ThongBaoController(ThongBaoService thongBaoService)
        {
            _thongBaoService = thongBaoService;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");

        /// <summary>
        /// Lấy danh sách thông báo chưa đọc
        /// </summary>
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread([FromQuery] int limit = 20)
        {
            var userId = GetUserId();
            var notifications = await _thongBaoService.GetUnreadAsync(userId, limit);
            return Ok(notifications);
        }

        /// <summary>
        /// Lấy tất cả thông báo (có phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var notifications = await _thongBaoService.GetAllAsync(userId, page, pageSize);
            return Ok(notifications);
        }

        /// <summary>
        /// Đếm số thông báo chưa đọc
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> CountUnread()
        {
            var userId = GetUserId();
            var count = await _thongBaoService.CountUnreadAsync(userId);
            return Ok(new { count });
        }

        /// <summary>
        /// Đánh dấu 1 thông báo đã đọc
        /// </summary>
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetUserId();
            var success = await _thongBaoService.MarkAsReadAsync(id, userId);
            
            if (!success)
                return NotFound(new { message = "Không tìm thấy thông báo" });

            return Ok(new { message = "Đã đánh dấu đọc" });
        }

        /// <summary>
        /// Đánh dấu tất cả đã đọc
        /// </summary>
        /// <summary>
        /// Đánh dấu tất cả thông báo đã đọc
        /// </summary>
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetUserId();
            var count = await _thongBaoService.MarkAllAsReadAsync(userId);
            return Ok(new { message = $"Đã đánh dấu {count} thông báo đã đọc", count });
        }

        /// <summary>
        /// Đánh dấu thông báo liên quan đến entity đã đọc
        /// Dùng khi user navigate vào chi tiết entity
        /// </summary>
        [HttpPost("mark-read-by-entity")]
        public async Task<IActionResult> MarkAsReadByEntity([FromQuery] string entityType, [FromQuery] int entityId)
        {
            var userId = GetUserId();
            var count = await _thongBaoService.MarkAsReadByEntityAsync(userId, entityType, entityId);
            return Ok(new { message = $"Đã đánh dấu {count} thông báo đã đọc", count });
        }
    }
}
