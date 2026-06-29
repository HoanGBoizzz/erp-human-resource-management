using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.Task;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly TaskService _taskService;

        public TaskController(TaskService taskService)
        {
            _taskService = taskService;
        }

        // GET /api/tasks/cua-toi - Danh sách task của tôi (nhân viên)
        [HttpGet("cua-toi")]
        public async Task<IActionResult> GetMyTasks()
        {
            var employeeIdClaim = User.FindFirstValue("EmployeeId");
            if (string.IsNullOrEmpty(employeeIdClaim))
                return BadRequest(new { message = "Không tìm thấy thông tin nhân viên" });

            int employeeId = int.Parse(employeeIdClaim);
            var tasks = await _taskService.GetMyTasksAsync(employeeId);

            return Ok(new { tasks });
        }

        // PUT /api/tasks/{id} - Cập nhật task (nhân viên cập nhật tiến độ)
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDto dto)
        {
            var userId = int.Parse(User.FindFirstValue("userid")!);
            var success = await _taskService.UpdateTaskAsync(id, dto, userId);

            if (!success)
                return NotFound(new { message = "Không tìm thấy task" });

            return Ok(new { message = "Cập nhật task thành công" });
        }
    }
}
