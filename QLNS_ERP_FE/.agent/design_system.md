# 🎨 QLNS ERP - Design System (Clean UI)

## 1. Triết lý thiết kế (Design Philosophy)
- **Clean & Modern**: Sử dụng nhiều khoảng trắng (whitespace), bo góc mềm mại (12px).
- **White Cards**: Nền trang màu xám nhạt (`#f3f6f9`), các khối nội dung là thẻ trắng (`#ffffff`) nổi bật.
- **Interactive**: Hiệu ứng hover nhẹ nhàng (nổi lên, đổ bóng sâu hơn) để tăng trải nghiệm người dùng.
- **Consistent**: Đồng bộ về màu sắc, font chữ và spacing.

## 2. Bảng màu (Color Palette)

| Tên biến | Mã màu | Sử dụng |
|----------|--------|---------|
| `$primary` | `#2563eb` | Màu chủ đạo (Blue), nút bấm, link |
| `$bg-page` | `#f3f6f9` | Nền trang (Light Gray Blue) |
| `$card-bg` | `#ffffff` | Nền thẻ (White) |
| `$text-dark` | `#1e293b` | Tiêu đề, nội dung chính |
| `$text-muted` | `#64748b` | Text phụ, label |
| `$border-color` | `#e2e8f0` | Đường viền nhẹ |

## 3. Components (SCSS)

### 3.1. Header Card
Thẻ tiêu đề đầu trang, chứa Title, Subtitle và các nút hành động.

```scss
.header-card {
  background: $card-bg;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  padding: 24px 28px;
  margin-bottom: 24px;
}
```

### 3.2. Content Card (Thẻ nội dung)
Thẻ chứa thông tin chính, có hiệu ứng hover.

```scss
.content-card {
  background: $card-bg;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  padding: 24px;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  border: 1px solid transparent;

  &:hover {
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1); // Bóng đổ sâu hơn
    transform: translateY(-2px); // Nổi lên nhẹ
    border-color: rgba($primary, 0.1);
  }
}
```

### 3.3. KPI Card (Thẻ thống kê)
Thẻ hiển thị chỉ số KPI với hiệu ứng "Slide-in" gradient khi hover.

**Cấu trúc HTML:**
```html
<div class="kpi-card kpi-primary"> <!-- hoặc kpi-success, kpi-warning, kpi-danger, kpi-info -->
  <div class="kpi-icon">
    <i class="bi bi-wallet2"></i>
  </div>
  <div class="kpi-content">
    <div class="kpi-label">Tổng lương năm</div>
    <div class="kpi-value">{{ value | number }}</div>
    <div class="kpi-unit">đ</div> <!-- Optional -->
  </div>
</div>
```

**SCSS:**
```scss
.kpi-card {
  position: relative;
  background: $card-bg;
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
  z-index: 0;
  
  // Slide-in gradient background
  &::before {
    content: '';
    position: absolute;
    inset: 0;
    transform: translateX(-105%);
    transition: transform 0.35s ease;
    z-index: -1;
  }

  &:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
    
    &::before { transform: translateX(0); } // Gradient trượt từ trái sang phải
    
    .kpi-icon {
      background: rgba(255, 255, 255, 0.2) !important;
      color: white !important;
    }
    
    .kpi-label, .kpi-value, .kpi-unit {
      color: white !important; // Text chuyển sang trắng
    }
  }
  
  .kpi-icon {
    width: 56px;
    height: 56px;
    border-radius: 12px;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 24px;
    transition: all 0.3s ease;
  }
  
  .kpi-content {
    flex: 1;
  }
  
  .kpi-label {
    display: block;
    font-size: 0.75rem;
    color: #64748b;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    font-weight: 600;
    margin-bottom: 0.5rem;
    transition: color 0.3s ease;
  }
  
  .kpi-value {
    display: block;
    font-size: 1.5rem;
    font-weight: 700;
    color: #1e293b;
    line-height: 1;
    transition: color 0.3s ease;
  }
  
  .kpi-unit {
    display: block;
    font-size: 0.85rem;
    color: #64748b;
    margin-top: 0.25rem;
    font-weight: 500;
    transition: color 0.3s ease;
  }
  
  // Variants
  &.kpi-primary {
    .kpi-icon {
      background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
      color: #2563eb;
    }
    &::before {
      background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
    }
  }
  
  &.kpi-success {
    .kpi-icon {
      background: linear-gradient(135deg, #dcfce7 0%, #bbf7d0 100%);
      color: #16a34a;
    }
    &::before {
      background: linear-gradient(135deg, #16a34a 0%, #15803d 100%);
    }
  }
  
  &.kpi-warning {
    .kpi-icon {
      background: linear-gradient(135deg, #fef3c7 0%, #fde68a 100%);
      color: #f59e0b;
    }
    &::before {
      background: linear-gradient(135deg, #f59e0b 0%, #d97706 100%);
    }
  }
  
  &.kpi-danger {
    .kpi-icon {
      background: linear-gradient(135deg, #fee2e2 0%, #fecaca 100%);
      color: #ef4444;
    }
    &::before {
      background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%);
    }
  }
  
  &.kpi-info {
    .kpi-icon {
      background: linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%);
      color: #3b82f6;
    }
    &::before {
      background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%);
    }
  }
}
```

**Lưu ý:**
- Label luôn đứng **trước** value (thứ tự: label → value → unit)
- Sử dụng `.kpi-content` thay vì `.kpi-info`
- Icon backgrounds sử dụng **linear-gradient** với 2 điểm dừng
- Class variants: `kpi-primary`, `kpi-success`, `kpi-warning`, `kpi-danger`, `kpi-info` (không dùng tên cũ như `total`, `progress`, `completed`)
- `.kpi-unit` là optional, chỉ dùng khi cần hiển thị đơn vị (đ, giờ, lần, v.v.)

### 3.4. Action Button (Nút hành động)
Nút bấm có hiệu ứng "Slide-in" màu nền khi hover.

```scss
.action-btn {
  position: relative;
  overflow: hidden;
  background: $card-bg;
  color: $text-dark;
  border: 1px solid $border-color;
  z-index: 0;
  
  &::before {
    content: '';
    position: absolute;
    top: 0; left: 0; width: 0; height: 100%;
    background: $primary;
    z-index: -1;
    transition: width 0.3s ease;
  }

  &:hover:not(:disabled) {
    color: white;
    border-color: $primary;
    transform: translateY(-1px);
    box-shadow: 0 4px 12px rgba($primary, 0.2);
    
    &::before { 
      width: 100%; // Background trượt từ trái sang phải
    }
  }
}
```

### 3.5. Loading Overlay (Lớp phủ loading)
Hiệu ứng loading khi thực hiện các action (tìm kiếm, phân trang, reload, xem chi tiết).

```scss
.loading-overlay {
  position: absolute;
  inset: 0;
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;
  border-radius: $radius-2xl;

  .spinner-container {
    text-align: center;
    
    .spinner-border {
      width: 40px;
      height: 40px;
      border-width: 3px;
      color: $primary;
    }

    .loading-text {
      margin-top: 12px;
      color: $text-muted;
      font-size: 0.875rem;
      font-weight: 500;
    }
  }
}
```

**Sử dụng:**
```html
<div class="content-card">
  <div class="loading-overlay" *ngIf="loadingList">
    <div class="spinner-container">
      <div class="spinner-border"></div>
      <div class="loading-text">Đang tải dữ liệu...</div>
    </div>
  </div>
  <!-- Nội dung chính -->
</div>
```

## 4. Typography
- **Font Family**: `'Segoe UI', system-ui, sans-serif`
- **Page Title**: `1.5rem`, `700` weight, `$text-dark`
- **Section Title**: `1.1rem`, `700` weight, `$text-dark`
- **Label**: `0.8rem`, `$text-muted`
- **Value**: `0.95rem`, `600` weight, `$text-dark`

## 5. Layout Guidelines
- **Grid System**: Sử dụng Bootstrap Grid (`row`, `col-*`).
- **Spacing**:
  - Padding trang: `24px`
  - Gap giữa các thẻ: `24px` (hoặc `g-4` của Bootstrap)
  - Padding trong thẻ: `24px`

## 6. Áp dụng cho các Role khác
Khi phát triển các trang cho HR hoặc Giám đốc, hãy tuân thủ:
1. Copy file SCSS mẫu từ `dashboard.component.scss` hoặc `ho-so.component.scss`.
2. Sử dụng cấu trúc HTML:
   ```html
   <div class="page-container">
     <div class="header-card">...</div>
     <div class="row">
       <div class="col-md-X">
         <div class="content-card">...</div>
       </div>
     </div>
   </div>
   ```
3. Giữ nguyên các biến màu sắc để đảm bảo tính đồng bộ toàn hệ thống.
