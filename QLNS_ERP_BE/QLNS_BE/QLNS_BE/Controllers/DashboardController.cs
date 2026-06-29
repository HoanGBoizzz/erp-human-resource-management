using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Services;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
   [ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        // ✅ LẤY ĐÚNG ROLE
        var roleClaim = User.FindFirst(ClaimTypes.Role);
        if (roleClaim == null)
        {
            Console.WriteLine("[Dashboard] ❌ Role claim not found");
            return Unauthorized();
        }

        var role = roleClaim.Value;
        Console.WriteLine($"[Dashboard] Role: {role}");

        // ✅ LẤY EMPLOYEEID TỪ JWT (QUAN TRỌNG!)
        var empIdClaim = User.FindFirst("EmployeeId");
        
        if (empIdClaim == null || !int.TryParse(empIdClaim.Value, out int employeeId))
        {
            Console.WriteLine("[Dashboard] ❌ EmployeeId claim not found or invalid");
            return BadRequest(new { message = "Không tìm thấy liên kết nhân viên." });
        }

        Console.WriteLine($"[Dashboard] EmployeeId from token: {employeeId}");

        // ✅ GỌI SERVICE VỚI EMPLOYEEID
        switch (role)
        {
            case "EMPLOYEE":
                var empData = await _dashboardService.GetEmployeeDashboardAsync(employeeId);
                Console.WriteLine($"[Dashboard] Employee data: hoTen = {empData?.HoTen}");
                return Ok(new { role = "EMPLOYEE", data = empData });

            case "HR_ACC":
                var hrData = await _dashboardService.GetHrDashboardAsync();
                return Ok(new { role = "HR_ACC", data = hrData });

            case "GIAM_DOC":
                var gdData = await _dashboardService.GetDirectorDashboardAsync();
                return Ok(new { role = "GIAM_DOC", data = gdData });

            default:
                return Forbid();
        }
    }
}
}
