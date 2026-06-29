# Hướng Dẫn Setup Chi Tiết `python_face_service`

Tài liệu này hướng dẫn cài đặt và chạy dịch vụ nhận diện khuôn mặt bằng InsightFace trong thư mục `QLNS_ERP_BE/python_face_service`.

## 1. Mục tiêu

Service Python này cung cấp API xử lý khuôn mặt cho Backend C#:

- Trích xuất embedding (512 chiều)
- So sánh 2 embedding
- Nhận diện khuôn mặt từ danh sách nhân viên
- Validate chất lượng ảnh

Cổng mặc định: `5000`

---

## 2. Yêu cầu hệ thống

### 2.1. Bắt buộc

- Windows 10/11, macOS hoặc Linux
- Python: **3.11.x** (khuyến nghị)
- pip mới

### 2.2. Khuyến nghị thêm

- Dùng virtual environment (`venv`) để tránh xung đột thư viện
- RAM tối thiểu 8GB
- Kết nối mạng ổn định lần đầu để tải model InsightFace

### 2.3. Kiểm tra nhanh

```powershell
python --version
pip --version
```

Kỳ vọng:

- Python hiển thị `3.11.x`

---

## 3. Cấu trúc thư mục liên quan

Trong `python_face_service` hiện có các file chính:

- `app.py`: mã nguồn Flask API
- `requirements.txt`: danh sách thư viện Python
- `Dockerfile`: đóng gói container
- `docker-compose.yml`: chạy service bằng Docker Compose
- `start.bat`: script chạy nhanh trên Windows
- `README.md`, `DOCKER_SETUP.md`: tài liệu tham khảo bổ sung

---

## 4. Setup theo cách Local (không Docker)

## 4.1. Di chuyển vào thư mục service

```powershell
cd C:\ĐỒ_ÁN_TN\QLNS\QLNS_ERP_BE\python_face_service
```

## 4.2. Tạo và kích hoạt virtual environment

```powershell
python -m venv venv
.\venv\Scripts\Activate
```

Sau khi kích hoạt thành công, terminal sẽ có tiền tố `(venv)`.

## 4.3. Nâng cấp công cụ cài gói

```powershell
python -m pip install --upgrade pip setuptools wheel
```

## 4.4. Cài dependencies

Cách chuẩn theo file `requirements.txt`:

```powershell
pip install -r requirements.txt
```

Danh sách gói chính đang dùng:

- flask>=3.0.0
- flask-cors>=4.0.0
- onnxruntime>=1.20.0
- opencv-python>=4.8.0
- numpy
- pillow
- scikit-learn
- insightface==0.7.3

## 4.5. Chạy service

```powershell
python app.py
```

Khi chạy thành công, console hiển thị các dòng tương tự:

- Đang tải model ArcFace
- Model đã sẵn sàng
- Server chạy tại `http://localhost:5000`

---

## 5. Setup theo cách Docker (khuyến nghị cho môi trường ổn định)

## 5.1. Cài Docker Desktop

- Tải tại: https://www.docker.com/products/docker-desktop/
- Mở Docker Desktop và chờ engine chạy xong

## 5.2. Build và chạy bằng Docker Compose

```powershell
cd C:\ĐỒ_ÁN_TN\QLNS\QLNS_ERP_BE\python_face_service
docker-compose up -d
```

Lần đầu có thể mất vài phút do tải model.

## 5.3. Kiểm tra logs

```powershell
docker-compose logs -f
```

## 5.4. Dừng service

```powershell
docker-compose down
```

---

## 6. Kiểm tra service sau setup

## 6.1. Health check

```powershell
curl http://localhost:5000/health
```

Kỳ vọng JSON tương tự:

```json
{
  "status": "healthy",
  "service": "InsightFace Python Service",
  "model": "buffalo_l (ArcFace)",
  "ready": true
}
```

## 6.2. Test endpoint validate (gợi ý)

- Endpoint: `POST /api/face/validate`
- Body: `{ "image": "<base64>" }`

Nếu ảnh hợp lệ và có khuôn mặt, response có `hasFace: true`.

---

## 7. Tích hợp với Backend C#

Trong backend (`QLNS_BE`), cấu hình URL service Python:

```json
{
  "FaceRecognition": {
    "PythonApiUrl": "http://localhost:5000"
  }
}
```

Sau đó chạy backend và kiểm tra log gọi API Python.

---

## 8. Danh sách API hiện có

- `GET /health`
- `POST /api/face/extract`
- `POST /api/face/compare`
- `POST /api/face/identify`
- `POST /api/face/validate`

---

## 9. Lỗi thường gặp và cách xử lý

## 9.1. `No module named 'insightface'`

```powershell
pip install insightface==0.7.3
```

## 9.2. Chạy được Python nhưng gọi API bị lỗi kết nối

- Kiểm tra service đã chạy chưa:

```powershell
curl http://localhost:5000/health
```

- Nếu chưa chạy: chạy lại `python app.py` hoặc `docker-compose up -d`

## 9.3. Lỗi không tương thích phiên bản Python

- Dùng Python 3.11.x
- Tạo lại venv và cài lại dependencies

```powershell
Deactivate
Remove-Item -Recurse -Force venv
python -m venv venv
.\venv\Scripts\Activate
pip install -r requirements.txt
```

## 9.4. Port 5000 bị chiếm

- Tìm tiến trình:

```powershell
netstat -ano | findstr :5000
```

- Đổi port trong app hoặc giải phóng process đang chiếm cổng

---

## 10. Quy trình nhanh cho máy mới

```powershell
cd C:\ĐỒ_ÁN_TN\QLNS\QLNS_ERP_BE\python_face_service
python -m venv venv
.\venv\Scripts\Activate
python -m pip install --upgrade pip setuptools wheel
pip install -r requirements.txt
python app.py
```

Mở tab terminal mới để test:

```powershell
curl http://localhost:5000/health
```

Nếu trả về `ready: true` thì setup thành công.
