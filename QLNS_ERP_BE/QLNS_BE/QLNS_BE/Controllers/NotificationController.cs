using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    /// <summary>
    /// API lấy số lượng thông báo cho sidebar badges
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _service;

        public NotificationController(NotificationService service)
        {
            _service = service;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue("userid") ?? "0");
        private string GetRole() => User.FindFirstValue(ClaimTypes.Role) ?? "";

        /// <summary>
        /// Lấy số lượng thông báo cho từng mục sidebar
        /// GET api/notification/counts
        /// </summary>
        [HttpGet("counts")]
        public async Task<IActionResult> GetCounts()
        {
            var counts = await _service.GetCountsAsync(GetUserId(), GetRole());
            return Ok(counts);
        }
    }
}
