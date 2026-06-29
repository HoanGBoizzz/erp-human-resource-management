# PATCH: Face Registration Validation

## File: `QLNS_BE/Services/FaceDataService.cs`

### Tìm dòng (~line 155):

```csharp
_logger.LogInformation($"✅ [ĐĂNG KÝ FACE] Gemini phân tích thành công - NV #{nvHoSoId} (Quality: {quality:F2})");
// 5.4. Log thông tin đặc điểm khuôn mặt (debug)
if (faceJson.TryGetProperty("faceFeatures", out var features))
{
    _logger.LogDebug($"📊 [ĐĂNG KÝ FACE] Đặc điểm: {features}");
}
```

### Thay thế bằng:

```csharp
_logger.LogInformation($"✅ [ĐĂNG KÝ FACE] InsightFace phân tích thành công - NV #{nvHoSoId} (Quality: {quality:F2}, Ảnh #{imageNumber})");

// 5.4. VALIDATION: Ảnh 2-3 phải giống ảnh gốc (ảnh 1) >= 78-80%
if (imageNumber > 1 && existingFaces.Any())
{
    var firstFace = existingFaces.First(); // Ảnh gốc
    var similarity = _faceService.CompareEncodings(encoding, firstFace.FaceEncoding);
    
    var minSimilarity = 0.78; // Yêu cầu giống >= 78%
    if (similarity < minSimilarity)
    {
        _logger.LogWarning($"⚠️ [ĐĂNG KÝ FACE] Ảnh #{imageNumber} không giống ảnh gốc đủ - NV #{nvHoSoId} (Độ giống: {similarity:P0})");
        return new RegisterFaceResponseDto
        {
            Success = false,
            Message = $"❌ Ảnh #{imageNumber} không giống ảnh gốc (ảnh 1).\\n" +
                    $"• Độ giống hiện tại: {similarity:P0}\\n" +
                    $"• Yêu cầu tối thiểu: {minSimilarity:P0}\\n" +
                    $"• Vui lòng chụp cùng 1 người với ảnh đầu tiên",
            QualityScore = (decimal)similarity
        };
    }
    _logger.LogInformation($"✅ [ĐĂNG KÝ FACE] Ảnh #{imageNumber} khớp với ảnh gốc ({similarity:P0}) - NV #{nvHoSoId}");
}
```

## Giải thích:

✅ **Ảnh 1**: Là ảnh gốc, không cần validation
✅ **Ảnh 2-3**: Phải giống ảnh gốc >= 78% mới được chấp nhận
✅ **Message rõ ràng**: Hiển thị độ giống hiện tại vs yêu cầu

## Test:

1. Đăng ký ảnh 1: OK (gốc)
2. Đăng ký ảnh 2 (cùng người): OK nếu giống >= 78%
3. Đăng ký ảnh 2 (khác người): FAIL với message chi tiết
