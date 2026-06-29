from flask import Flask, request, jsonify
from flask_cors import CORS
import insightface
from insightface.app import FaceAnalysis
import cv2
import numpy as np
import base64
import json
from typing import Optional, Dict, List, Tuple
import os

app = Flask(__name__)
CORS(app)

# Khởi tạo InsightFace model
face_app = None

def init_face_model():
    """Khởi tạo InsightFace model với ArcFace"""
    global face_app
    try:
        print("🔄 [INSIGHTFACE] Đang tải model ArcFace...")
        face_app = FaceAnalysis(
            name='buffalo_l',  # Model chính xác cao
            providers=['CPUExecutionProvider']  # Dùng CPU (có thể đổi sang GPU)
        )
        face_app.prepare(ctx_id=0, det_size=(640, 640))
        print("✅ [INSIGHTFACE] Model đã sẵn sàng!")
        return True
    except Exception as e:
        print(f"❌ [INSIGHTFACE] Lỗi khi tải model: {str(e)}")
        return False

def decode_image(base64_string: str) -> Optional[np.ndarray]:
    """Decode base64 string thành numpy array"""
    try:
        # Loại bỏ header nếu có
        if ',' in base64_string:
            base64_string = base64_string.split(',')[1]
        
        img_bytes = base64.b64decode(base64_string)
        nparr = np.frombuffer(img_bytes, np.uint8)
        img = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
        return img
    except Exception as e:
        print(f"❌ [DECODE] Lỗi decode ảnh: {str(e)}")
        return None

def cosine_similarity(feat1: np.ndarray, feat2: np.ndarray) -> float:
    """Tính độ tương đồng cosine giữa 2 embeddings"""
    return float(np.dot(feat1, feat2) / (np.linalg.norm(feat1) * np.linalg.norm(feat2)))

@app.route('/health', methods=['GET'])
def health():
    """Health check endpoint"""
    return jsonify({
        'status': 'healthy',
        'service': 'InsightFace Python Service',
        'model': 'buffalo_l (ArcFace)',
        'ready': face_app is not None
    })

@app.route('/api/face/extract', methods=['POST'])
def extract_face_encoding():
    """
    Trích xuất face encoding từ ảnh
    Input: { "image": "base64_string" }
    Output: { 
        "success": true,
        "encoding": [...],  // 512-dim embedding
        "hasFace": true,
        "quality": 0.95,
        "bbox": [x,y,w,h]
    }
    """
    try:
        data = request.get_json()
        if not data or 'image' not in data:
            return jsonify({'success': False, 'message': 'Missing image data'}), 400
        
        # Decode image
        img = decode_image(data['image'])
        if img is None:
            return jsonify({'success': False, 'message': 'Invalid image format'}), 400
        
        # Detect faces
        faces = face_app.get(img)
        
        if len(faces) == 0:
            return jsonify({
                'success': True,
                'hasFace': False,
                'message': 'Không phát hiện khuôn mặt trong ảnh'
            })
        
        # Lấy khuôn mặt đầu tiên (hoặc khuôn mặt lớn nhất)
        face = faces[0]
        if len(faces) > 1:
            # Nếu có nhiều khuôn mặt, chọn khuôn mặt lớn nhất
            face = max(faces, key=lambda f: (f.bbox[2] - f.bbox[0]) * (f.bbox[3] - f.bbox[1]))
        
        # Trích xuất embedding (512-dim)
        encoding = face.embedding.tolist()
        
        # Đánh giá chất lượng dựa trên detection score
        quality = float(face.det_score)
        
        # Bounding box
        bbox = face.bbox.tolist()
        
        return jsonify({
            'success': True,
            'hasFace': True,
            'encoding': encoding,
            'quality': quality,
            'bbox': bbox,
            'age': int(face.age) if hasattr(face, 'age') else None,
            'gender': 'male' if face.gender == 1 else 'female' if hasattr(face, 'gender') else None
        })
        
    except Exception as e:
        print(f"❌ [EXTRACT] Lỗi: {str(e)}")
        return jsonify({'success': False, 'message': str(e)}), 500

@app.route('/api/face/compare', methods=['POST'])
def compare_faces():
    """
    So sánh 2 face encodings
    Input: { 
        "encoding1": [...],
        "encoding2": [...]
    }
    Output: {
        "success": true,
        "similarity": 0.85
    }
    """
    try:
        data = request.get_json()
        if not data or 'encoding1' not in data or 'encoding2' not in data:
            return jsonify({'success': False, 'message': 'Missing encoding data'}), 400
        
        enc1 = np.array(data['encoding1'])
        enc2 = np.array(data['encoding2'])
        
        similarity = cosine_similarity(enc1, enc2)
        
        return jsonify({
            'success': True,
            'similarity': similarity
        })
        
    except Exception as e:
        print(f"❌ [COMPARE] Lỗi: {str(e)}")
        return jsonify({'success': False, 'message': str(e)}), 500

@app.route('/api/face/identify', methods=['POST'])
def identify_face():
    """
    Nhận diện khuôn mặt từ danh sách encodings
    Input: {
        "image": "base64_string",
        "encodings": [
            {"id": 1, "encoding": [...]},
            {"id": 2, "encoding": [...]}
        ],
        "threshold": 0.5
    }
    Output: {
        "success": true,
        "matched": true,
        "employeeId": 1,
        "similarity": 0.87
    }
    """
    try:
        data = request.get_json()
        if not data or 'image' not in data:
            return jsonify({'success': False, 'message': 'Missing image data'}), 400
        
        # Extract encoding từ ảnh mới
        img = decode_image(data['image'])
        if img is None:
            return jsonify({'success': False, 'message': 'Invalid image format'}), 400
        
        faces = face_app.get(img)
        if len(faces) == 0:
            return jsonify({
                'success': True,
                'matched': False,
                'message': 'Không phát hiện khuôn mặt'
            })
        
        face = faces[0]
        if len(faces) > 1:
            face = max(faces, key=lambda f: (f.bbox[2] - f.bbox[0]) * (f.bbox[3] - f.bbox[1]))
        
        query_encoding = face.embedding
        
        # So sánh với danh sách encodings
        encodings = data.get('encodings', [])
        threshold = data.get('threshold', 0.5)
        
        best_match_id = None
        best_similarity = 0.0
        
        for item in encodings:
            emp_id = item['id']
            emp_encoding = np.array(item['encoding'])
            
            similarity = cosine_similarity(query_encoding, emp_encoding)
            
            if similarity > best_similarity:
                best_similarity = similarity
                best_match_id = emp_id
        
        matched = best_similarity >= threshold
        
        return jsonify({
            'success': True,
            'matched': matched,
            'employeeId': best_match_id if matched else None,
            'similarity': float(best_similarity),
            'quality': float(face.det_score)
        })
        
    except Exception as e:
        print(f"❌ [IDENTIFY] Lỗi: {str(e)}")
        return jsonify({'success': False, 'message': str(e)}), 500

@app.route('/api/face/validate', methods=['POST'])
def validate_face():
    """
    Validate ảnh có khuôn mặt và chất lượng
    Input: { "image": "base64_string" }
    Output: {
        "success": true,
        "hasFace": true,
        "quality": 0.95,
        "isGoodQuality": true
    }
    """
    try:
        data = request.get_json()
        if not data or 'image' not in data:
            return jsonify({'success': False, 'message': 'Missing image data'}), 400
        
        img = decode_image(data['image'])
        if img is None:
            return jsonify({'success': False, 'message': 'Invalid image format'}), 400
        
        faces = face_app.get(img)
        
        if len(faces) == 0:
            return jsonify({
                'success': True,
                'hasFace': False,
                'quality': 0.0,
                'isGoodQuality': False
            })
        
        face = faces[0]
        quality = float(face.det_score)
        min_quality = data.get('minQuality', 0.4)
        
        return jsonify({
            'success': True,
            'hasFace': True,
            'quality': quality,
            'isGoodQuality': quality >= min_quality,
            'faceCount': len(faces)
        })
        
    except Exception as e:
        print(f"❌ [VALIDATE] Lỗi: {str(e)}")
        return jsonify({'success': False, 'message': str(e)}), 500

if __name__ == '__main__':
    print("=" * 60)
    print("🚀 INSIGHTFACE PYTHON SERVICE")
    print("=" * 60)
    
    # Khởi tạo model
    if not init_face_model():
        print("❌ Không thể khởi tạo model. Thoát.")
        exit(1)
    
    print("\n📡 Server đang chạy tại http://localhost:5000")
    print("📖 API Endpoints:")
    print("   - GET  /health              - Health check")
    print("   - POST /api/face/extract    - Trích xuất encoding")
    print("   - POST /api/face/compare    - So sánh 2 encodings")
    print("   - POST /api/face/identify   - Nhận diện khuôn mặt")
    print("   - POST /api/face/validate   - Validate ảnh\n")
    
    app.run(host='0.0.0.0', port=5000, debug=False)
