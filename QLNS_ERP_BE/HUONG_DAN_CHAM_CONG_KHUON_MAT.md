# HỆ THỐNG CHẤM CÔNG KHUÔN MẶT – TÀI LIỆU CHI TIẾT

> **Mục tiêu:** Mô tả toàn bộ cơ chế nhận diện khuôn mặt phục vụ chấm công trong hệ thống QLNS ERP: từ đăng ký khuôn mặt, luồng nhận diện, so sánh vector, đến ghi nhận chấm công và các chiến lược triển khai AI. Dành cho người chưa biết gì về dự án.

---

## MỤC LỤC

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Các thành phần dữ liệu](#2-các-thành-phần-dữ-liệu)
3. [Ba chiến lược nhận diện khuôn mặt](#3-ba-chiến-lược-nhận-diện-khuôn-mặt)
4. [Luồng đăng ký khuôn mặt](#4-luồng-đăng-ký-khuôn-mặt)
5. [Luồng chấm công bằng khuôn mặt](#5-luồng-chấm-công-bằng-khuôn-mặt)
6. [Hai chế độ nhận diện: Identify vs Verify](#6-hai-chế-độ-nhận-diện-identify-vs-verify)
7. [Thuật toán so sánh khuôn mặt](#7-thuật-toán-so-sánh-khuôn-mặt)
8. [Tối ưu hiệu năng (Cache + Parallel)](#8-tối-ưu-hiệu-năng-cache--parallel)
9. [Bảo mật & chống gian lận](#9-bảo-mật--chống-gian-lận)
10. [Cấu hình hệ thống](#10-cấu-hình-hệ-thống)
11. [Chấm công thủ công (Manual)](#11-chấm-công-thủ-công-manual)
12. [API Endpoints](#12-api-endpoints)
13. [Vai trò & phân quyền](#13-vai-trò--phân-quyền)
14. [Python Face Service](#14-python-face-service)
15. [Sơ đồ tổng thể](#15-sơ-đồ-tổng-thể)
16. [Câu hỏi thường gặp (FAQ)](#16-câu-hỏi-thường-gặp-faq)

---

## 1. Tổng quan kiến trúc

Hệ thống hoạt động theo mô hình **Backend-AI tách biệt**: ASP.NET Core xử lý business logic, còn việc phân tích khuôn mặt được ủy quyền cho một dịch vụ AI bên ngoài.

```
[Camera / Trình duyệt]
       │
       │  Gửi ảnh khuôn mặt (base64/file)
       ▼
[ASP.NET Core – FaceRecognitionController]
       │
       │  Gọi IFaceRecognitionService
       ▼
┌─────────────────────────────────────────────┐
│         CHIẾN LƯỢC AI (chọn 1 trong 3)      │
│                                             │
│  1. InsightFacePythonService  (ArcFace)     │
│     → Python Flask + InsightFace model      │
│     → 512-dim embedding vector              │
│                                             │
│  2. GeminiFaceRecognitionService            │
│     → Google Gemini Vision API              │
│     → Trích xuất đặc điểm khuôn mặt        │
│                                             │
│  3. SimpleFaceRecognitionService (fallback) │
│     → Perceptual Hash (pHash)               │
│     → Kém chính xác, dùng khi không có AI  │
└─────────────────────────────────────────────┘
       │
       │  Trả về encoding / similarity score
       ▼
[FaceDataService – Business Logic]
       │
       ├── Lưu encoding vào NvFaceData (DB)
       ├── Ghi log vào ChamCongFaceLog
       └── Cập nhật ChamCong (giờ vào/ra)
```

**Hiện trạng triển khai:**
- `Gemini:ApiKey` đang để trống → hệ thống dùng `SimpleFaceRecognitionService` làm mặc định
- Nếu bật Python service tại `localhost:5000` → `SimpleFaceRecognitionService` sẽ delegate sang Python (ArcFace)
- `InsightFacePythonService` là service tối ưu nhất, gọi Python API trực tiếp

---

## 2. Các thành phần dữ liệu

### 2.1 Bảng `NvFaceData` – Dữ liệu khuôn mặt nhân viên

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `NvHoSoId` | int | Nhân viên sở hữu dữ liệu khuôn mặt này |
| `FaceEncoding` | TEXT | JSON lưu vector embedding hoặc hash khuôn mặt |
| `FaceImageUrl` | string? | Đường dẫn ảnh gốc đã lưu trên server |
| `FaceImageThumbnail` | string? | Đường dẫn ảnh thu nhỏ |
| `IsActive` | bool | `true` = đang dùng để nhận diện |
| `QualityScore` | decimal? | Điểm chất lượng ảnh (0–1, do AI đánh giá) |
| `CreatedAt` | DateTime | Ngày đăng ký |
| `CreatedBy` | int? | Tài khoản đăng ký (HR hoặc chính nhân viên) |

> Mỗi nhân viên có thể đăng ký **tối đa 3 ảnh** khuôn mặt. Ảnh đầu tiên là **ảnh gốc** (primary). Các ảnh 2–3 phải giống ảnh gốc ≥ 70% để được chấp nhận.

---

### 2.2 Bảng `ChamCongFaceLog` – Log nhận diện khuôn mặt

Ghi lại mọi lần hệ thống kích hoạt camera chấm công, kể cả lần thất bại.

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | int | Khóa chính |
| `ChamCongId` | int? | Liên kết bản ghi chấm công (null nếu thất bại) |
| `NvHoSoId` | int? | Nhân viên được nhận diện (null nếu không nhận ra) |
| `ThoiGian` | DateTime | Thời điểm chấm công |
| `Loai` | string | `"VAO"` (check-in) hoặc `"RA"` (check-out) |
| `FaceImageUrl` | string? | Ảnh khuôn mặt lúc chấm công (bằng chứng) |
| `ConfidenceScore` | decimal? | Độ khớp (0–1), ví dụ `0.89` = khớp 89% |
| `TrangThai` | string | `THANH_CONG` / `THAT_BAI` / `NGHI_NGO` / `DA_XU_LY` |
| `LyDoThatBai` | string? | Lý do khi thất bại (VD: "Không phát hiện khuôn mặt") |
| `IpAddress` | string? | IP của thiết bị chấm công |
| `DeviceInfo` | string? | Thông tin trình duyệt/thiết bị |
| `ProcessingTimeMs` | int? | Thời gian xử lý (millisecond) |

---

### 2.3 Bảng `ChamCong` – Bản ghi chấm công

Cập nhật thêm 3 trường liên quan đến face recognition:

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `PhuongThuc` | string? | `"MANUAL"` (HR nhập tay) hoặc `"FACE_RECOGNITION"` (camera) |
| `FaceLogVaoId` | int? | FK → `ChamCongFaceLog` ghi nhận giờ vào |
| `FaceLogRaId` | int? | FK → `ChamCongFaceLog` ghi nhận giờ ra |

---

### 2.4 Bảng `FaceRecognitionConfig` – Cấu hình nhận diện

| Trường | Kiểu | Ý nghĩa |
|---|---|---|
| `KeyName` | string | Tên tham số (VD: `confidence_threshold`) |
| `Value` | string | Giá trị dạng string |
| `DataType` | string | `STRING` / `INT` / `DECIMAL` / `BOOLEAN` / `JSON` |
| `Description` | string? | Mô tả tham số |

---

## 3. Ba chiến lược nhận diện khuôn mặt

Hệ thống dùng **Design Pattern Strategy** — giao diện `IFaceRecognitionService` được hiện thực bởi 3 class khác nhau:

### 3.1 InsightFacePythonService – Tốt nhất (ArcFace)

**Nguyên lý:** Gọi Python Flask service chạy model **ArcFace (buffalo_l)** của InsightFace.

```
Ảnh input → Python /api/face/extract
          → InsightFace detect khuôn mặt
          → ArcFace model trích xuất 512-dim embedding vector
          → Trả về: { encoding: [512 số float], quality, bbox }
```

**512-dim embedding vector:** Là một mảng 512 số thực, mỗi số đại diện cho một đặc trưng khuôn mặt học được qua Deep Learning. Hai ảnh cùng người → 2 vector rất giống nhau (cosine similarity ≈ 0.9+). Hai người khác nhau → vectors khác xa nhau.

**Ưu điểm:**
- Chính xác nhất (~99% trên benchmark LFW)
- Không phụ thuộc cloud, chạy offline
- Tốc độ ~200ms/ảnh sau tối ưu

**Nhược điểm:** Cần cài đặt Python + InsightFace + model ~300MB

---

### 3.2 GeminiFaceRecognitionService – Cloud AI

**Nguyên lý:** Gửi ảnh (base64) đến **Google Gemini Vision API**, yêu cầu phân tích và trả về JSON đặc điểm khuôn mặt.

```
Ảnh input → Gemini Vision API (cloud)
          → AI phân tích ảnh
          → Trả về: {
              hasFace: true,
              isRealPerson: true,          ← chống giả mạo
              faceFeatures: {
                eyeDistance,    ← khoảng cách 2 mắt
                noseWidth,      ← chiều rộng mũi
                mouthWidth,     ← chiều rộng miệng
                faceShape,      ← hình dạng khuôn mặt
                skinTone,       ← tông da
                age,            ← ước tính tuổi
                gender          ← giới tính
              },
              quality: 0.85,
              fingerprint: "abc123" ← chuỗi nhận dạng ngắn
            }
```

**So sánh encodings (Gemini):** Tính điểm cho từng trong 7 đặc điểm → lấy trung bình

```
score = avg(eyeScore + noseScore + mouthScore + shapeScore + skinScore + ageScore + genderScore)
```

**Ưu điểm:** Không cần cài đặt server local, dùng ngay với API key.

**Nhược điểm:** Tốn chi phí API, cần internet, ít chính xác hơn vector 512-dim.

---

### 3.3 SimpleFaceRecognitionService – Fallback (pHash)

**Nguyên lý:** Dùng **Perceptual Hash (pHash)** — mỗi ảnh được resize về 8×8 pixel, chuyển grayscale, so sánh với giá trị trung bình → tạo chuỗi 64-bit nhị phân.

```
Ảnh 100×100 → Resize 8×8 (grayscale) → 64 pixels → so average → 64-bit string
                                                                 "1100101011..."
```

**So sánh:** Dùng **Hamming distance** — đếm số bit khác nhau → similarity = 1 - (hamming / 64)

**Ưu điểm:** Cực kỳ nhanh, không cần AI, chạy hoàn toàn trên .NET.

**Nhược điểm:** Rất kém chính xác, dễ nhận sai, chỉ dùng khi không có lựa chọn nào khác.

---

### Cách chọn service (Program.cs)

```csharp
if (!string.IsNullOrEmpty(geminiApiKey))
    services.AddScoped<IFaceRecognitionService, GeminiFaceRecognitionService>();
else
    services.AddScoped<IFaceRecognitionService, SimpleFaceRecognitionService>();
// InsightFacePythonService phải được đăng ký thủ công khi muốn dùng
```

---

## 4. Luồng đăng ký khuôn mặt

### Ai được đăng ký?

| Người thực hiện | Endpoint | Điều kiện |
|---|---|---|
| HR / Giám đốc | `POST /api/face-recognition/register/{nvId}` | Đăng ký cho bất kỳ nhân viên nào |
| Nhân viên tự đăng ký | `POST /api/face-recognition/register-self` | Chỉ đăng ký cho chính mình |

### Các bước xử lý (FaceDataService.RegisterFaceAsync)

```
Bước 1: Kiểm tra nhân viên tồn tại trong DB

Bước 2: Đếm số ảnh đã đăng ký
  → Nếu đã có 3 ảnh (MaxFacePerEmployee=3): từ chối
  → Số thứ tự ảnh mới = (số ảnh hiện có) + 1

Bước 3: Validate file ảnh
  → Chỉ chấp nhận: .jpg, .jpeg, .png
  → Kích thước tối đa: 5 MB

Bước 4: Gửi ảnh cho AI (ExtractFaceEncodingAsync)
  → AI phân tích ảnh → trả về JSON encoding

Bước 5: Validate kết quả AI
  5.1 Kiểm tra hasFace = true
      → Không có mặt: lỗi "Không phát hiện khuôn mặt trong ảnh"
  
  5.2 Kiểm tra chống giả mạo (isRealPerson)
      → isRealPerson = false: lỗi "Phát hiện ảnh giả mạo"
  
  5.3 Kiểm tra chất lượng ảnh (quality >= 0.5)
      → Chất lượng thấp: lỗi với gợi ý cải thiện
  
  5.4 Kiểm tra TRÙNG LẶP với nhân viên KHÁC
      → So sánh encoding mới với tất cả face của các NV khác
      → Nếu similarity >= 70%: lỗi "Khuôn mặt đã đăng ký cho {HoTen khác}"
  
  5.5 Nếu là ảnh 2 hoặc 3: kiểm tra giống ảnh gốc (ảnh 1)
      → Nếu similarity < 70%: lỗi "Ảnh không giống ảnh gốc"

Bước 6: Lưu file ảnh vào server (+ tạo thumbnail)

Bước 7: Lưu bản ghi NvFaceData vào DB

Bước 8: Invalidate cache face (để hệ thống load lại dữ liệu mới)

Bước 9: Ghi AuditLog
```

### Kết quả trả về

```json
{
  "success": true,
  "message": "✅ Đăng ký ảnh gốc (ảnh 1/3) thành công!",
  "faceId": 42,
  "qualityScore": 0.87,
  "imageNumber": 1
}
```

---

## 5. Luồng chấm công bằng khuôn mặt

### Hai loại chấm công: Check-in (Vào) và Check-out (Ra)

```http
POST /api/face-recognition/attendance/check-in
POST /api/face-recognition/attendance/check-out
Content-Type: multipart/form-data

{ image: <file ảnh khuôn mặt> }
```

> **Bảo mật quan trọng:** `NvHoSoId` **KHÔNG** lấy từ request body. Hệ thống đọc từ JWT token claim `EmployeeId`. Điều này ngăn chặn **IDOR** (Insecure Direct Object Reference) — kẻ tấn công không thể chấm công thay người khác bằng cách thay đổi ID trong request.

### Luồng xử lý check-in (ChamCongService)

```
Bước 1: Lấy NvHoSoId từ JWT token (KHÔNG từ request)

Bước 2: Kiểm tra nhân viên đã đăng ký khuôn mặt chưa
  → Nếu chưa: lỗi "Bạn chưa đăng ký khuôn mặt"

Bước 3: Xác minh khuôn mặt – VerifyEmployeeFaceAsync(nvHoSoId, image)
  → So sánh ảnh chụp với TẤT CẢ ảnh đã đăng ký của nhân viên đó
  → Tìm similarity cao nhất
  → isMatch = (maxSimilarity >= ConfidenceThreshold=0.6)

Bước 4: Nếu KHÔNG khớp:
  → Ghi log: TrangThai = "THAT_BAI"
  → Trả về lỗi với confidence score

Bước 5: Nếu KHỚP:
  → Xác định tháng hiện tại → tìm BangCongThang
  → Kiểm tra bảng công đã bị chốt chưa (DA_CHOT_CONG)
  
  Nếu LÀ check-in:
    → Tìm ChamCong hôm nay của nhân viên
    → Nếu chưa có: tạo mới, ghi GioVao = giờ hiện tại
    → Xác định TrangThai: GioVao <= GioChuanVao? "DI_LAM" : "TRE"
    → PhuongThuc = "FACE_RECOGNITION"
  
  Nếu LÀ check-out:
    → Tìm ChamCong hôm nay (phải có GioVao rồi)
    → Cập nhật GioRa = giờ hiện tại
    → Giữ nguyên TrangThai (đã set lúc check-in)

Bước 6: Ghi log thành công (ChamCongFaceLog)
  → TrangThai = "THANH_CONG"
  → Liên kết ChamCong.FaceLogVaoId hoặc FaceLogRaId

Bước 7: Trả về kết quả
```

### Kết quả check-in thành công

```json
{
  "success": true,
  "message": "Chấm công vào thành công lúc 08:03",
  "nvHoSoId": 5,
  "tenNhanVien": "Nguyễn Văn A",
  "confidenceScore": 0.91,
  "thoiGianChamCong": "2026-03-08T08:03:00Z",
  "loaiChamCong": "VAO",
  "chamCongId": 123,
  "logId": 456
}
```

---

## 6. Hai chế độ nhận diện: Identify vs Verify

Hệ thống hỗ trợ **2 chế độ** hoàn toàn khác nhau:

### Chế độ 1: Verify (Xác minh) – Dùng cho chấm công đăng nhập

```
Câu hỏi: "Ảnh này có phải là NV #5 không?"

Đầu vào: nvHoSoId (đã biết) + ảnh khuôn mặt
Xử lý:   Chỉ so sánh với các ảnh đã đăng ký của NV #5
Kết quả: (isMatch: bool, confidence: float)

Dùng khi: Nhân viên đã đăng nhập → camera chỉ cần xác nhận đúng người
```

### Chế độ 2: Identify (Nhận diện) – Dùng cho KIOSK không đăng nhập

```
Câu hỏi: "Ảnh này là ai?"

Đầu vào: ảnh khuôn mặt (không biết là ai)
Xử lý:   So sánh với TẤT CẢ nhân viên trong DB
Kết quả: (nvId: int?, confidence: float?)

Dùng khi: Kiosk chấm công đặt ở cửa vào, không yêu cầu đăng nhập
```

| Tiêu chí | Verify | Identify |
|---|---|---|
| Biết trước NV là ai? | ✅ Có | ❌ Không |
| So sánh với bao nhiêu NV? | Chỉ 1 NV | Toàn bộ DB |
| Tốc độ | Nhanh (~200ms) | Chậm hơn (phụ thuộc số NV) |
| Endpoint | `/attendance/check-in` | `/kiosk` (nếu có) |
| Authentication | Cần đăng nhập | Không cần (AllowAnonymous) |

---

## 7. Thuật toán so sánh khuôn mặt

### InsightFace / SimpleFace (Python): Cosine Similarity

**Cosine Similarity** đo góc giữa 2 vector trong không gian nhiều chiều:

$$\text{similarity} = \frac{\vec{A} \cdot \vec{B}}{|\vec{A}| \times |\vec{B}|} = \frac{\sum_{i=1}^{512} A_i \times B_i}{\sqrt{\sum A_i^2} \times \sqrt{\sum B_i^2}}$$

```
Kết quả:
  1.0  = giống hoàn toàn (cùng 1 ảnh)
  0.9  = rất giống (cùng người, ánh sáng khác nhau)
  0.75 = có thể là cùng người (cần kiểm tra)
  0.6  = ngưỡng mặc định (ConfidenceThreshold)
  0.3  = khác người
  0.0  = hoàn toàn khác nhau
```

**Code (C#):**
```csharp
double dotProduct = 0, norm1 = 0, norm2 = 0;
for (int i = 0; i < vec1.Count; i++) {
    dotProduct += vec1[i] * vec2[i];   // Tích vô hướng
    norm1      += vec1[i] * vec1[i];   // ||A||^2
    norm2      += vec2[i] * vec2[i];   // ||B||^2
}
return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
```

---

### SimpleFace (fallback): Hamming Distance

```csharp
// pHash: "10110010..." vs "10110011..."
// Đếm bit khác nhau → hamming distance
// similarity = 1 - (hamming / 64)
```

---

### Ngưỡng quyết định (Confidence Threshold)

| Ngưỡng | Ngữ cảnh sử dụng |
|---|---|
| `≥ 0.98` | Early-stop trong Parallel comparison (chắc chắn tuyệt đối) |
| `≥ 0.75` | Giới hạn trên của business threshold (clamp để tránh reject quá chặt) |
| `0.6` | Ngưỡng mặc định chấm công (`ConfidenceThreshold` config) |
| `0.70` | Ngưỡng kiểm tra trùng lặp khi đăng ký |
| `0.70` | Ngưỡng ảnh phụ phải giống ảnh gốc |
| `0.5` | Ngưỡng chất lượng ảnh khi đăng ký |

---

## 8. Tối ưu hiệu năng (Cache + Parallel)

Phiên bản đầu của hệ thống mất **6–8 giây** mỗi lần nhận diện. Sau tối ưu còn **dưới 3 giây**.

### Vấn đề ban đầu

```
Mỗi lần nhận diện:
  1. Query DB → lấy tất cả NvFaceData           (~100ms)
  2. Parse JSON encoding của TẤT CẢ nhân viên  (~500ms × N NV)
  3. So sánh tuần tự từng người một             (~200ms × N NV)
  → Tổng: 6-8 giây cho 20 nhân viên
```

### Giải pháp 1: In-Memory Cache (IMemoryCache)

```csharp
// Cache key: "all_active_faces"
// Thời gian tồn tại: 5 phút
// Nội dung: List<CachedFaceData> (đã parse vector sẵn)

private class CachedFaceData {
    public int NvHoSoId { get; set; }
    public List<double>? EncodingVector { get; set; }  // 512 số float, đã parse
}
```

**Effect:** Lần gọi đầu: query DB + parse JSON. Từ lần 2 trở đi: lấy thẳng từ RAM → tiết kiệm ~600ms cho 20 NV.

**Invalidation:** Khi có face mới đăng ký/xóa → gọi `InvalidateFaceCache()` → xóa cache key → lần tiếp theo sẽ load lại từ DB.

---

### Giải pháp 2: Parallel.ForEach (So sánh song song)

```csharp
Parallel.ForEach(allFaces, (face, state) => {
    if (earlyStop) return;

    var similarity = CompareFaceVectors(inputVector, face.EncodingVector);
    comparisonResults.Add((face.NvHoSoId, similarity));

    // Early termination: nếu gặp match ≥ 98% → dừng tất cả thread
    if (similarity >= 0.98) {
        earlyStop = true;
        state.Stop();
    }
});
```

**Effect:** Thay vì so sánh tuần tự 20 người (20 × 200ms = 4s), so sánh đồng thời trên nhiều CPU core → giảm xuống ~200ms.

---

### Giải pháp 3: Pre-parsed Vector (không parse JSON lại)

```csharp
// Trước tối ưu:
double similarity = CompareEncodings(encoding1Json, encoding2Json);
// → Mỗi lần gọi: JSON.parse(enc1) + JSON.parse(enc2) + tính toán

// Sau tối ưu (cache đã parse sẵn):
double similarity = CompareFaceVectors(inputVector, cachedFace.EncodingVector);
// → Chỉ tính toán, không parse JSON nữa
```

---

### Kết quả tổng hợp

```
Giai đoạn                    Trước      Sau
─────────────────────────────────────────────
Extract encoding (Python)    ~200ms     ~200ms   (không đổi)
Get face data từ DB           ~500ms     ~0ms     (đã cache)
Parse JSON encodings          ~300ms     ~0ms     (pre-parsed)
So sánh tất cả NV             ~4000ms    ~200ms   (parallel)
─────────────────────────────────────────────
TỔNG                         ~5000ms    ~400ms
```

---

## 9. Bảo mật & chống gian lận

### 9.1 Chống giả mạo ảnh (Anti-Spoofing)

Khi dùng **Gemini service**, API kiểm tra `isRealPerson`:

```json
{ "isRealPerson": false }  →  "Phát hiện ảnh giả mạo!"
```

**Các hành vi bị từ chối:**
- Chụp ảnh từ màn hình điện thoại/máy tính
- Dùng ảnh in giấy
- Dùng ảnh 3D mask

---

### 9.2 Chống chấm công thay (IDOR Prevention)

```csharp
// ✅ ĐÚNG: Lấy nvHoSoId từ JWT token
var employeeIdClaim = User.FindFirstValue("EmployeeId");
var nvHoSoId = int.Parse(employeeIdClaim!);

// ❌ SAI (đã bỏ): Lấy từ request body
// var nvHoSoId = dto.NvHoSoId;
```

---

### 9.3 Chống đăng ký khuôn mặt trùng lặp

Khi đăng ký ảnh mới, hệ thống so sánh với **tất cả nhân viên khác**:

```
Nếu similarity(ảnh mới, ảnh NV khác) >= 0.70:
  → Từ chối, thông báo: "Khuôn mặt đã đăng ký cho {HoTen}"
```

Điều này ngăn:
- Nhân viên A đăng ký khuôn mặt của nhân viên B để chấm công thay
- Khi nhân viên A chấm công → hệ thống nhận ra là khuôn mặt B → xảy ra nhầm lẫn

---

### 9.4 Xóa khuôn mặt chỉ xóa được của mình

```http
DELETE /api/face-recognition/my-face/{faceId}  → [EMPLOYEE, HR_ACC, GIAM_DOC]
```

Logic kiểm tra ownership:
```csharp
// Kiểm tra faceId thuộc về employeeId đang đăng nhập
var faceData = await _context.NvFaceDatas.FindAsync(faceId);
if (faceData.NvHoSoId != currentEmployeeId) → 403 Forbidden
```

HR & GĐ dùng endpoint riêng và có thể xóa của bất kỳ NV:
```http
DELETE /api/face-recognition/face/{faceId}  → [HR_ACC, GIAM_DOC]
```

---

### 9.5 Ghi log đầy đủ mọi lần nhận diện

Mọi lần camera kích hoạt — dù thành công hay thất bại — đều được ghi vào `ChamCongFaceLog`:

```
- Thời gian chính xác
- IP address thiết bị
- Confidence score
- Lý do thất bại (nếu có)
- Thời gian xử lý (millisecond)
- Ảnh khuôn mặt đã chụp (bằng chứng)
```

---

## 10. Cấu hình hệ thống

### Cấu hình trong `appsettings.json`

```json
{
  "FaceRecognition": {
    "ConfidenceThreshold": 0.6,        // Ngưỡng nhận diện (0-1)
    "MinQualityScore": 0.4,            // Chất lượng ảnh tối thiểu
    "MaxFacesPerEmployee": 3,          // Tối đa bao nhiêu ảnh/NV
    "ImageMaxSizeMB": 5,               // Kích thước ảnh tối đa
    "AllowedExtensions": [".jpg", ".jpeg", ".png"],
    "PythonApiUrl": "http://localhost:5000",  // URL Python service
    "TimeoutSeconds": 3                // Timeout gọi Python API
  },
  "Gemini": {
    "ApiKey": "",                      // Để trống = không dùng Gemini
    "Model": "gemini-2.5-flash",
    "TimeoutSeconds": 3
  },
  "WorkTime": {
    "Start": "08:00",                  // Giờ vào chuẩn
    "End": "17:00",                    // Giờ ra chuẩn
    "LateGraceMinutes": 0,             // Gia cú đến muộn (phút)
    "EarlyLeaveGraceMinutes": 0        // Gia cú về sớm (phút)
  }
}
```

### Giới hạn ConfidenceThreshold

```csharp
private double GetConfidenceThreshold() {
    var threshold = _config.GetValue<double>("FaceRecognition:ConfidenceThreshold", 0.6);
    // Clamp: không cho phép đặt quá 0.75
    // Bug fix: trước đây đặt 0.9 → từ chối tất cả mọi người vì ArcFace thường cho 0.75-0.85
    return Math.Min(threshold, 0.75);
}
```

---

## 11. Chấm công thủ công (Manual)

Ngoài khuôn mặt, HR có thể chấm công thủ công:

```http
PUT /api/ChamCong/cap-nhat/{chamCongId}
{
  "gioVao": "08:15",
  "gioRa": "17:30",
  "soGioOt": 1.5,
  "trangThai": "DI_LAM",
  "ghiChu": "Chấm bù do hệ thống lỗi"
}
```

Khi HR nhập thủ công, `PhuongThuc = "MANUAL"`. Trong module tính lương, bản ghi có `PhuongThuc = "MANUAL"` với `GioVao` sau giờ chuẩn cũng được xác định là đi muộn (nếu vượt gia cú).

### Trạng thái chấm công

| TrangThai | Ý nghĩa | Ảnh hưởng lương |
|---|---|---|
| `DI_LAM` | Đi làm đúng giờ | Tính công đủ |
| `TRE` | Đi muộn (face recognition xác định) | Tính công + phạt tiền |
| `VANG_MAT` | Vắng mặt không phép | Không tính công |
| `NGHI_PHEP` | Nghỉ phép có phê duyệt | Tùy cấu hình |
| `NGHI_LE` | Nghỉ lễ | Không tính công thường |

---

## 12. API Endpoints

### FaceRecognitionController (`/api/face-recognition`)

| Method | Endpoint | Phân quyền | Mô tả |
|---|---|---|---|
| `GET` | `/health` | Public | Kiểm tra Python service còn chạy không |
| `POST` | `/register/{nvId}` | HR, GĐ | Đăng ký khuôn mặt cho NV bất kỳ |
| `POST` | `/register-self` | All | Nhân viên tự đăng ký |
| `GET` | `/registered` | HR, GĐ | Danh sách NV đã đăng ký face |
| `GET` | `/employee/{nvId}` | HR, GĐ | Dữ liệu face của 1 NV |
| `DELETE` | `/face/{faceId}` | HR, GĐ | Xóa face bất kỳ |
| `DELETE` | `/my-face/{faceId}` | All | Xóa face của chính mình |
| `DELETE` | `/employee/{nvId}` | HR, GĐ | Xóa TẤT CẢ face của 1 NV |
| `GET` | `/my-face-data` | All | Xem face data của chính mình |
| `POST` | `/attendance/check-in` | All | Chấm công vào bằng khuôn mặt |
| `POST` | `/attendance/check-out` | All | Chấm công ra bằng khuôn mặt |
| `POST` | `/verify/{nvId}` | Authorize | Kiểm tra ảnh có khớp NV không |
| `GET` | `/logs` | HR, GĐ | Xem log nhận diện |

### ChamCongController (`/api/ChamCong`)

| Method | Endpoint | Phân quyền | Mô tả |
|---|---|---|---|
| `GET` | `/bang-cong?nam=2026` | HR, GĐ | Danh sách bảng công |
| `GET` | `/bang-cong/{id}` | HR, GĐ | Chi tiết bảng công |
| `GET` | `/nhan-vien/{nvId}/ngay?ngay=...` | HR, GĐ | Chấm công 1 NV theo ngày |
| `PUT` | `/cap-nhat/{chamCongId}` | HR, GĐ | Cập nhật chấm công thủ công |
| `DELETE` | `/cap-nhat/{chamCongId}` | HR, GĐ | Xóa bản ghi chấm công |
| `PUT` | `/lock` | HR, GĐ | Chốt bảng công tháng |
| `GET` | `/me/thang-list?nam=2026` | EMPLOYEE | NV xem danh sách tháng của mình |
| `GET` | `/me/chi-tiet?thang=3&nam=2026` | EMPLOYEE | NV xem chi tiết chấm công |
| `GET` | `/bang-cong-paged?...` | HR, GĐ | Danh sách có phân trang + lọc |
| `GET` | `/config` | HR, GĐ | Đọc cấu hình giờ làm |
| `POST` | `/config` | HR, GĐ | Cập nhật cấu hình giờ làm |
| `POST` | `/notify-checkin` | Public | Webhook thông báo check-in |

---

## 13. Vai trò & phân quyền

| Chức năng | EMPLOYEE | HR_ACC | GIAM_DOC |
|---|:---:|:---:|:---:|
| Tự đăng ký khuôn mặt | ✅ | ✅ | ✅ |
| Đăng ký khuôn mặt cho NV khác | ❌ | ✅ | ✅ |
| Chấm công bằng khuôn mặt | ✅ | ✅ | ✅ |
| Xem face data của NV khác | ❌ | ✅ | ✅ |
| Xóa face của chính mình | ✅ | ✅ | ✅ |
| Xóa face của NV khác | ❌ | ✅ | ✅ |
| Cập nhật chấm công thủ công | ❌ | ✅ | ✅ |
| Chốt bảng công | ❌ | ✅ | ✅ |
| Xem log nhận diện | ❌ | ✅ | ✅ |
| Xem chấm công của mình | ✅ | ✅ | ✅ |

---

## 14. Python Face Service

Python Flask server chạy model **ArcFace (buffalo_l)** của InsightFace:

### Yêu cầu môi trường

```bash
pip install flask flask-cors insightface opencv-python numpy
python app.py  # Mặc định chạy ở http://localhost:5000
```

### Endpoints Python

| Endpoint | Method | Input | Output |
|---|---|---|---|
| `/health` | GET | — | `{ status, model, ready }` |
| `/api/face/extract` | POST | `{ image: base64 }` | `{ success, encoding: [512 floats], hasFace, quality, bbox }` |

### Luồng xử lý trong Python

```python
# /api/face/extract
1. Decode base64 → numpy BGR array
2. FaceAnalysis.get(image) → phát hiện khuôn mặt
3. Nếu nhiều khuôn mặt → chọn khuôn mặt to nhất (bbox area)
4. Lấy face.embedding (512-dim ArcFace vector)
5. Tính quality score từ face.det_score
6. Trả về: { encoding: [...512], hasFace, quality, bbox }
```

### Cosine Similarity (Python)

```python
def cosine_similarity(feat1, feat2):
    return np.dot(feat1, feat2) / (np.linalg.norm(feat1) * np.linalg.norm(feat2))
```

*Kết quả giống hệt công thức C# trong InsightFacePythonService.*

---

## 15. Sơ đồ tổng thể

```
┌────────────────────────────────────────────────────────────────────┐
│                   HỆ THỐNG CHẤM CÔNG KHUÔN MẶT                    │
├────────────────────────────────────────────────────────────────────┤
│                                                                    │
│  BƯỚC 1: ĐĂNG KÝ KHUÔN MẶT (1 lần)                                │
│  ─────────────────────────────────                                 │
│  [HR/NV] → Upload ảnh                                              │
│      │                                                             │
│      ▼                                                             │
│  [FaceDataService]                                                 │
│      ├─ Validate (≤3 ảnh, ≤5MB, jpg/png)                          │
│      ├─ AI: ExtractFaceEncoding → 512-dim vector                   │
│      ├─ Kiểm tra: hasFace? isRealPerson? quality≥0.5?             │
│      ├─ Kiểm tra: trùng NV khác? (similarity ≥ 70%)              │
│      ├─ Kiểm tra: giống ảnh gốc? (ảnh 2,3 cần ≥70%)              │
│      └─ Lưu NvFaceData + Invalidate Cache                         │
│                                                                    │
│  BƯỚC 2: CHẤM CÔNG HÀNG NGÀY                                       │
│  ─────────────────────────────                                     │
│  [NV đăng nhập] → Chụp ảnh → POST /check-in                       │
│      │                                                             │
│      ▼                                                             │
│  [FaceRecognitionController]                                      │
│      │ Lấy NvHoSoId từ JWT (không từ body)                        │
│      ▼                                                             │
│  [IFaceRecognitionService.VerifyEmployeeFaceAsync]                 │
│      ├─ ExtractEncoding(ảnh mới) → vector 512-dim                 │
│      ├─ Load ảnh đã đăng ký của NV đó (từ cache nếu có)           │
│      ├─ CosineSimilarity(vector_mới, vector_đã_đăng_ký)           │
│      └─ similarity ≥ 0.6? → KHỚP : KHÔNG KHỚP                    │
│                │                                                   │
│         KHỚP  ─┤                                                   │
│                ▼                                                   │
│  [ChamCongService]                                                 │
│      ├─ Tìm/tạo ChamCong hôm nay                                  │
│      ├─ Ghi GioVao / GioRa                                         │
│      ├─ TrangThai: "DI_LAM" hoặc "TRE"                            │
│      ├─ PhuongThuc = "FACE_RECOGNITION"                            │
│      └─ Ghi ChamCongFaceLog (THANH_CONG)                          │
│                │                                                   │
│         KHÔNG KHỚP                                                 │
│                ▼                                                   │
│  [ChamCongFaceLog] (THAT_BAI + lý do)                             │
│                                                                    │
│  BƯỚC 3: TÍNH LƯƠNG (cuối tháng)                                   │
│  ────────────────────────────────                                  │
│  LuongService đọc:                                                 │
│      ChamCong.TrangThai IN ('DI_LAM', 'TRE') → TongCong           │
│      ChamCong.TrangThai = 'TRE' → SoLanDiMuon → PhatTien          │
│      (Không phân biệt FACE_RECOGNITION hay MANUAL)                │
│                                                                    │
└────────────────────────────────────────────────────────────────────┘
```

---

## 16. Câu hỏi thường gặp (FAQ)

### Q: Tại sao nhận diện mất 3–8 giây?
**A:** Toàn bộ thời gian là do bước AI extract encoding: gửi ảnh qua HTTP đến Python service → model ArcFace xử lý → trả về. Phần so sánh vector sau tối ưu chỉ mất ~200ms. Nếu Python service chưa warm up (lần gọi đầu tiên sau khởi động), model cần load → mất thêm 1–2 giây.

---

### Q: Đội mắt kính / đổi kiểu tóc có bị nhận sai không?
**A:** ArcFace (InsightFace) rất mạnh với các biến đổi như: ánh sáng khác nhau, góc chụp thay đổi, đeo kính, thay đổi kiểu tóc. Độ chính xác thường vẫn đạt ≥85% trong điều kiện bình thường. Nếu thay đổi quá lớn (ví dụ để râu dài sau khi đăng ký không có râu), nên đăng ký lại ảnh.

---

### Q: Nhân viên có thể chấm công cho người khác không?
**A:** Không thể. Hệ thống dùng **Verify mode** — chỉ so sánh ảnh chụp với khuôn mặt của **chính người đang đăng nhập** (lấy từ JWT). Dù có chụp ảnh người khác, hệ thống sẽ trả về "Không khớp khuôn mặt".

---

### Q: Mất điện/mất mạng có chấm được không?
**A:** Không. Hệ thống cần kết nối đến Python service để xử lý AI. Nếu Python service down → trả lỗi. Trong trường hợp này, HR có thể chấm công thủ công qua giao diện quản lý.

---

### Q: Cache 5 phút có vấn đề gì không?
**A:** Nếu HR đăng ký face mới cho nhân viên A, trong 5 phút tiếp theo hệ thống vẫn dùng cache cũ (chưa có face mới). Tuy nhiên, code đã gọi `InvalidateFaceCache()` ngay sau khi đăng ký thành công → cache bị xóa ngay lập tức → lần nhận diện tiếp theo sẽ load dữ liệu mới từ DB.

---

### Q: Ngưỡng 0.6 có phù hợp không?
**A:** Với ArcFace (InsightFace), similarity của cùng một người thường dao động từ **0.75 – 0.92**. Ngưỡng 0.6 khá thấp, có thể gây nhận sai (false positive) nếu có người giống nhau. Nên tăng lên 0.70 trong môi trường production. Hệ thống đã có clamp max = 0.75 để tránh cấu hình quá chặt rejecting người thật.

---

### Q: Cùng một khuôn mặt bị đăng ký cho 2 nhân viên thì sao?
**A:** Hệ thống kiểm tra duplicate khi đăng ký (ngưỡng 70%). Nếu muốn "cưỡng ép" đăng ký trường hợp này, phải xóa face của nhân viên cũ trước. Không thể có 2 NV cùng khuôn mặt tồn tại song song trong hệ thống (để tránh nhầm lẫn khi identify).

---

*Tài liệu được tạo ngày 08/03/2026 – Phiên bản 1.0*
