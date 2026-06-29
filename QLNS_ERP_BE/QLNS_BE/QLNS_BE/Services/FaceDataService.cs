using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using QLNS_BE.Models.Dtos.FaceRecognition;
using QLNS_BE.Models.Entities;
using QLNS_BE.Services.FaceRecognition;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using System.Text.Json;

namespace QLNS_BE.Services
{
    public class FaceDataService
    {
        private readonly AppDbContext _context;
        private readonly IFaceRecognitionService _faceService;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;
        private readonly IConfiguration _config;
        private readonly ILogger<FaceDataService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FaceDataService(
            AppDbContext context,
            IFaceRecognitionService faceService,
            IWebHostEnvironment env,
            AuditLogService auditLog,
            IConfiguration config,
            ILogger<FaceDataService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _faceService = faceService;
            _env = env;
            _auditLog = auditLog;
            _config = config;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Đăng ký khuôn mặt cho nhân viên
        /// </summary>
        public async Task<RegisterFaceResponseDto> RegisterFaceAsync(
            int nvHoSoId,
            IFormFile imageFile,
            int createdBy)
        {
            try
            {
                // 1. Validate nhân viên tồn tại
                var nv = await _context.NvHoSos.FindAsync(nvHoSoId);
                if (nv == null)
                {
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = "Nhân viên không tồn tại"
                    };
                }

                // 2. Kiểm tra số lượng face đã đăng ký
                var existingFaces = await _context.NvFaceDatas
                    .Where(x => x.NvHoSoId == nvHoSoId && x.IsActive)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

                var maxFaces = int.Parse(_config["FaceRecognition:MaxFacePerEmployee"] ?? "3");
                if (existingFaces.Count >= maxFaces)
                {
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = $"Đã đạt giới hạn {maxFaces} ảnh/nhân viên. Vui lòng xóa ảnh cũ trước."
                    };
                }

                // 2.1. Đánh số thứ tự ảnh (ảnh 1 là gốc)
                var imageNumber = existingFaces.Count + 1;

                // 3. Validate file
                if (!IsValidImageFile(imageFile))
                {
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = "File không hợp lệ. Chỉ chấp nhận ảnh JPG, PNG (max 5MB)"
                    };
                }

                // 4. Extract face encoding bằng Gemini AI
                using var stream = imageFile.OpenReadStream();
                var encoding = await _faceService.ExtractFaceEncodingAsync(stream);

                if (string.IsNullOrEmpty(encoding))
                {
                    _logger.LogWarning($"❌ [ĐĂNG KÝ FACE] Gemini không trả về kết quả cho NV #{nvHoSoId}");
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = "Không thể phân tích ảnh. Vui lòng thử lại."
                    };
                }

                // 5. Parse JSON từ Gemini và validate
                JsonElement faceJson;
                try
                {
                    faceJson = JsonSerializer.Deserialize<JsonElement>(encoding);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [ĐĂNG KÝ FACE] Lỗi parse JSON từ Gemini");
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = "Lỗi phân tích ảnh từ AI. Vui lòng thử lại."
                    };
                }

                // 5.1. Kiểm tra có khuôn mặt không
                if (!faceJson.TryGetProperty("hasFace", out var hasFace) || !hasFace.GetBoolean())
                {
                    _logger.LogWarning($"⚠️ [ĐĂNG KÝ FACE] Gemini không phát hiện khuôn mặt trong ảnh - NV #{nvHoSoId}");
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = "❌ Không phát hiện khuôn mặt trong ảnh. Vui lòng:\n• Đảm bảo khuôn mặt nằm trong khung hình\n• Ánh sáng đủ sáng\n• Camera rõ nét"
                    };
                }

                // 5.2. Kiểm tra anti-spoofing (ảnh thật hay giả)
                if (faceJson.TryGetProperty("isRealPerson", out var isReal) && !isReal.GetBoolean())
                {
                    _logger.LogWarning($"🚫 [ĐĂNG KÝ FACE - ANTI-SPOOF] Phát hiện ảnh giả mạo - NV #{nvHoSoId}");
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = "🚫 Phát hiện ảnh giả mạo!\n• Không được chụp ảnh từ màn hình\n• Không được dùng ảnh in\n• Vui lòng chụp trực tiếp khuôn mặt"
                    };
                }

                // 5.3. Lấy quality từ JSON (thay vì gọi API lần 2)
                double quality = 0.5;
                if (faceJson.TryGetProperty("quality", out var qualityProp))
                {
                    quality = qualityProp.GetDouble();
                }

                if (quality < 0.5)
                {
                    _logger.LogWarning($"⚠️ [ĐĂNG KÝ FACE] Chất lượng ảnh thấp ({quality:F2}) - NV #{nvHoSoId}");
                    return new RegisterFaceResponseDto
                    {
                        Success = false,
                        Message = $"Chất lượng ảnh quá thấp ({quality:P0}). Vui lòng:\n• Chụp ở nơi có ánh sáng tốt\n• Camera rõ nét, không bị mờ\n• Khuôn mặt đối diện camera",
                        QualityScore = (decimal)quality
                    };
                }

                _logger.LogInformation($"✅ [ĐĂNG KÝ FACE] Gemini phân tích thành công - NV #{nvHoSoId} (Quality: {quality:F2})");

                // 5.4. CHECK DUPLICATE: Khuôn mặt này đã đăng ký cho nhân viên khác chưa?
                // Lấy tất cả face data của các nhân viên KHÁC (không phải nvHoSoId hiện tại)
                var otherEmployeeFaces = await _context.NvFaceDatas
                    .Include(f => f.NhanVien)
                    .Where(x => x.NvHoSoId != nvHoSoId && x.IsActive)
                    .ToListAsync();

                _logger.LogInformation($"🔍 [DUPLICATE CHECK] Đang kiểm tra {otherEmployeeFaces.Count} khuôn mặt của nhân viên khác...");

                // So sánh với từng face của nhân viên khác
                var duplicateThreshold = 0.70; // Ngưỡng nhận diện trùng lặp (70%) - Giảm từ 75% để chặt chẽ hơn
                foreach (var otherFace in otherEmployeeFaces)
                {
                    if (string.IsNullOrEmpty(otherFace.FaceEncoding)) continue;

                    var similarity = _faceService.CompareEncodings(encoding, otherFace.FaceEncoding);
                    
                    // Log tất cả các similarity để debug
                    if (similarity > 0.5) // Chỉ log nếu similarity > 50%
                    {
                        _logger.LogInformation($"📊 [DUPLICATE CHECK] So sánh với NV #{otherFace.NvHoSoId} ({otherFace.NhanVien?.HoTen}): {similarity:P0}");
                    }

                    if (similarity >= duplicateThreshold)
                    {
                        var otherEmployee = otherFace.NhanVien;
                        _logger.LogWarning($"🚫 [ĐĂNG KÝ FACE - DUPLICATE] Khuôn mặt đã đăng ký cho NV #{otherFace.NvHoSoId} ({otherEmployee?.HoTen}) - Độ khớp: {similarity:P0}");
                        return new RegisterFaceResponseDto
                        {
                            Success = false,
                            Message = $" Khuôn mặt này đã được đăng ký cho nhân viên khác!\n\n" +
                                    $"• Nhân viên: {otherEmployee?.HoTen}\n" +
                                    $"• Mã NV: {otherEmployee?.MaNhanVien}\n" +
                                    $"• Độ trùng khớp: {similarity:P0}\n\n" +
                                    $"Vui lòng kiểm tra lại hoặc liên hệ HR nếu đây là nhầm lẫn."
                        };
                    }
                }

                _logger.LogInformation($" [ĐĂNG KÝ FACE] Không phát hiện trùng lặp với nhân viên khác - NV #{nvHoSoId}");

                // 5.5. VALIDATION: Ảnh 2-3 phải giống ảnh gốc >= 70-80%
                double? similarityWithFirst = null;
                if (imageNumber > 1 && existingFaces.Any())
                {
                    var firstFace = existingFaces.First(); // Ảnh gốc (ảnh 1)
                    similarityWithFirst = _faceService.CompareEncodings(encoding, firstFace.FaceEncoding);

                    var minSimilarity = 0.70; // Yêu cầu giống >= 70%
                    if (similarityWithFirst < minSimilarity)
                    {
                        _logger.LogWarning($" [ĐĂNG KÝ FACE] Ảnh #{imageNumber} không giống ảnh gốc đủ (Độ khớp: {similarityWithFirst:P0}, Yêu cầu: {minSimilarity:P0}) - NV #{nvHoSoId}");
                        return new RegisterFaceResponseDto
                        {
                            Success = false,
                            Message = $"❌ Ảnh #{imageNumber} không giống ảnh gốc (ảnh 1).\n" +
                                    $"• Độ giống hiện tại: {similarityWithFirst:P0}\n" +
                                    $"• Yêu cầu tối thiểu: {minSimilarity:P0}\n\n" +
                                    $"Vui lòng chụp lại ảnh của cùng một người!",
                            QualityScore = (decimal)similarityWithFirst.Value
                        };
                    }

                    _logger.LogInformation($" [ĐĂNG KÝ FACE] Ảnh #{imageNumber} giống ảnh gốc {similarityWithFirst:P0} (>= {minSimilarity:P0})");
                }

                // 5.6. Log thông tin đặc điểm khuôn mặt (debug)
                if (faceJson.TryGetProperty("faceFeatures", out var features))
                {
                    _logger.LogDebug($" [ĐĂNG KÝ FACE] Đặc điểm: {features}");
                }

                // 6. Lưu file ảnh
                stream.Position = 0;
                var fileName = $"{nvHoSoId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
                var (savedPath, thumbnailPath) = await SaveImageAsync(stream, fileName);

                // 7. Lưu vào DB với số thứ tự
                var faceData = new NvFaceData
                {
                    NvHoSoId = nvHoSoId,
                    FaceEncoding = encoding,
                    FaceImageUrl = savedPath,
                    FaceImageThumbnail = thumbnailPath,
                    QualityScore = (decimal)quality,
                    IsActive = true,
                    CreatedBy = createdBy,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.NvFaceDatas.Add(faceData);
                await _context.SaveChangesAsync();

                // ✅ Invalidate cache để IdentifyEmployeeAsync load face mới
                _faceService.InvalidateFaceCache();

                await _auditLog.LogActionAsync(
                    taiKhoanId: createdBy,
                    bang: "Dữ liệu khuôn mặt",
                    doiTuongId: faceData.Id,
                    tenDoiTuong: nv.HoTen,
                    hanhDong: "Đăng ký khuôn mặt",
                    ghiChu: $"Đăng ký khuôn mặt - Ảnh #{imageNumber}/{maxFaces} - Quality: {faceData.QualityScore:F2}");

                // Message với similarity đã tính sẵn
                var message = imageNumber == 1
                    ? $"✅ Đăng ký ảnh gốc (ảnh {imageNumber}/{maxFaces}) thành công!"
                    : $"✅ Đăng ký ảnh {imageNumber}/{maxFaces} thành công! (Độ giống với ảnh gốc: {similarityWithFirst:P0})";

                return new RegisterFaceResponseDto
                {
                    Success = true,
                    Message = message,
                    FaceId = faceData.Id,
                    QualityScore = (decimal)quality,
                    ImageNumber = imageNumber
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registering face for employee {nvHoSoId}");
                return new RegisterFaceResponseDto
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Lấy danh sách nhân viên đã đăng ký face
        /// </summary>
        public async Task<List<FaceDataDto>> GetRegisteredEmployeesAsync()
        {
            var result = await _context.NvFaceDatas
                .Where(x => x.IsActive)
                .Include(x => x.NhanVien)
                .GroupBy(x => x.NvHoSoId)
                .Select(g => new FaceDataDto
                {
                    NvHoSoId = g.Key,
                    TenNhanVien = g.First().NhanVien!.HoTen,
                    MaNhanVien = g.First().NhanVien!.MaNhanVien,
                    SoLuongAnh = g.Count(),
                    FaceImageUrl = g.First().FaceImageUrl,
                    ChatLuongTrungBinh = g.Average(x => x.QualityScore),
                    NgayDangKy = g.Min(x => x.CreatedAt),
                    IsActive = true
                })
                .OrderBy(x => x.TenNhanVien)
                .ToListAsync();

            // Map relative paths to full URLs
            foreach (var item in result)
            {
                item.FaceImageUrl = GetFullUrl(item.FaceImageUrl);
            }

            return result;
        }

        /// <summary>
        /// Lấy chi tiết face data của 1 nhân viên
        /// </summary>
        public async Task<List<FaceDataDto>> GetEmployeeFaceDataAsync(int nvHoSoId)
        {
            var result = await _context.NvFaceDatas
                .Where(x => x.NvHoSoId == nvHoSoId && x.IsActive)
                .Include(x => x.NhanVien)
                .Select(x => new FaceDataDto
                {
                    Id = x.Id,
                    NvHoSoId = x.NvHoSoId,
                    TenNhanVien = x.NhanVien!.HoTen,
                    MaNhanVien = x.NhanVien!.MaNhanVien,
                    FaceImageUrl = x.FaceImageUrl,
                    QualityScore = x.QualityScore,
                    NgayDangKy = x.CreatedAt,
                    IsActive = x.IsActive
                })
                .OrderByDescending(x => x.NgayDangKy)
                .ToListAsync();

            // Map relative paths to full URLs
            foreach (var item in result)
            {
                item.FaceImageUrl = GetFullUrl(item.FaceImageUrl);
            }

            return result;
        }

        /// <summary>
        /// Xóa dữ liệu khuôn mặt (soft delete)
        /// </summary>
        public async Task<bool> DeleteFaceDataAsync(int faceId, int deletedBy)
        {
            var faceData = await _context.NvFaceDatas.FindAsync(faceId);
            if (faceData == null) return false;

            faceData.IsActive = false;
            faceData.UpdatedAt = DateTime.Now;
            faceData.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            // ✅ Invalidate cache để IdentifyEmployeeAsync không dùng face đã xóa
            _faceService.InvalidateFaceCache();

            await _auditLog.LogActionAsync(
                taiKhoanId: deletedBy,
                bang: "Dữ liệu khuôn mặt",
                doiTuongId: faceId,
                tenDoiTuong: faceData.NhanVien?.HoTen,
                hanhDong: "Xóa dữ liệu khuôn mặt",
                ghiChu: "Xóa dữ liệu khuôn mặt");

            return true;
        }

        /// <summary>
        /// Xóa ảnh khuôn mặt nhưng kiểm tra quyền sở hữu (dành cho EMPLOYEE)
        /// </summary>
        public async Task<(bool found, bool authorized)> DeleteFaceDataIfOwnerAsync(int faceId, int deletedBy, int nvHoSoId)
        {
            var faceData = await _context.NvFaceDatas
                .FirstOrDefaultAsync(x => x.Id == faceId && x.IsActive);

            if (faceData == null) return (false, false);
            if (faceData.NvHoSoId != nvHoSoId) return (true, false);

            faceData.IsActive = false;
            faceData.UpdatedAt = DateTime.Now;
            faceData.UpdatedBy = deletedBy;

            await _context.SaveChangesAsync();

            // ✅ Invalidate cache để IdentifyEmployeeAsync không dùng face đã xóa
            _faceService.InvalidateFaceCache();

            await _auditLog.LogActionAsync(
                taiKhoanId: deletedBy,
                bang: "Dữ liệu khuôn mặt",
                doiTuongId: faceId,
                tenDoiTuong: faceData.NhanVien?.HoTen,
                hanhDong: "Xóa dữ liệu khuôn mặt",
                ghiChu: "Nhân viên tự xóa dữ liệu khuôn mặt");

            return (true, true);
        }

        /// <summary>
        /// Xóa tất cả face data của nhân viên
        /// </summary>
        public async Task<bool> DeleteAllFaceDataAsync(int nvHoSoId, int deletedBy)
        {
            var faceDatas = await _context.NvFaceDatas
                .Where(x => x.NvHoSoId == nvHoSoId && x.IsActive)
                .ToListAsync();

            if (!faceDatas.Any()) return false;

            foreach (var face in faceDatas)
            {
                face.IsActive = false;
                face.UpdatedAt = DateTime.Now;
                face.UpdatedBy = deletedBy;
            }

            await _context.SaveChangesAsync();

            // ✅ Invalidate cache để IdentifyEmployeeAsync không dùng faces đã xóa
            _faceService.InvalidateFaceCache();

            await _auditLog.LogActionAsync(
                taiKhoanId: deletedBy,
                bang: "Dữ liệu khuôn mặt",
                doiTuongId: nvHoSoId,
                tenDoiTuong: faceDatas.First().NhanVien?.HoTen,
                hanhDong: "XÓA_TẤT_CẢ_FACE",
                ghiChu: $"Xóa {faceDatas.Count} ảnh khuôn mặt");

            return true;
        }

        // ==================== PRIVATE METHODS ====================

        private bool IsValidImageFile(IFormFile file)
        {
            // Check size
            var maxSizeMB = int.Parse(_config["FaceRecognition:ImageSettings:MaxSizeMB"] ?? "5");
            if (file.Length > maxSizeMB * 1024 * 1024)
                return false;

            // Check extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(extension))
                return false;

            return true;
        }

        private async Task<(string savedPath, string thumbnailPath)> SaveImageAsync(Stream imageStream, string fileName)
        {
            // Tạo thư mục nếu chưa có
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "faces", "registered");
            Directory.CreateDirectory(uploadDir);

            var thumbDir = Path.Combine(_env.WebRootPath, "uploads", "faces", "thumbnails");
            Directory.CreateDirectory(thumbDir);

            // Load image
            using var image = Image.Load<Rgba32>(imageStream);

            // Save original (resize nếu quá lớn)
            if (image.Width > 800 || image.Height > 800)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(800, 800),
                    Mode = ResizeMode.Max
                }));
            }

            var savePath = Path.Combine(uploadDir, fileName);
            await image.SaveAsync(savePath, new JpegEncoder { Quality = 85 });

            // Create thumbnail
            var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
            {
                Size = new Size(200, 200),
                Mode = ResizeMode.Crop
            }));

            var thumbPath = Path.Combine(thumbDir, fileName);
            await thumbnail.SaveAsync(thumbPath, new JpegEncoder { Quality = 75 });

            // Return relative paths
            return (
                $"/uploads/faces/registered/{fileName}",
                $"/uploads/faces/thumbnails/{fileName}"
            );
        }

        /// <summary>
        /// Chuyển đổi relative path thành full URL
        /// </summary>
        private string? GetFullUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return null;

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
                return relativePath;

            var scheme = request.Scheme; // http hoặc https
            var host = request.Host.Value; // localhost:5000 hoặc domain

            return $"{scheme}://{host}{relativePath}";
        }
    }
}
