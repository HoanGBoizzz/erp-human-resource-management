# Hướng dẫn cấu hình Gemini AI cho Face Recognition

## Yêu cầu
- Google Gemini API Key (miễn phí hoặc trả phí)
- Đã cài đặt package `System.Net.Http` trong project

## Các bước cấu hình

### 1. Lấy API Key từ Google AI Studio
1. Truy cập: https://makersuite.google.com/app/apikey
2. Đăng nhập tài khoản Google
3. Tạo API Key mới (chọn project hoặc tạo mới)
4. Copy API Key

### 2. Cấu hình trong Backend

**File: `appsettings.json`**
```json
{
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY_HERE",  // ← Paste API key vào đây
    "Model": "gemini-1.5-flash",
    "MaxTokens": 2048,
    "Temperature": 0.1
  }
}
```

**LƯU Ý QUAN TRỌNG:**
- ❌ **KHÔNG** commit file `appsettings.json` chứa API key lên Git public
- ✅ Nên dùng `appsettings.Development.json` (local) hoặc User Secrets
- ✅ Production: Đặt API key vào Environment Variables

### 3. Cấu hình User Secrets (Khuyến nghị cho Development)

**Bước 1:** Click phải vào project `QLNS_BE` → chọn **"Manage User Secrets"**

**Bước 2:** File `secrets.json` sẽ mở ra, thêm config:
```json
{
  "Gemini": {
    "ApiKey": "AIzaSy..."  // ← Paste API key thật vào đây
  }
}
```

**Bước 3:** Xóa API key trong `appsettings.json`:
```json
{
  "Gemini": {
    "ApiKey": "",  // ← Để trống, lấy từ secrets
    "Model": "gemini-1.5-flash",
    "MaxTokens": 2048,
    "Temperature": 0.1
  }
}
```

### 4. Kiểm tra hoạt động

Backend sẽ tự động chọn và **HIỂN THỊ RÕ RÀNG** service nào đang dùng:

**✅ Nếu có Gemini API Key:**
```
✅ [FACE RECOGNITION] Sử dụng Gemini AI (API Key đã cấu hình)
📌 [FACE RECOGNITION] Model: gemini-1.5-flash
📌 [FACE RECOGNITION] Threshold: 0.6
```

**⚠️ Nếu KHÔNG có API Key:**
```
⚠️  [FACE RECOGNITION] Sử dụng Simple Hash (fallback - không có Gemini API Key)
📌 [FACE RECOGNITION] Threshold: 0.6
💡 [FACE RECOGNITION] Để dùng Gemini AI: Thêm 'Gemini:ApiKey' vào appsettings
```

---

## Cách kiểm tra AI có hoạt động

### Phương pháp 1: Xem Console khi khởi động Backend ⭐ (ĐƠN GIẢN NHẤT)

**Bước 1:** Chạy backend (F5 hoặc `dotnet run`)

**Bước 2:** Xem dòng đầu tiên trong Console:

- ✅ **Thấy dòng này** = Gemini AI đang hoạt động:
  ```
  ✅ [FACE RECOGNITION] Sử dụng Gemini AI (API Key đã cấu hình)
  ```

- ⚠️ **Thấy dòng này** = Chưa có Gemini (đang dùng Simple Hash):
  ```
  ⚠️ [FACE RECOGNITION] Sử dụng Simple Hash (fallback)
  ```

### Phương pháp 2: Gọi API Health Check 🔍

**Bước 1:** Backend đã chạy

**Bước 2:** Mở trình duyệt hoặc Postman, gọi:
```
GET http://localhost:5042/api/face-recognition/health
```

**Bước 3:** Đọc kết quả JSON:

**✅ KHI GEMINI HOẠT ĐỘNG:**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-10T...",
  "faceRecognitionService": {
    "activeService": "GeminiFaceRecognitionService",
    "isGeminiAI": true,
    "isSimpleHash": false,
    "hasGeminiApiKey": true,
    "geminiModel": "gemini-1.5-flash",
    "confidenceThreshold": "0.6",
    "minQualityScore": "0.45"
  },
  "capabilities": {
    "faceDetection": true,
    "antiSpoofing": true,    // ← CHỈ CÓ KHI DÙNG GEMINI
    "qualityAssessment": true,
    "faceMatching": true
  },
  "message": "✅ Đang sử dụng Gemini AI - Nhận diện chính xác + Anti-spoofing"
}
```

**⚠️ KHI CHƯA CÓ GEMINI (Simple Hash):**
```json
{
  "faceRecognitionService": {
    "activeService": "SimpleFaceRecognitionService",
    "isGeminiAI": false,
    "isSimpleHash": true,
    "hasGeminiApiKey": false
  },
  "capabilities": {
    "antiSpoofing": false    // ← KHÔNG CÓ
  },
  "message": "⚠️ Đang sử dụng Simple Hash (fallback)"
}
```

### Phương pháp 3: Xem Log khi Chấm công 📝

Khi nhân viên CHẤM CÔNG, check log trong Console:

**✅ GEMINI AI đang hoạt động:**
```
🔍 [GEMINI AI] Bắt đầu phân tích khuôn mặt...
📦 [GEMINI AI] Đã chuyển ảnh sang base64 (125436 bytes)
🌐 [GEMINI AI] Đang gọi Gemini API...
✅ [GEMINI AI] API trả về thành công
✅ [GEMINI AI] Đã trích xuất đặc điểm khuôn mặt (JSON 842 chars)
✅ [GEMINI AI] Đã phát hiện khuôn mặt
✅ [GEMINI AI - ANTI-SPOOF] Ảnh hợp lệ (không phải giả mạo)
✅ [GEMINI AI] Nhận diện thành công - NV #12 (Độ khớp: 87%)
```

**⚠️ Simple Hash (không có Gemini):**
```
(Không có log [GEMINI AI])
Chỉ có log đơn giản về hash
```

### Phương pháp 4: Test Anti-Spoofing (Chỉ Gemini) 🛡️

**Thử nghiệm:**
1. Chụp ảnh khuôn mặt từ màn hình điện thoại
2. Dùng ảnh đó để chấm công

**✅ NẾU GEMINI HOẠT ĐỘNG:**
```
🚫 [GEMINI AI - ANTI-SPOOF] Phát hiện ảnh giả mạo (chụp từ màn hình hoặc ảnh in)
→ Chấm công THẤT BẠI
```

**⚠️ NẾU SIMPLE HASH (không có Gemini):**
```
→ Chấm công THÀNH CÔNG (vì không có anti-spoof)
```

---

## Tóm tắt: Làm sao biết Gemini có hoạt động?

| Dấu hiệu | Gemini AI ✅ | Simple Hash ⚠️ |
|----------|-------------|----------------|
| **Console khi start** | `✅ Sử dụng Gemini AI` | `⚠️ Sử dụng Simple Hash` |
| **API /health** | `"isGeminiAI": true` | `"isGeminiAI": false` |
| **Log chấm công** | Có `[GEMINI AI]` | Không có |
| **Anti-spoofing** | Chặn ảnh giả | Không chặn |
| **Độ chính xác** | Cao (85-90%) | Thấp (~50%) |

## Tính năng của Gemini Face Recognition

### ✅ Nhận diện chính xác hơn
- Phân tích đặc điểm khuôn mặt chi tiết (mắt, mũi, miệng, hình dạng mặt)
- So sánh dựa trên nhiều yếu tố (tuổi, giới tính, màu da, tóc)
- Độ chính xác cao hơn perceptual hash

### ✅ Anti-spoofing (Chống giả mạo)
- Phát hiện ảnh chụp từ màn hình
- Phát hiện ảnh in
- Phát hiện ảnh từ điện thoại khác
- Chỉ chấp nhận ảnh chụp trực tiếp từ webcam

### ✅ Đánh giá chất lượng ảnh
- Tự đánh giá độ rõ nét
- Kiểm tra ánh sáng
- Đề xuất chụp lại nếu chất lượng kém

## Chi phí sử dụng

### Free tier (Gemini 1.5 Flash)
- **15 request/phút** (đủ cho công ty nhỏ ~100 nhân viên)
- **1500 request/ngày**
- **Miễn phí hoàn toàn**

### Ước tính sử dụng
- Chấm công vào/ra: ~2 request/nhân viên/ngày
- 50 nhân viên = ~100 requests/ngày ✅ (dư dả)
- 100 nhân viên = ~200 requests/ngày ✅ (OK)
- 500+ nhân viên → Cân nhắc upgrade hoặc cache

## Tối ưu hóa

### 1. Cache encoding đã đăng ký
```csharp
// Đã tự động lưu trong database (bảng NV_FACE_DATA)
// Không cần gọi Gemini lại khi đã đăng ký
```

### 2. Rate limiting
```json
// Thêm vào appsettings.json (tùy chọn)
{
  "FaceRecognition": {
    "MaxAttemptsPerMinute": 3,  // Giới hạn 3 lần thử/phút/người
    "CooldownSeconds": 60
  }
}
```

### 3. Batch processing (tương lai)
- Gom nhiều ảnh xử lý cùng lúc
- Giảm số lần gọi API

## Troubleshooting

### Lỗi: "Gemini API Key không được cấu hình"
**Nguyên nhân:** Chưa điền API Key

**Giải pháp:**
1. Kiểm tra `appsettings.json` hoặc User Secrets
2. Đảm bảo key không rỗng
3. Restart backend

### Lỗi: "Gemini API error: 429"
**Nguyên nhân:** Vượt quá rate limit

**Giải pháp:**
1. Chờ 1 phút rồi thử lại
2. Giảm số lần chụp liên tục
3. Cân nhắc upgrade plan

### Lỗi: "Không parse được response từ Gemini"
**Nguyên nhân:** Gemini trả về format không chuẩn

**Giải pháp:**
1. Kiểm tra log chi tiết
2. API key có đúng không
3. Thử đổi model sang `gemini-pro-vision`

## Chuyển đổi giữa Simple Hash và Gemini

**Gemini → Simple Hash:**
```json
{
  "Gemini": {
    "ApiKey": "",  // ← Xóa hoặc để trống
  }
}
```

**Simple Hash → Gemini:**
```json
{
  "Gemini": {
    "ApiKey": "AIzaSy...",  // ← Điền API key
  }
}
```

Backend tự động chọn service phù hợp khi restart.

## Bảo mật

### ✅ Best Practices
1. **User Secrets** (Development)
2. **Environment Variables** (Production)
3. **Azure Key Vault** (Enterprise)
4. **HTTPS only** khi gọi API

### ❌ KHÔNG NÊN
1. Commit API key lên Git
2. Để key trong code
3. Share key qua email/chat
4. Dùng chung key nhiều project

## Liên hệ hỗ trợ
- Google AI Studio: https://ai.google.dev/
- Gemini API Docs: https://ai.google.dev/docs
- Pricing: https://ai.google.dev/pricing

---
**Cập nhật lần cuối:** 2026-02-10
