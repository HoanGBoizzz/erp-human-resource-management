# Detailed Setup Guide for `python_face_service`

This document explains how to install, configure, and run the InsightFace-based facial recognition service located in `QLNS_ERP_BE/python_face_service`.

## 1. Overview

The Python service provides facial recognition APIs for the C# backend, including:

- Extracting 512-dimensional face embeddings
- Comparing two face embeddings
- Identifying employees from a registered employee database
- Validating image quality before recognition

**Default service port:** `5000`

---

# 2. System Requirements

## 2.1 Required

- Windows 10/11, macOS, or Linux
- Python **3.11.x** (recommended)
- Latest version of `pip`

## 2.2 Recommended

- Use a Python virtual environment (`venv`) to avoid dependency conflicts.
- Minimum **8 GB RAM**.
- Stable Internet connection during the first startup to download the InsightFace model.

## 2.3 Verify the Environment

```powershell
python --version
pip --version
```

Expected output:

- Python version **3.11.x**

---

# 3. Project Structure

The `python_face_service` directory contains the following important files:

| File | Description |
|------|-------------|
| `app.py` | Flask application entry point |
| `requirements.txt` | Python dependency list |
| `Dockerfile` | Docker image configuration |
| `docker-compose.yml` | Docker Compose configuration |
| `start.bat` | Quick startup script for Windows |
| `README.md` | Project overview |
| `DOCKER_SETUP.md` | Docker installation guide |

---

# 4. Local Installation (Without Docker)

## 4.1 Navigate to the Service Directory

```powershell
cd C:\PROJECT\QLNS_ERP_BE\python_face_service
```

Replace the path with your actual project location if necessary.

---

## 4.2 Create and Activate a Virtual Environment

```powershell
python -m venv venv

.\venv\Scripts\Activate
```

After activation, the terminal prompt should display:

```text
(venv)
```

---

## 4.3 Upgrade Package Management Tools

```powershell
python -m pip install --upgrade pip setuptools wheel
```

---

## 4.4 Install Dependencies

Install all required packages from `requirements.txt`:

```powershell
pip install -r requirements.txt
```

Main dependencies include:

- Flask
- Flask-CORS
- ONNX Runtime
- OpenCV
- NumPy
- Pillow
- Scikit-learn
- InsightFace 0.7.3

---

## 4.5 Start the Service

```powershell
python app.py
```

If the service starts successfully, the console should display messages similar to:

```text
Loading ArcFace model...
Model loaded successfully.
Server running at http://localhost:5000
```

---

# 5. Docker Installation (Recommended)

Docker is recommended for a more stable and reproducible deployment environment.

## 5.1 Install Docker Desktop

Download Docker Desktop from:

https://www.docker.com/products/docker-desktop/

Launch Docker Desktop and wait until the Docker Engine is running.

---

## 5.2 Build and Start the Service

```powershell
cd C:\PROJECT\QLNS_ERP_BE\python_face_service

docker-compose up -d
```

During the first startup, Docker may require several minutes to download the required InsightFace model.

---

## 5.3 View Service Logs

```powershell
docker-compose logs -f
```

---

## 5.4 Stop the Service

```powershell
docker-compose down
```

---

# 6. Verify the Installation

## 6.1 Health Check

```powershell
curl http://localhost:5000/health
```

Expected response:

```json
{
  "status": "healthy",
  "service": "InsightFace Python Service",
  "model": "buffalo_l (ArcFace)",
  "ready": true
}
```

---

## 6.2 Validate an Image

Endpoint:

```
POST /api/face/validate
```

Example request body:

```json
{
  "image": "<base64>"
}
```

If a valid face is detected, the response should contain:

```json
{
  "hasFace": true
}
```

---

# 7. Backend Integration

Configure the Python service URL in the C# backend:

```json
{
  "FaceRecognition": {
    "PythonApiUrl": "http://localhost:5000"
  }
}
```

After starting the backend application, verify that requests are successfully forwarded to the Python service.

---

# 8. Available API Endpoints

| Method | Endpoint | Description |
|---------|----------|-------------|
| GET | `/health` | Service health check |
| POST | `/api/face/extract` | Extract facial embeddings |
| POST | `/api/face/compare` | Compare two face embeddings |
| POST | `/api/face/identify` | Identify a registered employee |
| POST | `/api/face/validate` | Validate image quality |

---

# 9. Troubleshooting

## No module named `insightface`

```powershell
pip install insightface==0.7.3
```

---

## Unable to Connect to the Python Service

Verify that the service is running:

```powershell
curl http://localhost:5000/health
```

If the service is not running, execute:

```powershell
python app.py
```

or

```powershell
docker-compose up -d
```

---

## Python Version Compatibility Issues

Use **Python 3.11.x**.

Recreate the virtual environment:

```powershell
Deactivate

Remove-Item -Recurse -Force venv

python -m venv venv

.\venv\Scripts\Activate

pip install -r requirements.txt
```

---

## Port 5000 Is Already in Use

Find the process occupying the port:

```powershell
netstat -ano | findstr :5000
```

Terminate the process or configure the application to use another port.

---

# 10. Quick Setup on a New Machine

```powershell
cd C:\PROJECT\QLNS_ERP_BE\python_face_service

python -m venv venv

.\venv\Scripts\Activate

python -m pip install --upgrade pip setuptools wheel

pip install -r requirements.txt

python app.py
```

Open another terminal window and verify the service:

```powershell
curl http://localhost:5000/health
```

If the response contains:

```json
{
  "ready": true
}
```

the installation has been completed successfully.
