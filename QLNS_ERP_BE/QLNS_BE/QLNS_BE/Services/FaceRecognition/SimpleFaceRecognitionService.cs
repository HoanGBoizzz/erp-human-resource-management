using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace QLNS_BE.Services.FaceRecognition
{
    /// <summary>
    /// Simple Face Recognition Service - Phase 1
    /// Sử dụng perceptual hash để so sánh ảnh
    /// Sau này có thể replace bằng Python Face Recognition API hoặc Dlib
    /// </summary>
    public class SimpleFaceRecognitionService : IFaceRecognitionService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<SimpleFaceRecognitionService> _logger;
        private readonly IHttpClientFactory? _httpClientFactory;

        public SimpleFaceRecognitionService(
            AppDbContext context,
            IConfiguration config,
            ILogger<SimpleFaceRecognitionService> logger,
            IHttpClientFactory? httpClientFactory = null)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Trích xuất face encoding từ ảnh
        /// Phase 1: Dùng perceptual hash (simple)
        /// Phase 2: Call Python Face Recognition API
        /// </summary>
        public async Task<string?> ExtractFaceEncodingAsync(Stream imageStream)
        {
            try
            {
                // Kiểm tra xem có Python API không
                var pythonApiUrl = _config["FaceRecognition:PythonApiUrl"];

                if (!string.IsNullOrEmpty(pythonApiUrl) && _httpClientFactory != null)
                {
                    // Call Python API (nếu có)
                    return await CallPythonApiForEncodingAsync(imageStream, pythonApiUrl);
                }
                else
                {
                    // Fallback: Dùng simple perceptual hash
                    _logger.LogWarning("Python API not configured. Using simple hash method.");
                    return await GenerateSimpleHashAsync(imageStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting face encoding");
                return null;
            }
        }

        /// <summary>
        /// So sánh 2 encodings
        /// </summary>
        public double CompareEncodings(string encoding1, string encoding2)
        {
            try
            {
                // Nếu là hash đơn giản, so sánh Hamming distance
                if (encoding1.Length == encoding2.Length && encoding1.Length <= 64)
                {
                    return CalculateHashSimilarity(encoding1, encoding2);
                }

                // Nếu là JSON array (từ Python API)
                var arr1 = JsonSerializer.Deserialize<double[]>(encoding1);
                var arr2 = JsonSerializer.Deserialize<double[]>(encoding2);

                if (arr1 != null && arr2 != null && arr1.Length == arr2.Length)
                {
                    // Euclidean distance
                    double distance = CalculateEuclideanDistance(arr1, arr2);
                    // Convert to similarity score (0-1)
                    return Math.Max(0, 1 - distance);
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Tìm nhân viên từ ảnh
        /// </summary>
        public async Task<(int? nvId, double? confidence)> IdentifyEmployeeAsync(Stream imageStream)
        {
            try
            {
                // 1. Extract encoding
                var encoding = await ExtractFaceEncodingAsync(imageStream);
                if (string.IsNullOrEmpty(encoding))
                {
                    return (null, null);
                }

                // 2. Lấy tất cả face data đang active
                var allFaces = await _context.NvFaceDatas
                    .Where(x => x.IsActive)
                    .ToListAsync();

                if (!allFaces.Any())
                {
                    _logger.LogWarning("No active face data found in database");
                    return (null, null);
                }

                // 3. So sánh với từng face
                double maxSimilarity = 0;
                int? matchedNvId = null;

                foreach (var face in allFaces)
                {
                    var similarity = CompareEncodings(encoding, face.FaceEncoding);
                    if (similarity > maxSimilarity)
                    {
                        maxSimilarity = similarity;
                        matchedNvId = face.NvHoSoId;
                    }
                }

                // 4. Always return best candidate + similarity (caller decides threshold)
                return (matchedNvId, maxSimilarity > 0 ? maxSimilarity : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error identifying employee");
                return (null, null);
            }
        }

        /// <summary>
        /// Kiểm tra ảnh có khuôn mặt không
        /// </summary>
        public async Task<bool> HasFaceAsync(Stream imageStream)
        {
            try
            {
                using var image = await Image.LoadAsync(imageStream);

                // Simple check: ảnh phải đủ lớn và tỷ lệ hợp lý
                if (image.Width < 100 || image.Height < 100)
                    return false;

                double ratio = (double)image.Width / image.Height;
                if (ratio < 0.5 || ratio > 2.0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Đánh giá chất lượng ảnh
        /// </summary>
        public async Task<double> EvaluateImageQualityAsync(Stream imageStream)
        {
            try
            {
                using var image = await Image.LoadAsync(imageStream);

                double qualityScore = 1.0;

                // Kiểm tra kích thước
                if (image.Width < 200 || image.Height < 200)
                    qualityScore -= 0.3;

                // Kiểm tra tỷ lệ
                double ratio = (double)image.Width / image.Height;
                if (ratio < 0.7 || ratio > 1.5)
                    qualityScore -= 0.2;

                // Kiểm tra độ phân giải
                if (image.Width * image.Height < 100000) // < 100K pixels
                    qualityScore -= 0.2;

                return Math.Max(0, Math.Min(1, qualityScore));
            }
            catch
            {
                return 0.3; // Low quality by default
            }
        }

        // ==================== PRIVATE METHODS ====================

        /// <summary>
        /// Generate simple perceptual hash (không cần ML)
        /// </summary>
        private async Task<string> GenerateSimpleHashAsync(Stream imageStream)
        {
            using var image = Image.Load<Rgba32>(imageStream);

            // Resize to 8x8 for perceptual hash
            image.Mutate(x => x.Resize(8, 8));

            // Convert to grayscale and get average
            var pixels = new List<byte>();
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        var pixel = pixelRow[x];
                        // Simple grayscale
                        byte gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                        pixels.Add(gray);
                    }
                }
            });

            // Calculate average
            double average = pixels.Average(p => p);

            // Create hash
            var hashBits = new StringBuilder();
            foreach (var pixel in pixels)
            {
                hashBits.Append(pixel >= average ? '1' : '0');
            }

            return hashBits.ToString();
        }

        /// <summary>
        /// Call Python Face Recognition API (nếu có)
        /// </summary>
        private async Task<string?> CallPythonApiForEncodingAsync(Stream imageStream, string apiUrl)
        {
            try
            {
                var client = _httpClientFactory!.CreateClient("FaceRecognition");

                using var content = new MultipartFormDataContent();
                var imageContent = new StreamContent(imageStream);
                imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
                content.Add(imageContent, "image", "face.jpg");

                var response = await client.PostAsync($"{apiUrl}/extract-encoding", content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return result;
                }

                _logger.LogWarning($"Python API returned {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Python API");
                return null;
            }
        }

        /// <summary>
        /// Calculate similarity giữa 2 hash strings
        /// </summary>
        private double CalculateHashSimilarity(string hash1, string hash2)
        {
            int differentBits = 0;
            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                    differentBits++;
            }

            // Convert to similarity (0-1)
            return 1.0 - ((double)differentBits / hash1.Length);
        }

        /// <summary>
        /// Calculate Euclidean distance
        /// </summary>
        private double CalculateEuclideanDistance(double[] arr1, double[] arr2)
        {
            double sum = 0;
            for (int i = 0; i < arr1.Length; i++)
            {
                double diff = arr1[i] - arr2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        /// <summary>
        /// Xác minh khuôn mặt có thuộc về nhân viên chỉ định không
        /// </summary>
        public async Task<(bool isMatch, double? confidence)> VerifyEmployeeFaceAsync(int nvHoSoId, Stream imageStream)
        {
            try
            {
                // Extract encoding từ ảnh mới
                var encoding = await ExtractFaceEncodingAsync(imageStream);
                if (string.IsNullOrEmpty(encoding))
                {
                    return (false, null);
                }

                // Lấy tất cả face data của nhân viên này
                var employeeFaces = await _context.NvFaceDatas
                    .Where(x => x.NvHoSoId == nvHoSoId && x.IsActive)
                    .Select(x => x.FaceEncoding)
                    .ToListAsync();

                if (!employeeFaces.Any())
                {
                    _logger.LogWarning($"⚠️ Nhân viên #{nvHoSoId} chưa đăng ký khuôn mặt");
                    return (false, null);
                }

                // So sánh với tất cả faces của nhân viên, lấy best match
                double bestSimilarity = 0;
                foreach (var faceEncoding in employeeFaces)
                {
                    var similarity = CompareEncodings(encoding, faceEncoding);
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                    }
                }

                var threshold = GetConfidenceThreshold();
                var isMatch = bestSimilarity >= threshold;

                _logger.LogInformation($"✅ [VERIFY] Nhân viên #{nvHoSoId}: {(isMatch ? "KHỚP" : "KHÔNG KHỚP")} (Độ khớp: {bestSimilarity:P0})");

                return (isMatch, bestSimilarity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Lỗi khi verify face cho nhân viên #{nvHoSoId}");
                return (false, null);
            }
        }

        /// <summary>
        /// Lấy threshold từ config
        /// </summary>
        private double GetConfidenceThreshold()
        {
            var thresholdStr = _config["FaceRecognition:ConfidenceThreshold"] ?? "0.5";
            return double.TryParse(thresholdStr, out var threshold) ? threshold : 0.5;
        }

        /// <summary>
        /// Invalidate face cache (no-op for simple service, chỉ InsightFace cần)
        /// </summary>
        public void InvalidateFaceCache()
        {
            // No cache in simple service - do nothing
            _logger.LogDebug("InvalidateFaceCache called (no-op for SimpleFaceRecognitionService)");
        }
    }
}
