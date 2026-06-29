using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.FaceRecognition;
using QLNS_BE.Services;
using QLNS_BE.Services.FaceRecognition;
using System.Security.Claims;

namespace QLNS_BE.Controllers
{
    [ApiController]
    [Route("api/face-recognition")]
    public class FaceRecognitionController : ControllerBase
    {
        private readonly FaceDataService _faceDataService;
        private readonly ChamCongService _chamCongService;
        private readonly IFaceRecognitionService _faceService;
        private readonly ILogger<FaceRecognitionController> _logger;
        private readonly IConfiguration _config;

        public FaceRecognitionController(
            FaceDataService faceDataService,
            ChamCongService chamCongService,
            IFaceRecognitionService faceService,
            ILogger<FaceRecognitionController> logger,
            IConfiguration config)
        {
            _faceDataService = faceDataService;
            _chamCongService = chamCongService;
            _faceService = faceService;
            _logger = logger;
            _config = config;
        }

        // ============================================================
        // 0. HEALTH CHECK & DIAGNOSTICS
        // ============================================================

        /// <summary>
        /// Kiểm tra AI service đang hoạt động (Gemini hay Simple Hash)
        /// GET /api/face-recognition/health
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult GetHealthStatus()
        {
            var geminiKey = _config["Gemini:ApiKey"];
            var hasGeminiKey = !string.IsNullOrEmpty(geminiKey);
            var serviceName = _faceService.GetType().Name;

            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                faceRecognitionService = new
                {
                    activeService = serviceName,
                    isGeminiAI = serviceName.Contains("Gemini"),
                    isSimpleHash = serviceName.Contains("Simple"),
                    hasGeminiApiKey = hasGeminiKey,
                    geminiModel = _config["Gemini:Model"],
                    confidenceThreshold = _config["FaceRecognition:ConfidenceThreshold"],
                    minQualityScore = _config["FaceRecognition:MinQualityScore"]
                },
                capabilities = new
                {
                    faceDetection = true,
                    antiSpoofing = serviceName.Contains("Gemini"), // Chỉ Gemini có anti-spoof
                    qualityAssessment = true,
                    faceMatching = true
                },
                message = serviceName.Contains("Gemini")
                    ? "✅ Đang sử dụng Gemini AI - Nhận diện chính xác + Anti-spoofing"
                    : "⚠️ Đang sử dụng Simple Hash (fallback) - Độ chính xác thấp hơn, không có anti-spoof"
            });
        }

        // ============================================================
        // 1. ĐĂNG KÝ & QUẢN LÝ KHUÔN MẶT
        // ============================================================

        /// <summary>
        /// Đăng ký khuôn mặt cho nhân viên (chỉ HR/Admin)
        /// </summary>
        [HttpPost("register/{nvId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RegisterFace(int nvId, IFormFile image)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ảnh" });
                }

                var result = await _faceDataService.RegisterFaceAsync(nvId, image, userId);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in RegisterFace for employee {nvId}");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        /// <summary>
        /// Nhân viên tự đăng ký khuôn mặt của mình
        /// </summary>
        [HttpPost("register-self")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RegisterSelfFace(IFormFile image)
        {
            try
            {
                // Lấy thông tin từ JWT token
                var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
                var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

                if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return BadRequest(new { message = "Tài khoản chưa được liên kết với nhân viên. Vui lòng liên hệ HR." });
                }

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ảnh" });
                }

                var result = await _faceDataService.RegisterFaceAsync(employeeId, image, userId);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RegisterSelfFace");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        /// <summary>
        /// Lấy danh sách nhân viên đã đăng ký khuôn mặt
        /// </summary>
        [HttpGet("registered")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetRegisteredFaces()
        {
            try
            {
                var data = await _faceDataService.GetRegisteredEmployeesAsync();
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registered faces");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy chi tiết face data của 1 nhân viên
        /// </summary>
        [HttpGet("employee/{nvId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetEmployeeFaceData(int nvId)
        {
            try
            {
                var data = await _faceDataService.GetEmployeeFaceDataAsync(nvId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting face data for employee {nvId}");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Xóa 1 ảnh khuôn mặt (soft delete)
        /// </summary>
        [HttpDelete("face/{faceId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> DeleteFaceData(int faceId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
                var result = await _faceDataService.DeleteFaceDataAsync(faceId, userId);

                if (!result)
                    return NotFound(new { message = "Không tìm thấy dữ liệu khuôn mặt" });

                return Ok(new { message = "Xóa dữ liệu khuôn mặt thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting face data {faceId}");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Nhân viên tự xóa ảnh khuôn mặt của mình
        /// </summary>
        [HttpDelete("my-face/{faceId}")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> DeleteMyFace(int faceId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
                var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;

                if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
                {
                    return BadRequest(new { message = "Không xác định được thông tin nhân viên" });
                }

                var (found, authorized) = await _faceDataService.DeleteFaceDataIfOwnerAsync(faceId, userId, employeeId);

                if (!found)
                    return NotFound(new { message = "Không tìm thấy ảnh khuôn mặt" });

                if (!authorized)
                    return Forbid();

                return Ok(new { message = "Xóa ảnh khuôn mặt thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting my face data {faceId}");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Xóa tất cả ảnh khuôn mặt của nhân viên
        /// </summary>
        [HttpDelete("employee/{nvId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> DeleteAllFaceData(int nvId)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("userid")?.Value ?? "0");
                var result = await _faceDataService.DeleteAllFaceDataAsync(nvId, userId);

                if (!result)
                    return NotFound(new { message = "Nhân viên chưa đăng ký khuôn mặt" });

                return Ok(new { message = "Xóa tất cả dữ liệu khuôn mặt thành công" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting all face data for employee {nvId}");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy dữ liệu khuôn mặt của chính mình (dành cho EMPLOYEE)
        /// </summary>
        [HttpGet("my-face-data")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetMyFaceData()
        {
            try
            {
                // Lấy nvHoSoId từ JWT claims
                var nvHoSoIdClaim = User.FindFirst("EmployeeId")?.Value;

                if (string.IsNullOrEmpty(nvHoSoIdClaim) || !int.TryParse(nvHoSoIdClaim, out int nvHoSoId))
                {
                    return BadRequest(new { message = "Không xác định được thông tin nhân viên" });
                }

                var data = await _faceDataService.GetEmployeeFaceDataAsync(nvHoSoId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my face data");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        // ============================================================
        // 2. CHẤM CÔNG BẰNG KHUÔN MẶT ⭐
        // ============================================================

        /// <summary>
        /// Chấm công VÀO bằng khuôn mặt (YÊU CẦU ĐĂNG NHẬP)
        /// ✅ Chỉ chấm công cho tài khoản đang đăng nhập
        /// </summary>
        [HttpPost("attendance/check-in")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")] // YÊU CẦU ĐĂNG NHẬP
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CheckInByFace(IFormFile image, [FromQuery] bool xacNhanOt = false)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ảnh" });
                }

                // ✅ Lấy nvHoSoId từ JWT token
                var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
                if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int nvHoSoId))
                {
                    return BadRequest(new { message = "Tài khoản chưa được liên kết với nhân viên. Vui lòng liên hệ HR." });
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var device = Request.Headers["User-Agent"].ToString();

                // ✅ Truyền nvHoSoId để CHỈ verify face của người đang login
                var result = await _chamCongService.CheckInByFaceAsync(image, ip, device, nvHoSoId, xacNhanOt);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckInByFace");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống. Vui lòng thử lại sau."
                });
            }
        }

        /// <summary>
        /// Chấm công RA bằng khuôn mặt (YÊU CẦU ĐĂNG NHẬP)
        /// ✅ Chỉ chấm công cho tài khoản đang đăng nhập
        /// </summary>
        [HttpPost("attendance/check-out")]
        [Authorize(Roles = "EMPLOYEE,HR_ACC,GIAM_DOC")] // YÊU CẦU ĐĂNG NHẬP
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CheckOutByFace([FromQuery] bool xacNhanOtOut, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ảnh" });
                }

                // ✅ Lấy nvHoSoId từ JWT token
                var employeeIdClaim = User.FindFirst("EmployeeId")?.Value;
                if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int nvHoSoId))
                {
                    return BadRequest(new { message = "Tài khoản chưa được liên kết với nhân viên. Vui lòng liên hệ HR." });
                }

                var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                var device = Request.Headers["User-Agent"].ToString();

                // ✅ Truyền nvHoSoId để CHỈ verify face của người đang login
                var result = await _chamCongService.CheckOutByFaceAsync(image, ip, device, nvHoSoId, xacNhanOtOut);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckOutByFace");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi hệ thống. Vui lòng thử lại sau."
                });
            }
        }

        // ============================================================
        // 3. LOGS & VERIFICATION
        // ============================================================

        /// <summary>
        /// Verify khuôn mặt (test - không tạo chấm công)
        /// </summary>
        [HttpPost("verify/{nvId}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> VerifyFace(int nvId, IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Vui lòng chọn ảnh" });
                }

                using var stream = image.OpenReadStream();
                var (matchedNvId, confidence) = await _faceService.IdentifyEmployeeAsync(stream);

                var isMatch = matchedNvId == nvId;

                return Ok(new
                {
                    isMatch,
                    matchedNvId,
                    confidence,
                    message = isMatch
                        ? $"✅ Khớp với độ tin cậy {confidence:P0}"
                        : matchedNvId != null
                            ? $"❌ Không khớp. Nhận diện là nhân viên #{matchedNvId}"
                            : "❌ Không nhận diện được khuôn mặt"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying face for employee {nvId}");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Lấy lịch sử nhận diện khuôn mặt
        /// </summary>
        [HttpGet("logs")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetFaceLogs([FromQuery] FaceLogFilterDto filter)
        {
            try
            {
                var data = await _chamCongService.GetFaceLogsAsync(filter);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting face logs");
                return StatusCode(500, new { message = "Lỗi hệ thống" });
            }
        }
    }
}
