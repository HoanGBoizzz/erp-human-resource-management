@echo off
echo ============================================================
echo INSIGHTFACE PYTHON SERVICE - AUTO START
echo ============================================================
echo.

REM Kich hoat virtual environment neu co
if exist venv\Scripts\activate.bat (
    echo [1/3] Kich hoat virtual environment...
    call venv\Scripts\activate.bat
) else (
    echo [!] Virtual environment chua duoc tao.
    echo [!] Chay lenh: python -m venv venv
    echo [!] Sau do chay lai script nay.
    pause
    exit /b 1
)

REM Kiem tra dependencies
echo [2/3] Kiem tra dependencies...
python -c "import insightface" 2>nul
if errorlevel 1 (
    echo [!] InsightFace chua duoc cai dat.
    echo [!] Dang cai dat dependencies...
    pip install -r requirements.txt
    if errorlevel 1 (
        echo [!] Loi khi cai dat dependencies.
        pause
        exit /b 1
    )
)

REM Chay server
echo [3/3] Khoi dong server...
echo.
python app.py

pause
