#!/bin/bash
# =====================================================
# deploy.sh - Deploy QLNS ERP toàn bộ lên VPS
# Chạy: chmod +x deploy.sh && ./deploy.sh
# =====================================================

set -e

VPS_IP="YOUR_VPS_IP"          # ← Thay bằng IP VPS của bạn
FE_DIR="../QLNS_ERP_FE"        # Đường dẫn tới Angular project

echo "======================================"
echo "  QLNS ERP - Deploy Script"
echo "======================================"

# --- 1. Build Angular với đúng apiBaseUrl ---------------
echo ""
echo "[1/3] Build Angular Frontend..."
cd "$FE_DIR"

# Ghi đúng IP vào environment.ts
sed -i "s|http://YOUR_VPS_IP_OR_DOMAIN|http://$VPS_IP|g" src/environments/environment.ts

npm install --legacy-peer-deps
npx ng build --configuration production --output-path ../QLNS_ERP_BE/frontend_dist
echo "✅ Angular build xong -> ../QLNS_ERP_BE/frontend_dist"

cd ../QLNS_ERP_BE

# --- 2. Build & chạy Docker Compose ---------------------
echo ""
echo "[2/3] Docker Compose build & up..."
docker compose -f docker-compose.prod.yml build --no-cache
docker compose -f docker-compose.prod.yml up -d
echo "✅ Tất cả containers đã khởi động"

# --- 3. Kiểm tra ----------------------------------------
echo ""
echo "[3/3] Kiểm tra trạng thái..."
sleep 5
docker compose -f docker-compose.prod.yml ps

echo ""
echo "======================================"
echo "✅ Deploy hoàn tất!"
echo "   Web:  http://$VPS_IP"
echo "   API:  http://$VPS_IP/api"
echo "======================================"
