# 📋 PLAN: HỆ THỐNG CHẤM CÔNG BẰNG NHẬN DIỆN KHUÔN MẶT

**Dự án**: QLNS ERP - Face Recognition Attendance System  
**Tech Stack**: .NET 8 Web API + Angular 16 + MySQL  
**Ngày tạo**: 07/02/2026

---

## 🎯 MỤC TIÊU

Xây dựng hệ thống chấm công tự động bằng nhận diện khuôn mặt, cho phép:
- Nhân viên chấm công vào/ra bằng camera (không cần thẻ/QR)
- HR đăng ký/quản lý dữ liệu khuôn mặt nhân viên
- Lưu log đầy đủ để audit và xử lý tranh chấp
- Tích hợp với hệ thống chấm công hiện tại

---

## 🗄️ PHẦN 1: DATABASE SCHEMA

### 1.1. Bảng mới: `nv_face_data` (Dữ liệu khuôn mặt nhân viên)

```sql
CREATE TABLE `nv_face_data` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `nv_ho_so_id` INT NOT NULL COMMENT 'ID nhân viên',
  `face_encoding` TEXT NOT NULL COMMENT 'JSON array - 128 face embeddings',
  `face_image_url` VARCHAR(500) COMMENT 'URL ảnh khuôn mặt mẫu',
  `face_image_thumbnail` VARCHAR(500) COMMENT 'URL ảnh thumbnail',
  `is_active` TINYINT(1) DEFAULT 1 COMMENT 'Còn sử dụng không',
  `quality_score` DECIMAL(5,4) COMMENT 'Chất lượng ảnh (0-1)',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  `created_by` INT COMMENT 'Tài khoản HR tạo',
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `updated_by` INT COMMENT 'Tài khoản cập nhật',
  
  KEY `FK_FACE_NV_HO_SO` (`nv_ho_so_id`),
  KEY `FK_FACE_CREATED_BY` (`created_by`),
  KEY `IDX_ACTIVE` (`is_active`),
  
  CONSTRAINT `FK_FACE_NV_HO_SO` 
    FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) 
    ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `FK_FACE_CREATED_BY` 
    FOREIGN KEY (`created_by`) REFERENCES `tai_khoan` (`id`) 
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Index cho tìm kiếm nhanh
CREATE INDEX `IDX_NV_ACTIVE` ON `nv_face_data` (`nv_ho_so_id`, `is_active`);
```

**Giải thích cột:**
- `face_encoding`: Lưu vector 128 chiều (dùng JSON) - đặc trưng khuôn mặt
- `quality_score`: Điểm chất lượng ảnh để ưu tiên matching
- Có thể lưu nhiều ảnh/1 nhân viên (góc độ khác nhau)

---

### 1.2. Bảng mới: `cham_cong_face_log` (Lịch sử nhận diện)

```sql
CREATE TABLE `cham_cong_face_log` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `cham_cong_id` INT DEFAULT NULL COMMENT 'NULL nếu nhận diện thất bại',
  `nv_ho_so_id` INT DEFAULT NULL COMMENT 'Nhân viên được nhận diện',
  `thoi_gian` DATETIME NOT NULL COMMENT 'Thời điểm chấm công',
  `loai` ENUM('VAO','RA') NOT NULL,
  `face_image_url` VARCHAR(500) COMMENT 'Ảnh chụp lúc chấm công',
  `confidence_score` DECIMAL(5,4) COMMENT 'Độ tin cậy nhận diện (0-1)',
  `trang_thai` ENUM('THANH_CONG','THAT_BAI','NGHI_NGO','DA_XU_LY') NOT NULL DEFAULT 'THANH_CONG',
  `ly_do_that_bai` VARCHAR(500) COMMENT 'Lý do thất bại: không phát hiện mặt, không khớp, v.v.',
  `ghi_chu` VARCHAR(500),
  
  -- Thông tin thiết bị & bảo mật
  `ip_address` VARCHAR(50),
  `device_info` VARCHAR(200) COMMENT 'User agent, device name',
  `location` VARCHAR(200) COMMENT 'Vị trí GPS (nếu có)',
  
  -- Metadata
  `processing_time_ms` INT COMMENT 'Thời gian xử lý (ms)',
  `created_at` DATETIME DEFAULT CURRENT_TIMESTAMP,
  
  KEY `FK_FACE_LOG_CHAM_CONG` (`cham_cong_id`),
  KEY `FK_FACE_LOG_NV` (`nv_ho_so_id`),
  KEY `IDX_THOI_GIAN` (`thoi_gian`),
  KEY `IDX_TRANG_THAI` (`trang_thai`),
  KEY `IDX_NGAY` (DATE(`thoi_gian`)),
  
  CONSTRAINT `FK_FACE_LOG_CHAM_CONG` 
    FOREIGN KEY (`cham_cong_id`) REFERENCES `cham_cong` (`id`) 
    ON DELETE SET NULL ON UPDATE CASCADE,
  CONSTRAINT `FK_FACE_LOG_NV` 
    FOREIGN KEY (`nv_ho_so_id`) REFERENCES `nv_ho_so` (`id`) 
    ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

**Mục đích:**
- Lưu **TẤT CẢ** lần nhận diện (cả thành công lẫn thất bại)
- Audit trail đầy đủ
- Phát hiện gian lận (nhiều người dùng 1 ảnh, nhận diện lạ)

---

### 1.3. Cập nhật bảng `cham_cong` (Thêm cột)

```sql
ALTER TABLE `cham_cong` 
ADD COLUMN `phuong_thuc` ENUM('MANUAL','FACE_RECOGNITION','QR_CODE','RFID_CARD','BIOMETRIC') 
  DEFAULT 'MANUAL' 
  COMMENT 'Phương thức chấm công' 
  AFTER `trang_thai`,

ADD COLUMN `created_by` INT DEFAULT NULL 
  COMMENT 'Tài khoản tạo (nếu manual/HR)' 
  AFTER `phuong_thuc`,

ADD COLUMN `face_log_vao_id` INT DEFAULT NULL 
  COMMENT 'ID log nhận diện khi vào' 
  AFTER `created_by`,

ADD COLUMN `face_log_ra_id` INT DEFAULT NULL 
  COMMENT 'ID log nhận diện khi ra' 
  AFTER `face_log_vao_id`,

ADD KEY `FK_CHAM_CONG_CREATED_BY` (`created_by`),
ADD KEY `FK_CHAM_CONG_FACE_VAO` (`face_log_vao_id`),
ADD KEY `FK_CHAM_CONG_FACE_RA` (`face_log_ra_id`),

ADD CONSTRAINT `FK_CHAM_CONG_CREATED_BY` 
  FOREIGN KEY (`created_by`) REFERENCES `tai_khoan` (`id`) 
  ON DELETE SET NULL,

ADD CONSTRAINT `FK_CHAM_CONG_FACE_VAO` 
  FOREIGN KEY (`face_log_vao_id`) REFERENCES `cham_cong_face_log` (`id`) 
  ON DELETE SET NULL,

ADD CONSTRAINT `FK_CHAM_CONG_FACE_RA` 
  FOREIGN KEY (`face_log_ra_id`) REFERENCES `cham_cong_face_log` (`id`) 
  ON DELETE SET NULL;
```

---

### 1.4. Bảng cấu hình: `face_recognition_config` (Optional - nếu muốn config qua DB)

```sql
CREATE TABLE `face_recognition_config` (
  `id` INT AUTO_INCREMENT PRIMARY KEY,
  `key_name` VARCHAR(100) NOT NULL UNIQUE COMMENT 'confidence_threshold, max_attempts',
  `value` VARCHAR(500) NOT NULL,
  `data_type` ENUM('STRING','INT','DECIMAL','BOOLEAN','JSON') DEFAULT 'STRING',
  `description` VARCHAR(500),
  `updated_at` DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  `updated_by` INT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Dữ liệu mẫu
INSERT INTO `face_recognition_config` (`key_name`, `value`, `data_type`, `description`) VALUES
('confidence_threshold', '0.60', 'DECIMAL', 'Ngưỡng độ tin cậy tối thiểu (0-1)'),
('max_face_per_employee', '3', 'INT', 'Số ảnh khuôn mặt tối đa/nhân viên'),
('enable_liveness_check', 'false', 'BOOLEAN', 'Kiểm tra ảnh thật (chống ảnh in)'),
('allow_checkin_minutes_before', '30', 'INT', 'Cho phép chấm công sớm trước giờ làm (phút)'),
('allow_multiple_checkin_per_day', 'false', 'BOOLEAN', 'Cho phép check-in nhiều lần/ngày');
```

---

## 🔧 PHẦN 2: BACKEND (.NET 8 WEB API)

### 2.1. NuGet Packages cần cài đặt

#### **Option 1: Dùng DlibDotNet (Miễn phí, offline, phức tạp)**

```powershell
cd QLNS_BE/QLNS_BE

# Core package
dotnet add package DlibDotNet --version 19.24.4
dotnet add package DlibDotNet.Extensions --version 19.24.4

# Image processing
dotnet add package SixLabors.ImageSharp --version 3.1.0
dotnet add package SixLabors.ImageSharp.Drawing --version 2.1.0
```

**Lưu ý:** Cần download model file:
- `shape_predictor_5_face_landmarks.dat` (9.2 MB)
- `dlib_face_recognition_resnet_model_v1.dat` (22.5 MB)
- Đặt trong `wwwroot/models/`

#### **Option 2: Dùng Azure Cognitive Services (Dễ, có phí ~$1/1000 calls)**

```powershell
dotnet add package Microsoft.Azure.CognitiveServices.Vision.Face --version 2.8.0
```

#### **Packages chung (bất kể dùng option nào)**

```powershell
# Image upload/processing
dotnet add package SixLabors.ImageSharp --version 3.1.0

# Nếu chưa có
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Newtonsoft.Json --version 13.0.3
```

---

### 2.2. Cấu trúc thư mục Backend

```
QLNS_BE/
├── Controllers/
│   └── FaceRecognitionController.cs         ⭐ NEW
│
├── Services/
│   ├── FaceRecognition/
│   │   ├── IFaceRecognitionService.cs       ⭐ NEW - Interface
│   │   ├── DlibFaceService.cs               ⭐ NEW - Implementation với Dlib
│   │   └── AzureFaceService.cs              ⭐ NEW - Implementation với Azure
│   │
│   ├── FaceDataService.cs                   ⭐ NEW - CRUD face data
│   └── ChamCongService.cs                   ✏️ UPDATE - Thêm logic face
│
├── Models/
│   ├── Entities/
│   │   ├── NvFaceData.cs                    ⭐ NEW
│   │   ├── ChamCongFaceLog.cs               ⭐ NEW
│   │   └── FaceRecognitionConfig.cs         ⭐ NEW (optional)
│   │
│   └── Dtos/
│       └── FaceRecognition/                 ⭐ NEW folder
│           ├── RegisterFaceDto.cs
│           ├── UpdateFaceDto.cs
│           ├── CheckInByFaceDto.cs
│           ├── FaceRecognitionResultDto.cs
│           ├── FaceDataDto.cs
│           ├── FaceLogDto.cs
│           └── FaceLogFilterDto.cs
│
├── Data/
│   └── AppDbContext.cs                      ✏️ UPDATE - Thêm DbSet mới
│
├── wwwroot/
│   ├── uploads/
│   │   └── faces/                           ⭐ NEW - Lưu ảnh khuôn mặt
│   │       ├── registered/                  (Ảnh đăng ký)
│   │       └── checkin/                     (Ảnh chấm công)
│   └── models/                              ⭐ NEW - Model files (nếu dùng Dlib)
│
├── Configuration/
│   └── FaceRecognitionSettings.cs           ⭐ NEW
│
└── appsettings.json                         ✏️ UPDATE
```

---

### 2.3. Entities (Models/Entities/)

#### **NvFaceData.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS_BE.Models.Entities
{
    [Table("nv_face_data")]
    public class NvFaceData
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("nv_ho_so_id")]
        public int NvHoSoId { get; set; }

        [Required]
        [Column("face_encoding", TypeName = "TEXT")]
        public string FaceEncoding { get; set; } = string.Empty; // JSON array

        [Column("face_image_url")]
        [MaxLength(500)]
        public string? FaceImageUrl { get; set; }

        [Column("face_image_thumbnail")]
        [MaxLength(500)]
        public string? FaceImageThumbnail { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("quality_score", TypeName = "decimal(5,4)")]
        public decimal? QualityScore { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("created_by")]
        public int? CreatedBy { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [Column("updated_by")]
        public int? UpdatedBy { get; set; }

        // Navigation
        [ForeignKey("NvHoSoId")]
        public virtual NvHoSo? NhanVien { get; set; }

        [ForeignKey("CreatedBy")]
        public virtual TaiKhoan? NguoiTao { get; set; }
    }
}
```

#### **ChamCongFaceLog.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLNS_BE.Models.Entities
{
    [Table("cham_cong_face_log")]
    public class ChamCongFaceLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("cham_cong_id")]
        public int? ChamCongId { get; set; }

        [Column("nv_ho_so_id")]
        public int? NvHoSoId { get; set; }

        [Required]
        [Column("thoi_gian")]
        public DateTime ThoiGian { get; set; }

        [Required]
        [Column("loai")]
        [MaxLength(10)]
        public string Loai { get; set; } = string.Empty; // VAO, RA

        [Column("face_image_url")]
        [MaxLength(500)]
        public string? FaceImageUrl { get; set; }

        [Column("confidence_score", TypeName = "decimal(5,4)")]
        public decimal? ConfidenceScore { get; set; }

        [Required]
        [Column("trang_thai")]
        [MaxLength(20)]
        public string TrangThai { get; set; } = "THANH_CONG"; // THANH_CONG, THAT_BAI, NGHI_NGO

        [Column("ly_do_that_bai")]
        [MaxLength(500)]
        public string? LyDoThatBai { get; set; }

        [Column("ghi_chu")]
        [MaxLength(500)]
        public string? GhiChu { get; set; }

        [Column("ip_address")]
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [Column("device_info")]
        [MaxLength(200)]
        public string? DeviceInfo { get; set; }

        [Column("location")]
        [MaxLength(200)]
        public string? Location { get; set; }

        [Column("processing_time_ms")]
        public int? ProcessingTimeMs { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("ChamCongId")]
        public virtual ChamCong? ChamCong { get; set; }

        [ForeignKey("NvHoSoId")]
        public virtual NvHoSo? NhanVien { get; set; }
    }
}
```

---

### 2.4. DTOs (Models/Dtos/FaceRecognition/)

#### **RegisterFaceDto.cs**
```csharp
public class RegisterFaceDto
{
    public IFormFile Image { get; set; } = null!;
    public int NvHoSoId { get; set; }
    public string? GhiChu { get; set; }
}
```

#### **FaceRecognitionResultDto.cs**
```csharp
public class FaceRecognitionResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? NvHoSoId { get; set; }
    public string? TenNhanVien { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public DateTime? ThoiGianChamCong { get; set; }
    public string? LoaiChamCong { get; set; } // VAO, RA
    public int? ChamCongId { get; set; }
    public int? LogId { get; set; }
}
```

#### **FaceLogDto.cs**
```csharp
public class FaceLogDto
{
    public int Id { get; set; }
    public int? NvHoSoId { get; set; }
    public string? TenNhanVien { get; set; }
    public DateTime ThoiGian { get; set; }
    public string Loai { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }
    public string? FaceImageUrl { get; set; }
    public string? LyDoThatBai { get; set; }
    public string? IpAddress { get; set; }
}
```

#### **FaceLogFilterDto.cs**
```csharp
public class FaceLogFilterDto
{
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public int? NvHoSoId { get; set; }
    public string? TrangThai { get; set; } // THANH_CONG, THAT_BAI, NGHI_NGO
    public string? Loai { get; set; } // VAO, RA
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
```

---

### 2.5. Interface & Service

#### **IFaceRecognitionService.cs**

```csharp
namespace QLNS_BE.Services.FaceRecognition
{
    public interface IFaceRecognitionService
    {
        /// <summary>
        /// Trích xuất face encoding từ ảnh
        /// </summary>
        /// <returns>Array 128 số double, hoặc null nếu không phát hiện mặt</returns>
        Task<double[]?> ExtractFaceEncodingAsync(Stream imageStream);

        /// <summary>
        /// So sánh 2 face encodings
        /// </summary>
        /// <returns>Khoảng cách Euclidean (càng nhỏ càng giống, 0-1)</returns>
        double CalculateDistance(double[] encoding1, double[] encoding2);

        /// <summary>
        /// Kiểm tra 2 khuôn mặt có khớp không
        /// </summary>
        bool IsMatch(double[] encoding1, double[] encoding2, double threshold = 0.6);

        /// <summary>
        /// Tìm nhân viên từ ảnh (so sánh với tất cả face data trong DB)
        /// </summary>
        Task<(int? nvId, double? confidence)> IdentifyEmployeeAsync(Stream imageStream);

        /// <summary>
        /// Kiểm tra ảnh có khuôn mặt không
        /// </summary>
        Task<bool> HasFaceAsync(Stream imageStream);

        /// <summary>
        /// Đánh giá chất lượng ảnh (độ nét, ánh sáng, góc độ)
        /// </summary>
        Task<double> EvaluateImageQualityAsync(Stream imageStream);
    }
}
```

#### **DlibFaceService.cs** (Implementation - tóm tắt)

```csharp
using DlibDotNet;
using System.Text.Json;

namespace QLNS_BE.Services.FaceRecognition
{
    public class DlibFaceService : IFaceRecognitionService
    {
        private readonly string _modelPath;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        
        // Dlib objects (singleton)
        private static FrontalFaceDetector? _faceDetector;
        private static ShapePredictor? _shapePredictor;
        private static DlibDotNet.Dnn.LossMetric? _faceRecognitionModel;

        public DlibFaceService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
            _modelPath = config["FaceRecognition:ModelPath"] ?? "wwwroot/models";
            
            InitializeModels();
        }

        private void InitializeModels()
        {
            if (_faceDetector == null)
            {
                _faceDetector = Dlib.GetFrontalFaceDetector();
                _shapePredictor = ShapePredictor.Deserialize(
                    Path.Combine(_modelPath, "shape_predictor_5_face_landmarks.dat"));
                _faceRecognitionModel = DlibDotNet.Dnn.LossMetric.Deserialize(
                    Path.Combine(_modelPath, "dlib_face_recognition_resnet_model_v1.dat"));
            }
        }

        public async Task<double[]?> ExtractFaceEncodingAsync(Stream imageStream)
        {
            // 1. Load image
            // 2. Detect faces
            // 3. Get landmarks
            // 4. Extract 128D encoding
            // ... implementation ...
            
            return await Task.FromResult<double[]?>(null); // Placeholder
        }

        public double CalculateDistance(double[] enc1, double[] enc2)
        {
            // Euclidean distance
            double sum = 0;
            for (int i = 0; i < enc1.Length; i++)
            {
                double diff = enc1[i] - enc2[i];
                sum += diff * diff;
            }
            return Math.Sqrt(sum);
        }

        public bool IsMatch(double[] enc1, double[] enc2, double threshold = 0.6)
        {
            return CalculateDistance(enc1, enc2) < threshold;
        }

        public async Task<(int? nvId, double? confidence)> IdentifyEmployeeAsync(Stream imageStream)
        {
            // 1. Extract encoding từ ảnh
            var encoding = await ExtractFaceEncodingAsync(imageStream);
            if (encoding == null) return (null, null);

            // 2. Lấy tất cả face data đang active
            var allFaces = await _context.NvFaceDatas
                .Where(x => x.IsActive)
                .ToListAsync();

            // 3. So sánh với từng face
            double minDistance = double.MaxValue;
            int? matchedNvId = null;

            foreach (var face in allFaces)
            {
                var storedEncoding = JsonSerializer.Deserialize<double[]>(face.FaceEncoding);
                if (storedEncoding == null) continue;

                var distance = CalculateDistance(encoding, storedEncoding);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    matchedNvId = face.NvHoSoId;
                }
            }

            // 4. Check threshold
            var threshold = double.Parse(_config["FaceRecognition:ConfidenceThreshold"] ?? "0.6");
            if (minDistance < threshold)
            {
                var confidence = 1 - minDistance; // Convert to confidence score
                return (matchedNvId, confidence);
            }

            return (null, null);
        }

        // ... other methods ...
    }
}
```

---

### 2.6. FaceDataService.cs (CRUD operations)

```csharp
namespace QLNS_BE.Services
{
    public class FaceDataService
    {
        private readonly AppDbContext _context;
        private readonly IFaceRecognitionService _faceService;
        private readonly IWebHostEnvironment _env;
        private readonly AuditLogService _auditLog;

        public FaceDataService(
            AppDbContext context, 
            IFaceRecognitionService faceService,
            IWebHostEnvironment env,
            AuditLogService auditLog)
        {
            _context = context;
            _faceService = faceService;
            _env = env;
            _auditLog = auditLog;
        }

        /// <summary>
        /// Đăng ký khuôn mặt cho nhân viên
        /// </summary>
        public async Task<(bool success, string message, int? faceId)> RegisterFaceAsync(
            int nvHoSoId, 
            IFormFile imageFile, 
            int createdBy)
        {
            // 1. Validate nhân viên tồn tại
            var nv = await _context.NvHoSos.FindAsync(nvHoSoId);
            if (nv == null) return (false, "Nhân viên không tồn tại", null);

            // 2. Kiểm tra số lượng face đã đăng ký
            var existingCount = await _context.NvFaceDatas
                .CountAsync(x => x.NvHoSoId == nvHoSoId && x.IsActive);
            
            var maxFaces = int.Parse(_config["FaceRecognition:MaxFacePerEmployee"] ?? "3");
            if (existingCount >= maxFaces)
                return (false, $"Đã đạt giới hạn {maxFaces} ảnh/nhân viên", null);

            // 3. Extract face encoding
            using var stream = imageFile.OpenReadStream();
            var encoding = await _faceService.ExtractFaceEncodingAsync(stream);
            
            if (encoding == null)
                return (false, "Không phát hiện khuôn mặt trong ảnh", null);

            // 4. Đánh giá chất lượng
            stream.Position = 0;
            var quality = await _faceService.EvaluateImageQualityAsync(stream);
            
            if (quality < 0.5)
                return (false, "Chất lượng ảnh quá thấp. Vui lòng chụp lại.", null);

            // 5. Lưu file ảnh
            var fileName = $"{nvHoSoId}_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var savePath = Path.Combine(_env.WebRootPath, "uploads/faces/registered", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);
            
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            // 6. Lưu vào DB
            var faceData = new NvFaceData
            {
                NvHoSoId = nvHoSoId,
                FaceEncoding = JsonSerializer.Serialize(encoding),
                FaceImageUrl = $"/uploads/faces/registered/{fileName}",
                QualityScore = (decimal)quality,
                IsActive = true,
                CreatedBy = createdBy,
                CreatedAt = DateTime.Now
            };

            _context.NvFaceDatas.Add(faceData);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync("FACE_REGISTER", $"Đăng ký khuôn mặt cho NV #{nvHoSoId}", createdBy);

            return (true, "Đăng ký khuôn mặt thành công", faceData.Id);
        }

        /// <summary>
        /// Lấy danh sách nhân viên đã đăng ký face
        /// </summary>
        public async Task<List<FaceDataDto>> GetRegisteredEmployeesAsync()
        {
            return await _context.NvFaceDatas
                .Where(x => x.IsActive)
                .Include(x => x.NhanVien)
                .GroupBy(x => x.NvHoSoId)
                .Select(g => new FaceDataDto
                {
                    NvHoSoId = g.Key,
                    TenNhanVien = g.First().NhanVien!.HoTen,
                    SoLuongAnh = g.Count(),
                    NgayDangKy = g.Min(x => x.CreatedAt),
                    ChatLuongTrungBinh = g.Average(x => x.QualityScore)
                })
                .ToListAsync();
        }

        // ... other CRUD methods ...
    }
}
```

---

### 2.7. Controller: FaceRecognitionController.cs

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QLNS_BE.Models.Dtos.FaceRecognition;
using QLNS_BE.Services;
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

        public FaceRecognitionController(
            FaceDataService faceDataService,
            ChamCongService chamCongService,
            IFaceRecognitionService faceService)
        {
            _faceDataService = faceDataService;
            _chamCongService = chamCongService;
            _faceService = faceService;
        }

        // ============================================================
        // 1. ĐĂNG KÝ KHUÔN MẶT
        // ============================================================
        
        /// <summary>
        /// Đăng ký khuôn mặt cho nhân viên (chỉ HR/Admin)
        /// </summary>
        [HttpPost("register/{nvId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> RegisterFace(int nvId, [FromForm] IFormFile image)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            
            var result = await _faceDataService.RegisterFaceAsync(nvId, image, userId);
            
            if (!result.success)
                return BadRequest(new { message = result.message });
            
            return Ok(new { 
                message = result.message, 
                faceId = result.faceId 
            });
        }

        /// <summary>
        /// Cập nhật/thay thế khuôn mặt
        /// </summary>
        [HttpPut("update/{nvId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> UpdateFace(int nvId, [FromForm] IFormFile image)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _faceDataService.UpdateFaceAsync(nvId, image, userId);
            
            if (!result.success)
                return BadRequest(new { message = result.message });
            
            return Ok(new { message = result.message });
        }

        /// <summary>
        /// Xóa dữ liệu khuôn mặt (soft delete)
        /// </summary>
        [HttpDelete("{nvId}")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> DeleteFaceData(int nvId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var result = await _faceDataService.DeleteFaceDataAsync(nvId, userId);
            
            if (!result)
                return NotFound(new { message = "Không tìm thấy dữ liệu khuôn mặt" });
            
            return Ok(new { message = "Xóa dữ liệu khuôn mặt thành công" });
        }

        /// <summary>
        /// Lấy danh sách nhân viên đã đăng ký khuôn mặt
        /// </summary>
        [HttpGet("registered")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetRegisteredFaces()
        {
            var data = await _faceDataService.GetRegisteredEmployeesAsync();
            return Ok(data);
        }

        // ============================================================
        // 2. CHẤM CÔNG BẰNG KHUÔN MẶT ⭐⭐⭐
        // ============================================================

        /// <summary>
        /// Chấm công VÀO bằng khuôn mặt
        /// </summary>
        [HttpPost("attendance/check-in")]
        [AllowAnonymous] // Hoặc dùng API Key authentication
        public async Task<IActionResult> CheckInByFace([FromForm] IFormFile image)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var device = Request.Headers["User-Agent"].ToString();

            var result = await _chamCongService.CheckInByFaceAsync(image, ip, device);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        /// <summary>
        /// Chấm công RA bằng khuôn mặt
        /// </summary>
        [HttpPost("attendance/check-out")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckOutByFace([FromForm] IFormFile image)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var device = Request.Headers["User-Agent"].ToString();

            var result = await _chamCongService.CheckOutByFaceAsync(image, ip, device);
            
            if (!result.Success)
                return BadRequest(result);
            
            return Ok(result);
        }

        // ============================================================
        // 3. LOGS & REPORTS
        // ============================================================

        /// <summary>
        /// Lấy lịch sử nhận diện khuôn mặt
        /// </summary>
        [HttpGet("logs")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetFaceLogs([FromQuery] FaceLogFilterDto filter)
        {
            var data = await _chamCongService.GetFaceLogsAsync(filter);
            return Ok(data);
        }

        /// <summary>
        /// Lấy thống kê nhận diện (thành công/thất bại)
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "HR_ACC,GIAM_DOC")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? from, DateTime? to)
        {
            var stats = await _chamCongService.GetFaceRecognitionStatsAsync(from, to);
            return Ok(stats);
        }

        /// <summary>
        /// Verify khuôn mặt (test - không tạo chấm công)
        /// </summary>
        [HttpPost("verify/{nvId}")]
        [Authorize]
        public async Task<IActionResult> VerifyFace(int nvId, [FromForm] IFormFile image)
        {
            using var stream = image.OpenReadStream();
            var (matchedNvId, confidence) = await _faceService.IdentifyEmployeeAsync(stream);
            
            var isMatch = matchedNvId == nvId;
            
            return Ok(new { 
                isMatch, 
                confidence,
                message = isMatch 
                    ? $"Khớp với độ tin cậy {confidence:P2}" 
                    : "Không khớp"
            });
        }
    }
}
```

---

### 2.8. Cập nhật ChamCongService.cs

Thêm 2 methods chính:

```csharp
// Thêm vào class ChamCongService

public async Task<FaceRecognitionResultDto> CheckInByFaceAsync(
    IFormFile image, 
    string? ipAddress, 
    string? deviceInfo)
{
    var startTime = DateTime.Now;
    var result = new FaceRecognitionResultDto();

    try
    {
        // 1. Nhận diện khuôn mặt
        using var stream = image.OpenReadStream();
        var (nvId, confidence) = await _faceRecognitionService.IdentifyEmployeeAsync(stream);

        // 2. Lưu log (dù thành công hay thất bại)
        var log = new ChamCongFaceLog
        {
            NvHoSoId = nvId,
            ThoiGian = DateTime.Now,
            Loai = "VAO",
            ConfidenceScore = (decimal?)confidence,
            TrangThai = nvId != null ? "THANH_CONG" : "THAT_BAI",
            LyDoThatBai = nvId == null ? "Không nhận diện được khuôn mặt" : null,
            IpAddress = ipAddress,
            DeviceInfo = deviceInfo,
            ProcessingTimeMs = (int)(DateTime.Now - startTime).TotalMilliseconds
        };

        // Lưu ảnh check-in
        var fileName = $"checkin_{DateTime.Now:yyyyMMddHHmmss}.jpg";
        var savePath = Path.Combine(_env.WebRootPath, "uploads/faces/checkin", fileName);
        // ... save logic ...
        log.FaceImageUrl = $"/uploads/faces/checkin/{fileName}";

        _context.ChamCongFaceLogs.Add(log);
        await _context.SaveChangesAsync();

        if (nvId == null)
        {
            result.Success = false;
            result.Message = "Không nhận diện được khuôn mặt. Vui lòng thử lại.";
            return result;
        }

        // 3. Tạo bản ghi chấm công
        var nv = await _context.NvHoSos.FindAsync(nvId);
        var today = DateTime.Today;
        
        // Tìm bảng công tháng hiện tại
        var bangCong = await _context.BangCongThangs
            .FirstOrDefaultAsync(x => x.Thang == today.Month && x.Nam == today.Year);

        if (bangCong == null)
        {
            result.Success = false;
            result.Message = "Chưa có bảng công tháng này";
            return result;
        }

        // Kiểm tra đã check-in chưa
        var existingChamCong = await _context.ChamCongs
            .FirstOrDefaultAsync(x => 
                x.NvHoSoId == nvId && 
                x.Ngay == today &&
                x.BangCongThangId == bangCong.Id);

        if (existingChamCong != null && existingChamCong.GioVao != null)
        {
            result.Success = false;
            result.Message = "Bạn đã check-in rồi";
            result.ThoiGianChamCong = existingChamCong.GioVao;
            return result;
        }

        // Tạo hoặc update chấm công
        if (existingChamCong == null)
        {
            existingChamCong = new ChamCong
            {
                NvHoSoId = nvId.Value,
                BangCongThangId = bangCong.Id,
                Ngay = today,
                GioVao = DateTime.Now,
                PhuongThuc = "FACE_RECOGNITION",
                FaceLogVaoId = log.Id,
                TrangThai = "CHUA_RA"
            };
            _context.ChamCongs.Add(existingChamCong);
        }
        else
        {
            existingChamCong.GioVao = DateTime.Now;
            existingChamCong.PhuongThuc = "FACE_RECOGNITION";
            existingChamCong.FaceLogVaoId = log.Id;
        }

        await _context.SaveChangesAsync();

        // Update log với cham_cong_id
        log.ChamCongId = existingChamCong.Id;
        await _context.SaveChangesAsync();

        result.Success = true;
        result.Message = $"Chào {nv?.HoTen}, check-in thành công!";
        result.NvHoSoId = nvId;
        result.TenNhanVien = nv?.HoTen;
        result.ConfidenceScore = (decimal?)confidence;
        result.ThoiGianChamCong = existingChamCong.GioVao;
        result.LoaiChamCong = "VAO";
        result.ChamCongId = existingChamCong.Id;
        result.LogId = log.Id;

        return result;
    }
    catch (Exception ex)
    {
        result.Success = false;
        result.Message = $"Lỗi hệ thống: {ex.Message}";
        return result;
    }
}

public async Task<FaceRecognitionResultDto> CheckOutByFaceAsync(
    IFormFile image, 
    string? ipAddress, 
    string? deviceInfo)
{
    // Tương tự CheckInByFaceAsync nhưng xử lý GioRa
    // ...
}
```

---

### 2.9. Cập nhật AppDbContext.cs

```csharp
public class AppDbContext : DbContext
{
    // ... existing DbSets ...

    // ⭐ NEW
    public DbSet<NvFaceData> NvFaceDatas { get; set; }
    public DbSet<ChamCongFaceLog> ChamCongFaceLogs { get; set; }
    public DbSet<FaceRecognitionConfig> FaceRecognitionConfigs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cấu hình bổ sung nếu cần
        modelBuilder.Entity<ChamCongFaceLog>()
            .HasIndex(x => new { x.ThoiGian, x.TrangThai });
    }
}
```

---

### 2.10. Cập nhật appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  
  "FaceRecognition": {
    "Provider": "DlibDotNet",
    "ConfidenceThreshold": 0.60,
    "MaxFacePerEmployee": 3,
    "ImageStoragePath": "wwwroot/uploads/faces",
    "ModelPath": "wwwroot/models",
    
    "ImageSettings": {
      "MaxSizeMB": 5,
      "AllowedExtensions": ["jpg", "jpeg", "png"],
      "ThumbnailSize": 200
    },

    "AzureFace": {
      "Endpoint": "https://yourname.cognitiveservices.azure.com/",
      "ApiKey": "",
      "Enabled": false
    }
  },

  "Attendance": {
    "AllowCheckInMinutesBefore": 30,
    "AllowCheckOutMinutesAfter": 180,
    "RequireMinConfidence": 0.60,
    "EnableDuplicateCheck": true,
    "SaveCheckInImages": true,
    "EnableLivenessDetection": false
  }
}
```

---

### 2.11. Đăng ký Services (Program.cs)

```csharp
// ... existing services ...

// ⭐ Face Recognition Services
builder.Services.AddScoped<FaceDataService>();

// Chọn 1 trong 2:
builder.Services.AddScoped<IFaceRecognitionService, DlibFaceService>();
// builder.Services.AddScoped<IFaceRecognitionService, AzureFaceService>();

// Configure settings
builder.Services.Configure<FaceRecognitionSettings>(
    builder.Configuration.GetSection("FaceRecognition"));
```

---

## 🎨 PHẦN 3: FRONTEND (ANGULAR 16)

### 3.1. NPM Packages

```bash
cd <angular-project-folder>

# Webcam capture
npm install ngx-webcam

# Image cropper
npm install ngx-image-cropper

# Loading spinner
npm install ngx-spinner

# Optional: client-side face detection
npm install @tensorflow/tfjs @tensorflow-models/blazeface
```

---

### 3.2. Cấu trúc Module & Components

```
src/app/
├── features/
│   └── face-recognition/
│       ├── face-recognition.module.ts          ⭐ NEW
│       ├── face-recognition-routing.module.ts  ⭐ NEW
│       │
│       ├── pages/
│       │   ├── face-registration/
│       │   │   ├── face-registration.component.ts
│       │   │   ├── face-registration.component.html
│       │   │   └── face-registration.component.scss
│       │   │
│       │   ├── face-attendance/                ⭐ Component chấm công
│       │   │   ├── face-attendance.component.ts
│       │   │   ├── face-attendance.component.html
│       │   │   └── face-attendance.component.scss
│       │   │
│       │   ├── face-management/                (Quản lý - HR)
│       │   │   ├── face-management.component.ts
│       │   │   └── ...
│       │   │
│       │   └── face-logs/                      (Xem logs)
│       │       └── ...
│       │
│       ├── components/
│       │   ├── webcam-capture/                 (Shared component)
│       │   │   ├── webcam-capture.component.ts
│       │   │   └── ...
│       │   │
│       │   └── face-preview/                   (Preview ảnh)
│       │       └── ...
│       │
│       └── services/
│           └── face-recognition.service.ts     ⭐ Service
│
└── core/
    └── services/
        └── attendance.service.ts                ✏️ UPDATE (nếu cần)
```

---

### 3.3. Service: face-recognition.service.ts

```typescript
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface FaceRecognitionResult {
  success: boolean;
  message: string;
  nvHoSoId?: number;
  tenNhanVien?: string;
  confidenceScore?: number;
  thoiGianChamCong?: string;
  loaiChamCong?: string;
  chamCongId?: number;
  logId?: number;
}

export interface FaceDataDto {
  nvHoSoId: number;
  tenNhanVien: string;
  soLuongAnh: number;
  ngayDangKy: string;
  chatLuongTrungBinh: number;
}

export interface FaceLogDto {
  id: number;
  nvHoSoId?: number;
  tenNhanVien?: string;
  thoiGian: string;
  loai: string;
  trangThai: string;
  confidenceScore?: number;
  faceImageUrl?: string;
  lyDoThatBai?: string;
}

@Injectable({
  providedIn: 'root'
})
export class FaceRecognitionService {
  private apiUrl = `${environment.apiUrl}/face-recognition`;

  constructor(private http: HttpClient) {}

  // ============== ĐĂNG KÝ KHUÔN MẶT ==============
  
  registerFace(nvId: number, imageFile: File): Observable<any> {
    const formData = new FormData();
    formData.append('image', imageFile);
    return this.http.post(`${this.apiUrl}/register/${nvId}`, formData);
  }

  updateFace(nvId: number, imageFile: File): Observable<any> {
    const formData = new FormData();
    formData.append('image', imageFile);
    return this.http.put(`${this.apiUrl}/update/${nvId}`, formData);
  }

  deleteFaceData(nvId: number): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${nvId}`);
  }

  getRegisteredFaces(): Observable<FaceDataDto[]> {
    return this.http.get<FaceDataDto[]>(`${this.apiUrl}/registered`);
  }

  // ============== CHẤM CÔNG ⭐ ==============
  
  checkInByFace(imageFile: File): Observable<FaceRecognitionResult> {
    const formData = new FormData();
    formData.append('image', imageFile);
    return this.http.post<FaceRecognitionResult>(
      `${this.apiUrl}/attendance/check-in`, 
      formData
    );
  }

  checkOutByFace(imageFile: File): Observable<FaceRecognitionResult> {
    const formData = new FormData();
    formData.append('image', imageFile);
    return this.http.post<FaceRecognitionResult>(
      `${this.apiUrl}/attendance/check-out`, 
      formData
    );
  }

  // ============== LOGS & STATS ==============
  
  getFaceLogs(filter: any): Observable<any> {
    return this.http.get(`${this.apiUrl}/logs`, { params: filter });
  }

  getStatistics(from?: Date, to?: Date): Observable<any> {
    const params: any = {};
    if (from) params.from = from.toISOString();
    if (to) params.to = to.toISOString();
    return this.http.get(`${this.apiUrl}/statistics`, { params });
  }

  verifyFace(nvId: number, imageFile: File): Observable<any> {
    const formData = new FormData();
    formData.append('image', imageFile);
    return this.http.post(`${this.apiUrl}/verify/${nvId}`, formData);
  }
}
```

---

### 3.4. Component: face-attendance.component.ts (CHẤM CÔNG)

```typescript
import { Component, OnInit, OnDestroy } from '@angular/core';
import { WebcamImage, WebcamInitError } from 'ngx-webcam';
import { Subject, Observable } from 'rxjs';
import { FaceRecognitionService, FaceRecognitionResult } from '../../services/face-recognition.service';
import { NgxSpinnerService } from 'ngx-spinner';

@Component({
  selector: 'app-face-attendance',
  templateUrl: './face-attendance.component.html',
  styleUrls: ['./face-attendance.component.scss']
})
export class FaceAttendanceComponent implements OnInit, OnDestroy {
  // Webcam
  public webcamImage: WebcamImage | null = null;
  public showWebcam = true;
  public errors: WebcamInitError[] = [];
  
  // Trigger
  private trigger: Subject<void> = new Subject<void>();
  
  // State
  public isProcessing = false;
  public result: FaceRecognitionResult | null = null;
  public mode: 'checkin' | 'checkout' = 'checkin';
  public currentTime = new Date();
  
  // Timer
  private timeInterval: any;

  constructor(
    private faceService: FaceRecognitionService,
    private spinner: NgxSpinnerService
  ) {}

  ngOnInit(): void {
    this.updateTime();
    this.timeInterval = setInterval(() => this.updateTime(), 1000);
  }

  ngOnDestroy(): void {
    if (this.timeInterval) {
      clearInterval(this.timeInterval);
    }
  }

  updateTime(): void {
    this.currentTime = new Date();
  }

  // ============== WEBCAM ==============
  
  public get triggerObservable(): Observable<void> {
    return this.trigger.asObservable();
  }

  public triggerSnapshot(): void {
    this.trigger.next();
  }

  public handleImage(webcamImage: WebcamImage): void {
    this.webcamImage = webcamImage;
    console.log('Captured image', webcamImage);
  }

  public handleInitError(error: WebcamInitError): void {
    this.errors.push(error);
    console.error('Webcam error:', error);
  }

  // ============== CHẤM CÔNG ==============
  
  public captureAndCheckIn(): void {
    this.mode = 'checkin';
    this.triggerSnapshot();
    
    // Đợi 100ms để đảm bảo ảnh đã được capture
    setTimeout(() => {
      if (this.webcamImage) {
        this.processAttendance('checkin');
      }
    }, 100);
  }

  public captureAndCheckOut(): void {
    this.mode = 'checkout';
    this.triggerSnapshot();
    
    setTimeout(() => {
      if (this.webcamImage) {
        this.processAttendance('checkout');
      }
    }, 100);
  }

  private processAttendance(type: 'checkin' | 'checkout'): void {
    if (!this.webcamImage || this.isProcessing) {
      return;
    }

    this.isProcessing = true;
    this.result = null;
    this.spinner.show();

    const blob = this.dataURItoBlob(this.webcamImage.imageAsDataUrl);
    const file = new File([blob], 'face.jpg', { type: 'image/jpeg' });

    const request$ = type === 'checkin' 
      ? this.faceService.checkInByFace(file)
      : this.faceService.checkOutByFace(file);

    request$.subscribe({
      next: (res: FaceRecognitionResult) => {
        this.result = res;
        
        if (res.success) {
          this.playSuccessSound();
          // Optional: Auto reset sau 3s
          setTimeout(() => this.reset(), 3000);
        } else {
          this.playErrorSound();
        }
      },
      error: (err) => {
        this.result = {
          success: false,
          message: 'Lỗi kết nối. Vui lòng thử lại.'
        };
        this.playErrorSound();
      },
      complete: () => {
        this.isProcessing = false;
        this.spinner.hide();
      }
    });
  }

  // ============== HELPERS ==============
  
  private dataURItoBlob(dataURI: string): Blob {
    const byteString = atob(dataURI.split(',')[1]);
    const mimeString = dataURI.split(',')[0].split(':')[1].split(';')[0];
    const ab = new ArrayBuffer(byteString.length);
    const ia = new Uint8Array(ab);
    
    for (let i = 0; i < byteString.length; i++) {
      ia[i] = byteString.charCodeAt(i);
    }
    
    return new Blob([ab], { type: mimeString });
  }

  public reset(): void {
    this.webcamImage = null;
    this.result = null;
    this.showWebcam = false;
    setTimeout(() => this.showWebcam = true, 100);
  }

  private playSuccessSound(): void {
    const audio = new Audio('assets/sounds/success.mp3');
    audio.play().catch(err => console.log('Audio play failed', err));
  }

  private playErrorSound(): void {
    const audio = new Audio('assets/sounds/error.mp3');
    audio.play().catch(err => console.log('Audio play failed', err));
  }

  public get resultClass(): string {
    if (!this.result) return '';
    return this.result.success ? 'success' : 'error';
  }

  public get confidencePercent(): number {
    if (!this.result?.confidenceScore) return 0;
    return Math.round(this.result.confidenceScore * 100);
  }
}
```

---

### 3.5. Template: face-attendance.component.html

```html
<div class="face-attendance-container">
  <!-- Header -->
  <div class="header">
    <h1>🎭 Chấm công bằng khuôn mặt</h1>
    <div class="current-time">
      <i class="bi bi-clock"></i>
      {{ currentTime | date:'HH:mm:ss - dd/MM/yyyy' }}
    </div>
  </div>

  <div class="content">
    <div class="webcam-section">
      <!-- Webcam -->
      <div class="webcam-wrapper">
        <webcam 
          *ngIf="showWebcam"
          [trigger]="triggerObservable" 
          (imageCapture)="handleImage($event)"
          (initError)="handleInitError($event)"
          [width]="640"
          [height]="480"
          [imageQuality]="1"
          imageType="image/jpeg">
        </webcam>

        <!-- Overlay guide -->
        <div class="face-guide-overlay">
          <div class="face-oval">
            <p>Đặt khuôn mặt vào khung</p>
          </div>
        </div>

        <!-- Error -->
        <div class="webcam-error" *ngIf="errors.length > 0">
          <i class="bi bi-exclamation-triangle"></i>
          <p>Không thể truy cập camera. Vui lòng kiểm tra quyền truy cập.</p>
        </div>
      </div>

      <!-- Buttons -->
      <div class="action-buttons">
        <button 
          class="btn btn-primary btn-lg"
          (click)="captureAndCheckIn()"
          [disabled]="isProcessing || errors.length > 0">
          <i class="bi bi-box-arrow-in-right"></i>
          Chấm công VÀO
        </button>

        <button 
          class="btn btn-danger btn-lg"
          (click)="captureAndCheckOut()"
          [disabled]="isProcessing || errors.length > 0">
          <i class="bi bi-box-arrow-right"></i>
          Chấm công RA
        </button>
      </div>
    </div>

    <!-- Result Section -->
    <div class="result-section" *ngIf="result">
      <div class="result-card" [ngClass]="resultClass">
        <div class="result-icon">
          <i *ngIf="result.success" class="bi bi-check-circle-fill"></i>
          <i *ngIf="!result.success" class="bi bi-x-circle-fill"></i>
        </div>

        <div class="result-content">
          <h2 *ngIf="result.success">✅ {{ result.message }}</h2>
          <h2 *ngIf="!result.success">❌ {{ result.message }}</h2>

          <div class="result-details" *ngIf="result.success">
            <p><strong>Nhân viên:</strong> {{ result.tenNhanVien }}</p>
            <p><strong>Thời gian:</strong> {{ result.thoiGianChamCong | date:'HH:mm:ss' }}</p>
            <p><strong>Độ tin cậy:</strong> 
              <span class="confidence-badge">{{ confidencePercent }}%</span>
            </p>
          </div>

          <button class="btn btn-secondary mt-3" (click)="reset()">
            Chụp lại
          </button>
        </div>
      </div>
    </div>

    <!-- Captured Image Preview (for debugging) -->
    <div class="preview-section" *ngIf="webcamImage && !result">
      <h4>Ảnh vừa chụp:</h4>
      <img [src]="webcamImage.imageAsDataUrl" class="captured-preview" />
    </div>
  </div>

  <!-- Loading Spinner -->
  <ngx-spinner 
    type="ball-clip-rotate-multiple"
    size="large"
    [fullScreen]="true">
    <p class="spinner-text">Đang nhận diện khuôn mặt...</p>
  </ngx-spinner>
</div>
```

---

### 3.6. Styles: face-attendance.component.scss

```scss
.face-attendance-container {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;

  .header {
    text-align: center;
    margin-bottom: 30px;

    h1 {
      font-size: 2.5rem;
      color: #333;
      margin-bottom: 10px;
    }

    .current-time {
      font-size: 1.2rem;
      color: #666;
      
      i {
        margin-right: 8px;
      }
    }
  }

  .content {
    display: flex;
    gap: 30px;
    align-items: flex-start;

    @media (max-width: 992px) {
      flex-direction: column;
    }
  }

  .webcam-section {
    flex: 1;
    min-width: 0;
  }

  .webcam-wrapper {
    position: relative;
    border-radius: 12px;
    overflow: hidden;
    box-shadow: 0 4px 20px rgba(0,0,0,0.15);
    background: #000;

    webcam {
      display: block;
      width: 100%;
      height: auto;
    }

    .face-guide-overlay {
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      pointer-events: none;
      display: flex;
      align-items: center;
      justify-content: center;

      .face-oval {
        width: 300px;
        height: 400px;
        border: 3px dashed #00ff00;
        border-radius: 50%;
        display: flex;
        align-items: flex-end;
        justify-content: center;
        padding-bottom: 20px;

        p {
          color: #fff;
          background: rgba(0,0,0,0.6);
          padding: 8px 16px;
          border-radius: 20px;
          font-size: 0.9rem;
        }
      }
    }

    .webcam-error {
      padding: 40px;
      text-align: center;
      color: #fff;
      background: #dc3545;

      i {
        font-size: 3rem;
        margin-bottom: 15px;
      }

      p {
        font-size: 1.1rem;
      }
    }
  }

  .action-buttons {
    display: flex;
    gap: 15px;
    margin-top: 20px;

    button {
      flex: 1;
      padding: 15px;
      font-size: 1.1rem;
      border-radius: 8px;
      transition: all 0.3s;

      i {
        margin-right: 8px;
      }

      &:hover:not(:disabled) {
        transform: translateY(-2px);
        box-shadow: 0 4px 15px rgba(0,0,0,0.2);
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }
  }

  .result-section {
    flex: 1;
    min-width: 300px;
  }

  .result-card {
    padding: 30px;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0,0,0,0.15);
    text-align: center;
    animation: slideIn 0.3s ease-out;

    &.success {
      background: linear-gradient(135deg, #e8f5e9, #c8e6c9);
      border: 2px solid #4caf50;

      .result-icon i {
        color: #4caf50;
      }
    }

    &.error {
      background: linear-gradient(135deg, #ffebee, #ffcdd2);
      border: 2px solid #f44336;

      .result-icon i {
        color: #f44336;
      }
    }

    .result-icon {
      i {
        font-size: 4rem;
        margin-bottom: 15px;
      }
    }

    .result-content {
      h2 {
        font-size: 1.5rem;
        margin-bottom: 20px;
      }

      .result-details {
        text-align: left;
        background: rgba(255,255,255,0.8);
        padding: 15px;
        border-radius: 8px;
        margin-bottom: 15px;

        p {
          margin-bottom: 10px;
          font-size: 1rem;

          strong {
            display: inline-block;
            width: 110px;
          }
        }

        .confidence-badge {
          background: #2196f3;
          color: #fff;
          padding: 4px 12px;
          border-radius: 12px;
          font-weight: bold;
        }
      }
    }
  }

  .preview-section {
    margin-top: 20px;
    text-align: center;

    .captured-preview {
      max-width: 300px;
      border-radius: 8px;
      box-shadow: 0 2px 10px rgba(0,0,0,0.1);
    }
  }
}

@keyframes slideIn {
  from {
    opacity: 0;
    transform: translateX(20px);
  }
  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.spinner-text {
  color: #fff;
  font-size: 1.2rem;
  margin-top: 15px;
}
```

---

### 3.7. Module: face-recognition.module.ts

```typescript
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { WebcamModule } from 'ngx-webcam';
import { NgxSpinnerModule } from 'ngx-spinner';
import { ImageCropperModule } from 'ngx-image-cropper';

import { FaceRecognitionRoutingModule } from './face-recognition-routing.module';
import { FaceAttendanceComponent } from './pages/face-attendance/face-attendance.component';
import { FaceRegistrationComponent } from './pages/face-registration/face-registration.component';
import { FaceManagementComponent } from './pages/face-management/face-management.component';
import { FaceLogsComponent } from './pages/face-logs/face-logs.component';

@NgModule({
  declarations: [
    FaceAttendanceComponent,
    FaceRegistrationComponent,
    FaceManagementComponent,
    FaceLogsComponent
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    FaceRecognitionRoutingModule,
    WebcamModule,
    NgxSpinnerModule,
    ImageCropperModule
  ]
})
export class FaceRecognitionModule { }
```

---

## 📝 PHẦN 4: TRIỂN KHAI THEO THỨ TỰ

### ✅ Giai đoạn 1: Database (1 ngày)
- [ ] Tạo bảng `nv_face_data`
- [ ] Tạo bảng `cham_cong_face_log`
- [ ] Alter bảng `cham_cong` thêm các cột mới
- [ ] Tạo bảng `face_recognition_config` (optional)
- [ ] Test migration

### ✅ Giai đoạn 2: Backend Setup (1-2 ngày)
- [ ] Install NuGet packages
- [ ] Download model files (nếu dùng Dlib)
- [ ] Tạo Entities (`NvFaceData`, `ChamCongFaceLog`)
- [ ] Cập nhật `AppDbContext`
- [ ] Tạo DTOs
- [ ] Cập nhật `appsettings.json`

### ✅ Giai đoạn 3: Backend Core Logic (2-3 ngày)
- [ ] Implement `IFaceRecognitionService` interface
- [ ] Implement `DlibFaceService` (hoặc `AzureFaceService`)
- [ ] Test face encoding extraction
- [ ] Implement `FaceDataService` (CRUD)
- [ ] Test face registration flow

### ✅ Giai đoạn 4: Backend API Endpoints (2 ngày)
- [ ] Tạo `FaceRecognitionController`
- [ ] Implement endpoint đăng ký face
- [ ] Implement endpoint check-in/check-out
- [ ] Cập nhật `ChamCongService` (add face logic)
- [ ] Test tất cả endpoints bằng Postman

### ✅ Giai đoạn 5: Frontend Setup (1 ngày)
- [ ] Install NPM packages
- [ ] Tạo `FaceRecognitionModule`
- [ ] Tạo routing
- [ ] Tạo `FaceRecognitionService`

### ✅ Giai đoạn 6: Frontend UI - Chấm công (2 ngày)
- [ ] Component `FaceAttendanceComponent`
- [ ] Implement webcam capture
- [ ] Implement check-in/check-out
- [ ] UI/UX và styling
- [ ] Test frontend + backend integration

### ✅ Giai đoạn 7: Frontend UI - Quản lý (1-2 ngày)
- [ ] Component đăng ký khuôn mặt (HR)
- [ ] Component quản lý danh sách
- [ ] Component xem logs
- [ ] Dashboard/statistics

### ✅ Giai đoạn 8: Testing & Optimization (2 ngày)
- [ ] Test toàn bộ flow end-to-end
- [ ] Test edge cases (ảnh mờ, nhiều người, không có mặt)
- [ ] Tối ưu performance (caching, compression)
- [ ] Security check
- [ ] UAT với users

### ✅ Giai đoạn 9: Deployment (1 ngày)
- [ ] Deploy backend
- [ ] Deploy frontend
- [ ] Config production settings
- [ ] Training cho users
- [ ] Monitoring & logging

**Tổng thời gian ước tính: 12-15 ngày**

---

## ⚠️ LƯU Ý QUAN TRỌNG

### 1. **Lựa chọn thư viện**

| Tính năng | DlibDotNet | Azure Face API |
|-----------|------------|----------------|
| Chi phí | Miễn phí | ~$1/1000 calls |
| Internet | Offline | Cần internet |
| Độ chính xác | Tốt (85-95%) | Rất tốt (95-99%) |
| Setup | Khó | Dễ |
| Tốc độ | Nhanh | Trung bình (network) |
| Liveness detection | Không | Có |

**Khuyến nghị:** Bắt đầu với Azure (nhanh), sau chuyển sang Dlib nếu cần tiết kiệm.

### 2. **Bảo mật**

- ✅ Endpoint chấm công cần **API Key** hoặc **IP whitelist**
- ✅ Face encoding nên **mã hóa** trước khi lưu DB
- ✅ Log đầy đủ (IP, device, thời gian) để audit
- ✅ HTTPS bắt buộc
- ✅ Rate limiting (tránh brute force)
- ✅ Validate file upload (size, type, content)

### 3. **Performance**

- ✅ Cache face encodings trong memory (Redis)
- ✅ Resize ảnh trước khi xử lý (max 800x800px)
- ✅ Compress ảnh lưu trữ (JPEG quality 85%)
- ✅ Background job cho heavy processing
- ✅ CDN cho ảnh tĩnh

### 4. **UX/UI**

- ✅ Preview webcam rõ ràng
- ✅ Guide "đặt mặt vào khung"
- ✅ Feedback ngay lập tức (< 2s)
- ✅ Âm thanh thông báo (success/error)
- ✅ Auto-reset sau mỗi lần chấm công
- ✅ Responsive (mobile-first)

### 5. **Edge Cases**

| Trường hợp | Xử lý |
|-----------|-------|
| Không phát hiện mặt | Yêu cầu chụp lại |
| Nhiều mặt trong ảnh | Chọn mặt lớn nhất |
| Ảnh mờ/tối | Reject, yêu cầu ảnh tốt hơn |
| Confidence thấp | Lưu log "NGHI_NGO", cần duyệt |
| Check-in 2 lần | Block, hiển thị "đã check-in" |
| Nhân viên chưa đăng ký | Hướng dẫn liên hệ HR |

### 6. **Compliance**

- ⚖️ **GDPR/Privacy**: Thông báo thu thập dữ liệu sinh trắc học
- ⚖️ **Consent**: Nhân viên đồng ý bằng văn bản
- ⚖️ **Retention**: Quy định thời gian lưu trữ ảnh
- ⚖️ **Access control**: Chỉ HR/Admin xem được ảnh

---

## 🔧 TROUBLESHOOTING

### Lỗi thường gặp

#### Backend
```
❌ "Could not load model file"
→ Kiểm tra đường dẫn model trong appsettings.json
→ Download lại model files

❌ "Out of memory"
→ Giảm size ảnh trước khi xử lý
→ Tăng memory limit cho app

❌ "No face detected"
→ Kiểm tra chất lượng ảnh
→ Điều chỉnh threshold
```

#### Frontend
```
❌ "Camera not accessible"
→ Kiểm tra HTTPS (webcam cần HTTPS)
→ Kiểm tra permission trong browser

❌ "CORS error"
→ Cấu hình CORS trong backend
→ Thêm domain vào whitelist
```

---

## 📚 TÀI LIỆU THAM KHẢO

### Documentation
- DlibDotNet: https://github.com/takuya-takeuchi/DlibDotNet
- Azure Face API: https://docs.microsoft.com/en-us/azure/cognitive-services/face/
- ngx-webcam: https://github.com/basst314/ngx-webcam
- Face Recognition Algorithm: https://arxiv.org/abs/1503.03832

### Model Files (Dlib)
- Download: http://dlib.net/files/
  - `shape_predictor_5_face_landmarks.dat` (9.2 MB)
  - `dlib_face_recognition_resnet_model_v1.dat` (22.5 MB)

---

## ✅ CHECKLIST HOÀN THÀNH

- [ ] Database schema đã tạo và test
- [ ] Backend packages đã install
- [ ] Face recognition service hoạt động
- [ ] API endpoints đã test bằng Postman
- [ ] Frontend components đã tạo
- [ ] Webcam capture hoạt động
- [ ] Check-in/check-out flow hoàn chỉnh
- [ ] UI/UX đẹp và responsive
- [ ] Security đã implement
- [ ] Performance đã tối ưu
- [ ] Documentation hoàn thiện
- [ ] User training đã thực hiện
- [ ] Production deployment thành công

---

**Người tạo:** GitHub Copilot  
**Ngày cập nhật:** 07/02/2026  
**Version:** 1.0
