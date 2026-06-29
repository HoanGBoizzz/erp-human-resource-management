# CHUYỂN SANG INSIGHTFACE - HƯỚNG DẪN NHANH

## 🎯 Tổng quan

Đã chuyển từ **Gemini AI** sang **InsightFace (ArcFace)** cho Face Recognition vì:

✅ **Chính xác hơn:** 95-99% (thay vì 60-80%)  
✅ **Nhanh hơn:** 100-200ms (thay vì 2-5s)  
✅ **Miễn phí:** Chạy local, không giới hạn request  
✅ **Ổn định:** Không phụ thuộc quota API  

Gemini vẫn được **GIỮ LẠI** cho các tính năng khác sau này.

---

## 🚀 CÀI ĐẶT NHANH

### ✅ PHƯƠNG ÁN 1: DOCKER (KHUYÊN DÙNG - "ĐỠ ĐAU ĐẦU")

**Ưu điểm:**
- ✅ Không cần cài Python thủ công
- ✅ Không cần Visual C++ Build Tools (~6GB)
- ✅ Setup nhanh ~10 phút
- ✅ Production-ready

**Chi tiết:** [python_face_service/DOCKER_SETUP.md](python_face_service/DOCKER_SETUP.md)

```powershell
# 1. Cài Docker Desktop: https://www.docker.com/products/docker-desktop/

# 2. Build và chạy
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service
docker-compose up -d

# 3. Test
curl http://localhost:5000/health

# 4. Chạy C# Backend
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\QLNS_BE
dotnet run
```

---

### ⚙️ PHƯƠNG ÁN 2: CÀI THỦ CÔNG (Nếu không dùng Docker)

### Bước 1: Cài Python (nếu chưa có)

```powershell
# Tải Python 3.9-3.11: https://www.python.org/downloads/
# Tick "Add Python to PATH" khi install
python --version
```

### Bước 2: Setup Python Service

```powershell
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service

# Tạo virtual environment
python -m venv venv
.\venv\Scripts\Activate

# Cài dependencies (5-10 phút, ~1-2GB)
pip install -r requirements.txt
```

### Bước 3: Chạy Python Service

```powershell
python app.py
```

**Hoặc dùng script tự động:**
```powershell
.\start.bat
```

**Kết quả mong đợi:**
```
✅ [INSIGHTFACE] Model đã sẵn sàng!
📡 Server đang chạy tại http://localhost:5000
```

### Bước 4: Chạy C# Backend

```powershell
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\QLNS_BE
dotnet run
```

**Phải thấy:**
```
✅ [FACE RECOGNITION] Sử dụng InsightFace (ArcFace) với Python service
📌 [FACE RECOGNITION] Python API: http://localhost:5000
```

---

## ✅ KIỂM TRA

### Test Python Service

```powershell
curl http://localhost:5000/health
```

Response:
```json
{
  "status": "healthy",
  "service": "InsightFace Python Service",
  "ready": true
}
```

### Test End-to-End

1. **Đăng ký khuôn mặt:** FE → "Khuôn mặt của tôi" → Chụp ảnh
2. **Chấm công:** FE → "Attendance Kiosk" → Chấm công vào/ra

**Log Backend:**
```
✅ [INSIGHTFACE] Nhận diện thành công - NV #4 (Độ khớp: 96%)
```

---

## 📁 CẤU TRÚC FILES

```
QLNS_ERP_BE/
├── python_face_service/          # Python service (MỚI)
│   ├── app.py                    # Flask server
│   ├── requirements.txt          # Dependencies
│   ├── start.bat                 # Auto-start script
│   └── README.md                 # Hướng dẫn chi tiết
│
└── QLNS_BE/QLNS_BE/
    ├── Services/FaceRecognition/
    │   ├── IFaceRecognitionService.cs
    │   ├── InsightFacePythonService.cs   # Implementation MỚI ✅
    │   ├── GeminiFaceRecognitionService.cs  # GIỮ LẠI (không dùng)
    │   └── SimpleFaceRecognitionService.cs  # Fallback
    │
    ├── Program.cs                # Đã update để dùng InsightFace
    └── appsettings.json          # PythonApiUrl = http://localhost:5000
```

---

## 🔧 CONFIG

### appsettings.json

```json
{
  "FaceRecognition": {
    "ConfidenceThreshold": 0.5,
    "PythonApiUrl": "http://localhost:5000"  // ← ĐÃ THÊM
  },
  "Gemini": {
    "ApiKey": "...",  // ← GIỮ LẠI cho tương lai
    "Model": "gemini-2.5-flash"
  }
}
```

---

## 🎯 SO SÁNH

| Tính năng | Gemini AI (CŨ) | InsightFace (MỚI) |
|-----------|-----------------|-------------------|
| Accuracy | 60-80% | **95-99%** ✅ |
| Speed | 2-5s | **100-200ms** ✅ |
| Cost | $0.01-0.03/req | **Miễn phí** ✅ |
| Quota | 15 req/min | **Không giới hạn** ✅ |
| Offline | ❌ | **✅** |

---

## ❓ TROUBLESHOOTING

### Python service không chạy?

```powershell
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service
python app.py
```

### Backend báo lỗi "Cannot connect"?

1. Kiểm tra Python service đang chạy:
   ```powershell
   curl http://localhost:5000/health
   ```

2. Check port 5000 có bị chiếm:
   ```powershell
   netstat -ano | findstr :5000
   ```

### Cài dependencies lỗi?

```powershell
pip install --upgrade pip
pip install -r requirements.txt --no-cache-dir
```

---

## 📚 TÀI LIỆU

- **Chi tiết Python Service:** [python_face_service/README.md](python_face_service/README.md)
- **InsightFace GitHub:** https://github.com/deepinsight/insightface
- **ArcFace Paper:** https://arxiv.org/abs/1801.07698

---

## 🎉 KẾT LUẬN

✅ Setup hoàn tất!  
✅ Gemini vẫn được giữ lại (không dùng cho face, dùng cho tính năng khác)  
✅ InsightFace chạy local, nhanh, chính xác, miễn phí!

**BẮT ĐẦU:**
```powershell
# Terminal 1: Python Service
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service
.\start.bat

# Terminal 2: C# Backend
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\QLNS_BE
dotnet run
```
