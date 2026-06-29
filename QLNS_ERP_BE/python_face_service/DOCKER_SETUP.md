# 🐳 DOCKER SETUP - GIẢI PHÁP "ĐỠ ĐAU ĐẦU" ✅

## Tại sao dùng Docker?

❌ **Vấn đề với cài thủ công trên Windows:**
- Cần Visual C++ Build Tools (~6GB)
- Build native extensions lâu
- Python version conflicts (3.13 vs 3.11)
- Unicode path issues (Đ, Ồ, Á...)

✅ **Docker giải quyết:**
- Không cần Visual C++ Build Tools
- Python 3.11 + InsightFace đã build sẵn
- Chạy độc lập, không conflict
- Setup 1 lần, chạy mãi mãi

---

## 🚀 CÁCH 1: Docker Desktop (Dễ nhất)

### Bước 1: Cài Docker Desktop

1. Download: https://www.docker.com/products/docker-desktop/
2. Install Docker Desktop for Windows
3. Restart máy
4. Mở Docker Desktop → Chờ Docker engine start

### Bước 2: Build và chạy service

```powershell
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service

# Build image (lần đầu mất ~5-10 phút)
docker-compose build

# Chạy service (chỉ mất vài giây)
docker-compose up -d
```

**⏳ LẦN ĐẦU TIÊN:** Container sẽ mất ~2-3 phút để download model buffalo_l (~281MB). Các lần sau start ngay lập tức vì model đã được cache trong Docker volume.

### Bước 3: Kiểm tra

```powershell
# Xem logs (theo dõi download progress)
docker-compose logs -f

# Chờ thấy: "✅ [INSIGHTFACE] Model đã sẵn sàng!"
# Sau đó Ctrl+C để thoát logs

# Test health check
curl http://localhost:5000/health
```

**Kết quả mong đợi:**
```json
{
  "status": "healthy",
  "service": "InsightFace Python Service",
  "model": "buffalo_l (ArcFace)",
  "ready": true
}
```

---

## 🎯 CÁCH 2: Docker CLI (Không cần Docker Desktop)

### Bước 1: Build image

```powershell
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service

docker build -t insightface-service .
```

### Bước 2: Chạy container

```powershell
docker run -d \
  --name insightface_python_service \
  -p 5000:5000 \
  -v insightface_models:/root/.insightface \
  --restart unless-stopped \
  insightface-service
```

### Bước 3: Kiểm tra

```powershell
# Xem logs
docker logs -f insightface_python_service

# Test
curl http://localhost:5000/health
```

---

## 📋 LỆNH QUẢN LÝ

### Docker Compose

```powershell
# Khởi động service
docker-compose up -d

# Dừng service
docker-compose down

# Xem logs real-time
docker-compose logs -f

# Restart service
docker-compose restart

# Rebuild sau khi sửa code
docker-compose up -d --build

# Xóa tất cả (bao gồm volumes)
docker-compose down -v
```

### Docker CLI

```powershell
# Start container
docker start insightface_python_service

# Stop container
docker stop insightface_python_service

# Xem logs
docker logs -f insightface_python_service

# Restart
docker restart insightface_python_service

# Xóa container
docker rm -f insightface_python_service

# Xóa image
docker rmi insightface-service
```

---

## 🔧 PRODUCTION DEPLOYMENT

### Chạy khi Windows khởi động

**PowerShell (Run as Admin):**

```powershell
# Tạo scheduled task để start Docker container khi boot
$action = New-ScheduledTaskAction -Execute "docker" -Argument "start insightface_python_service"
$trigger = New-ScheduledTaskTrigger -AtStartup
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName "InsightFace Docker Start" -Action $action -Trigger $trigger -Principal $principal
```

### Hoặc dùng Docker Compose restart policy

Đã có sẵn trong `docker-compose.yml`:
```yaml
restart: unless-stopped
```

Nghĩa là container tự động restart khi:
- Docker engine restart
- Container bị crash
- Windows reboot

---

## 🐛 TROUBLESHOOTING

### Lỗi: "docker: command not found"

**Giải pháp:**
1. Cài Docker Desktop: https://www.docker.com/products/docker-desktop/
2. Restart PowerShell
3. Kiểm tra: `docker --version`

### Lỗi: "Cannot connect to Docker daemon"

**Giải pháp:**
1. Mở Docker Desktop
2. Chờ Docker engine start (icon màu xanh)
3. Thử lại

### Lỗi: Port 5000 đã được sử dụng

**Giải pháp 1:** Đổi port trong `docker-compose.yml`

```yaml
ports:
  - "5001:5000"  # Đổi 5000 → 5001
```

**Giải pháp 2:** Tìm và kill process đang dùng port 5000

```powershell
# Tìm process
netstat -ano | findstr :5000

# Kill process (thay <PID>)
taskkill /PID <PID> /F
```

### Container bị crash liên tục

**Kiểm tra logs:**

```powershell
docker-compose logs insightface
```

**Rebuild từ đầu:**

```powershell
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

### Model download chậm (lần đầu chạy)

**Bình thường!** Model buffalo_l (~281MB) tự động download lần đầu tiên, mất ~2-3 phút.

**Theo dõi progress:**

```powershell
docker logs -f insightface_python_service
```

**Chờ thấy:**
```
✅ [INSIGHTFACE] Model đã sẵn sàng!
📡 Server đang chạy tại http://localhost:5000
```

Sau lần đầu, model được cache trong Docker volume `insightface_models` → các lần sau start ngay lập tức (~5 giây).

---

## ⚙️ TÙY CHỈNH

### Thay đổi cấu hình Flask

Sửa `app.py` → Rebuild:

```powershell
docker-compose up -d --build
```

### Mount code để development

`docker-compose.yml` thêm volume:

```yaml
volumes:
  - ./app.py:/app/app.py  # Hot reload khi sửa code
  - insightface_models:/root/.insightface
```

Chạy với debug mode:

```yaml
environment:
  - FLASK_ENV=development
  - FLASK_DEBUG=1
```

---

## 📊 SO SÁNH

| Tiêu chí | Cài thủ công | Docker |
|----------|-------------|--------|
| **Setup time** | 30-60 phút | 10-15 phút |
| **Disk space** | ~6GB (Visual C++) | ~2GB (Docker image) |
| **Build native** | ✅ Có (phức tạp) | ❌ Không cần |
| **Python version** | Phải 3.11 chính xác | Tự động (trong image) |
| **Conflicts** | Có thể có | Không (isolated) |
| **Update** | Cài lại từ đầu | `docker-compose pull` |
| **Cross-platform** | Windows only | ✅ Win/Mac/Linux |

---

## 🎉 KẾT LUẬN

✅ **Docker là giải pháp TỐI ƯU cho production:**
- Setup nhanh, ít lỗi
- Không cần Visual C++ Build Tools
- Chạy tự động khi khởi động
- Dễ update và rollback
- Portable (chạy được trên bất kỳ máy nào có Docker)

**BẮT ĐẦU NGAY:**

```powershell
# 1. Cài Docker Desktop
# Download: https://www.docker.com/products/docker-desktop/

# 2. Build và chạy
cd c:\ĐỒ_ÁN_TN\QLNS_ERP_BE\python_face_service
docker-compose up -d

# 3. Kiểm tra
curl http://localhost:5000/health
```

**SAU ĐÓ:** Chạy C# backend như bình thường, nó sẽ tự động kết nối với Python service trong Docker! 🚀
