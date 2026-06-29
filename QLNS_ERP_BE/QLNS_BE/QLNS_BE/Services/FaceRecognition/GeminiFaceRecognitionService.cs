using Microsoft.EntityFrameworkCore;
using QLNS.ERP.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QLNS_BE.Services.FaceRecognition
{
    /// <summary>
    /// Gemini-powered Face Recognition Service
    /// Sử dụng Gemini Vision API để nhận diện khuôn mặt và anti-spoofing
    /// </summary>
    public class GeminiFaceRecognitionService : IFaceRecognitionService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiFaceRecognitionService> _logger;
        private readonly HttpClient _httpClient;

        public GeminiFaceRecognitionService(
            AppDbContext context,
            IConfiguration config,
            ILogger<GeminiFaceRecognitionService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _config = config;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("Gemini");
        }

        /// <summary>
        /// Trích xuất face encoding từ ảnh
        /// Sử dụng Gemini Vision để mô tả khuôn mặt dưới dạng vector đặc trưng
        /// </summary>
        public async Task<string?> ExtractFaceEncodingAsync(Stream imageStream)
        {
            try
            {
                _logger.LogInformation("🔍 [GEMINI AI] Bắt đầu phân tích khuôn mặt...");

                // Chuyển ảnh sang base64
                var imageBytes = await ReadStreamAsync(imageStream);
                var base64Image = Convert.ToBase64String(imageBytes);
                _logger.LogDebug($"📦 [GEMINI AI] Đã chuyển ảnh sang base64 ({imageBytes.Length} bytes)");

                var apiKey = _config["Gemini:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("❌ [GEMINI AI] API Key không được cấu hình");
                    return null;
                }

                // Prompt đơn giản, rõ ràng
                var prompt = "Return JSON only (no markdown): {\"hasFace\":true/false,\"isRealPerson\":true/false,\"faceFeatures\":{\"eyeDistance\":120,\"noseWidth\":40,\"mouthWidth\":65,\"faceShape\":\"oval\",\"skinTone\":\"medium\",\"age\":25,\"gender\":\"male\"},\"quality\":0.8,\"fingerprint\":\"desc\"}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = prompt },
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = "image/jpeg",
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.0,
                        maxOutputTokens = 1024,  // TĂNG lên để tránh bị cắt JSON
                        stopSequences = new[] { "\n}\n" }  // Dừng khi JSON hoàn thành
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogInformation("🌐 [GEMINI AI] Đang gọi Gemini API...");

                var model = _config["Gemini:Model"] ?? "gemini-1.5-flash";
                var apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                _logger.LogDebug($"📡 [GEMINI API] URL: {apiUrl.Replace(apiKey, "***")}");

                var response = await _httpClient.PostAsync(apiUrl, content);

                var responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ [GEMINI API] Lỗi HTTP {response.StatusCode}");
                    _logger.LogError($"❌ [GEMINI API] Response: {responseText}");
                    return null;
                }

                _logger.LogInformation("✅ [GEMINI AI] API trả về thành công");
                _logger.LogDebug($"📄 [GEMINI API] RAW Response: {responseText}");

                var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseText);

                // Parse response từ Gemini
                if (geminiResponse.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var candidate = candidates[0];

                    // KIỂM TRA finishReason trước
                    if (candidate.TryGetProperty("finishReason", out var finishReason))
                    {
                        var reason = finishReason.GetString();
                        if (reason == "MAX_TOKENS")
                        {
                            _logger.LogError("❌ [GEMINI AI] Response bị cắt do MAX_TOKENS - cần tăng maxOutputTokens");
                            _logger.LogError($"❌ [GEMINI API] Full response: {responseText}");
                            return null;
                        }
                    }

                    if (candidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                    {
                        var textPart = parts[0];
                        if (textPart.TryGetProperty("text", out var textElement))
                        {
                            var text = textElement.GetString();

                            // Loại bỏ markdown và khoảng trắng
                            text = text?.Replace("```json", "").Replace("```", "").Trim();

                            _logger.LogInformation($"✅ [GEMINI AI] Đã trích xuất text ({text?.Length ?? 0} chars)");
                            _logger.LogDebug($"📊 [GEMINI AI] Parsed JSON: {text}");

                            // Validate JSON trước khi return
                            try
                            {
                                var testParse = JsonSerializer.Deserialize<JsonElement>(text ?? "{}");
                                if (!testParse.TryGetProperty("hasFace", out _))
                                {
                                    _logger.LogWarning($"⚠️ [GEMINI AI] JSON thiếu field 'hasFace': {text}");
                                    return null;
                                }

                                _logger.LogInformation("✅ [GEMINI AI] JSON hợp lệ, có đầy đủ fields");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"❌ [GEMINI AI] JSON không hợp lệ: {text}");
                                return null;
                            }

                            return text;
                        }
                    }
                }

                _logger.LogWarning("⚠️ [GEMINI AI] Không parse được response từ Gemini");
                _logger.LogWarning($"⚠️ [GEMINI API] Response structure: {responseText}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi trích xuất face encoding từ Gemini");
                return null;
            }
        }

        /// <summary>
        /// So sánh 2 encodings (JSON từ Gemini)
        /// </summary>
        public double CompareEncodings(string encoding1, string encoding2)
        {
            try
            {
                var face1 = JsonSerializer.Deserialize<JsonElement>(encoding1);
                var face2 = JsonSerializer.Deserialize<JsonElement>(encoding2);

                if (!face1.TryGetProperty("faceFeatures", out var features1) ||
                    !face2.TryGetProperty("faceFeatures", out var features2))
                {
                    return 0;
                }

                // So sánh các đặc điểm
                double score = 0;
                int count = 0;

                // So sánh khoảng cách mắt
                if (TryGetDouble(features1, "eyeDistance", out var eye1) &&
                    TryGetDouble(features2, "eyeDistance", out var eye2))
                {
                    score += 1.0 - Math.Min(Math.Abs(eye1 - eye2) / Math.Max(eye1, eye2), 1.0);
                    count++;
                }

                // So sánh độ rộng mũi
                if (TryGetDouble(features1, "noseWidth", out var nose1) &&
                    TryGetDouble(features2, "noseWidth", out var nose2))
                {
                    score += 1.0 - Math.Min(Math.Abs(nose1 - nose2) / Math.Max(nose1, nose2), 1.0);
                    count++;
                }

                // So sánh độ rộng miệng
                if (TryGetDouble(features1, "mouthWidth", out var mouth1) &&
                    TryGetDouble(features2, "mouthWidth", out var mouth2))
                {
                    score += 1.0 - Math.Min(Math.Abs(mouth1 - mouth2) / Math.Max(mouth1, mouth2), 1.0);
                    count++;
                }

                // So sánh hình dạng khuôn mặt
                if (TryGetString(features1, "faceShape", out var shape1) &&
                    TryGetString(features2, "faceShape", out var shape2))
                {
                    score += shape1 == shape2 ? 1.0 : 0.5;
                    count++;
                }

                // So sánh màu da
                if (TryGetString(features1, "skinTone", out var skin1) &&
                    TryGetString(features2, "skinTone", out var skin2))
                {
                    score += skin1 == skin2 ? 1.0 : 0.3;
                    count++;
                }

                // So sánh tuổi
                if (TryGetDouble(features1, "age", out var age1) &&
                    TryGetDouble(features2, "age", out var age2))
                {
                    var ageDiff = Math.Abs(age1 - age2);
                    score += ageDiff <= 5 ? 1.0 : Math.Max(0, 1.0 - (ageDiff / 20.0));
                    count++;
                }

                // So sánh giới tính
                if (TryGetString(features1, "gender", out var gender1) &&
                    TryGetString(features2, "gender", out var gender2))
                {
                    score += gender1 == gender2 ? 1.0 : 0;
                    count++;
                }

                return count > 0 ? score / count : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi so sánh encodings");
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

                var faceData = JsonSerializer.Deserialize<JsonElement>(encoding);

                // Kiểm tra có khuôn mặt không
                if (!faceData.TryGetProperty("hasFace", out var hasFace) || !hasFace.GetBoolean())
                {
                    _logger.LogWarning("⚠️ [GEMINI AI] Không phát hiện khuôn mặt trong ảnh");
                    return (null, null);
                }
                _logger.LogInformation("✅ [GEMINI AI] Đã phát hiện khuôn mặt");

                // Kiểm tra anti-spoof
                if (faceData.TryGetProperty("isRealPerson", out var isReal) && !isReal.GetBoolean())
                {
                    _logger.LogWarning("🚫 [GEMINI AI - ANTI-SPOOF] Phát hiện ảnh giả mạo (chụp từ màn hình hoặc ảnh in)");
                    return (null, null);
                }
                _logger.LogInformation("✅ [GEMINI AI - ANTI-SPOOF] Ảnh hợp lệ (không phải giả mạo)");

                // 2. Lấy tất cả face data đang active
                var allFaces = await _context.NvFaceDatas
                    .Where(x => x.IsActive)
                    .ToListAsync();

                if (!allFaces.Any())
                {
                    _logger.LogWarning("Không có dữ liệu khuôn mặt nào trong database");
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

                if (matchedNvId != null)
                {
                    _logger.LogInformation($"✅ [GEMINI AI] Nhận diện thành công - NV #{matchedNvId} (Độ khớp: {maxSimilarity:P0})");
                }
                else
                {
                    _logger.LogWarning($"⚠️ [GEMINI AI] Không khớp với ai (Điểm cao nhất: {maxSimilarity:P0})");
                }

                return (matchedNvId, maxSimilarity > 0 ? maxSimilarity : null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhận diện nhân viên");
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
                var encoding = await ExtractFaceEncodingAsync(imageStream);
                if (string.IsNullOrEmpty(encoding))
                {
                    return false;
                }

                var faceData = JsonSerializer.Deserialize<JsonElement>(encoding);
                return faceData.TryGetProperty("hasFace", out var hasFace) && hasFace.GetBoolean();
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
                var encoding = await ExtractFaceEncodingAsync(imageStream);
                if (string.IsNullOrEmpty(encoding))
                {
                    return 0.3;
                }

                var faceData = JsonSerializer.Deserialize<JsonElement>(encoding);
                if (faceData.TryGetProperty("quality", out var quality))
                {
                    return quality.GetDouble();
                }

                return 0.5;
            }
            catch
            {
                return 0.3;
            }
        }

        // Helper methods
        private async Task<byte[]> ReadStreamAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }

        private bool TryGetDouble(JsonElement element, string propertyName, out double value)
        {
            value = 0;
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                {
                    value = prop.GetDouble();
                    return true;
                }
            }
            return false;
        }

        private bool TryGetString(JsonElement element, string propertyName, out string value)
        {
            value = string.Empty;
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.String)
                {
                    value = prop.GetString() ?? string.Empty;
                    return true;
                }
            }
            return false;
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
        /// Invalidate face cache (no-op for Gemini service)
        /// </summary>
        public void InvalidateFaceCache()
        {
            // No cache in Gemini service - do nothing
            _logger.LogDebug("InvalidateFaceCache called (no-op for GeminiFaceRecognitionService)");
        }
    }
}
