# Hướng Dẫn Sử Dụng Pie Chart với Chart.js

Tài liệu này mô tả cách triển khai pie chart hình tròn đầy đủ trong Angular sử dụng Chart.js.

## Tổng Quan

Pie chart là loại biểu đồ tròn hiển thị dữ liệu dưới dạng các phần của một tổng thể. Mỗi phần đại diện cho tỷ lệ phần trăm của giá trị so với tổng.

**Lưu ý:** Nếu muốn tạo doughnut chart (có lỗ giữa), chỉ cần thêm thuộc tính `cutout: '60%'` vào options.

## Cấu Hình Cơ Bản

### 1. Import Chart.js

```typescript
import { Chart, ChartConfiguration } from 'chart.js/auto';
```

### 2. Tạo Canvas Reference

```typescript
@ViewChild('pieChart', { static: false }) pieChart!: ElementRef<HTMLCanvasElement>;
private chart?: Chart;
```

### 3. Cấu Hình Pie Chart Hình Tròn Đầy Đủ

```typescript
const config: ChartConfiguration<'pie'> = {
  type: 'pie',
  data: {
    labels: ['Nhãn 1', 'Nhãn 2', 'Nhãn 3'],
    datasets: [{
      data: [30, 50, 20],
      backgroundColor: ['#2563eb', '#7c3aed', '#ea580c'],
      borderWidth: 3,
      borderColor: '#ffffff',
      hoverBorderWidth: 4,
      hoverOffset: 8,
    }],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    // Không có cutout = pie chart đầy đủ
    plugins: {
      legend: {
        display: true,
        position: 'bottom',
        labels: {
          padding: 15,
          font: { size: 13, weight: 600 },
          color: '#1e293b',
          usePointStyle: true,
          pointStyle: 'circle',
        }
      },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: { size: 14, weight: 'bold' },
        bodyFont: { size: 13 },
        displayColors: true,
        callbacks: {
          label: (context) => {
            const value = context.parsed;
            const total = context.dataset.data.reduce((a, b) => a + b, 0);
            const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
            return `${context.label}: ${value} (${percent}%)`;
          }
        }
      }
    },
  },
};

this.chart = new Chart(this.pieChart.nativeElement, config);
```

## Tùy Chỉnh Nâng Cao

### Pie Chart vs Doughnut Chart

**Pie Chart (Hình tròn đầy đủ):**
```typescript
const config: ChartConfiguration<'pie'> = {
  type: 'pie',
  // ... không cần thuộc tính cutout
}
```

**Doughnut Chart (Có lỗ giữa):**
```typescript
const config: ChartConfiguration<'doughnut'> = {
  type: 'doughnut',
  options: {
    cutout: '60%', // Kích thước lỗ giữa
  }
}
```

Các giá trị cutout phổ biến:
- `cutout: '0%'` hoặc không có cutout - Pie chart đầy đủ
- `cutout: '50%'` - Doughnut chart vừa
- `cutout: '70%'` - Doughnut chart mỏng

### Hover Effects

```typescript
hoverBorderWidth: 4,    // Độ dày viền khi hover
hoverOffset: 8,         // Khoảng cách phần tử di chuyển khi hover
```

### Custom Tooltip với Phần Trăm

```typescript
callbacks: {
  label: (context) => {
    const value = context.parsed;
    const dataset = context.dataset.data as number[];
    const total = dataset.reduce((a, b) => a + b, 0);
    const percent = total > 0 ? ((value / total) * 100).toFixed(1) : '0';
    return `${context.label}: ${value} (${percent}%)`;
  }
}
```

### Legend Styling

```typescript
legend: {
  display: true,
  position: 'bottom',  // 'top' | 'bottom' | 'left' | 'right'
  labels: {
    padding: 15,
    font: { size: 13, weight: 600 },
    color: '#1e293b',
    usePointStyle: true,      // Sử dụng hình tròn thay vì hình vuông
    pointStyle: 'circle',     // 'circle' | 'rect' | 'triangle' | ...
  }
}
```

## Màu Sắc

### Bảng Màu Mặc Định

```typescript
const colors = {
  primary: '#2563eb',   // Xanh dương
  purple: '#7c3aed',    // Tím
  orange: '#ea580c',    // Cam
  green: '#16a34a',     // Xanh lá
  danger: '#ef4444',    // Đỏ
  gray: '#94a3b8',      // Xám
};
```

### Gradient Colors (Nâng Cao)

```typescript
const gradient = ctx.createLinearGradient(0, 0, 0, 400);
gradient.addColorStop(0, '#2563eb');
gradient.addColorStop(1, '#7c3aed');
backgroundColor: [gradient, ...]
```

## Responsive Design

### HTML Template

```html
<div class="chart-card">
  <div class="chart-header">
    <div class="chart-title">
      <i class="bi bi-pie-chart"></i>
      Tiêu đề biểu đồ
    </div>
  </div>
  <div class="chart-body">
    <canvas #pieChart></canvas>
  </div>
</div>
```

### SCSS Styling

```scss
.chart-body {
  padding: 20px 24px 24px;
  min-height: 280px;
  display: flex;
  align-items: center;
  justify-content: center;
}
```

## Ví Dụ Thực Tế

Xem implementation trong `dashboard.component.ts` (dòng 220-274) để tham khảo ví dụ hoàn chỉnh về **pie chart hình tròn đầy đủ** hiển thị tổng quan tháng với OT, Dự án, và Đơn phép.

**Đặc điểm của implementation:**
- Sử dụng `type: 'pie'` cho pie chart đầy đủ
- Không có thuộc tính `cutout` (khác với doughnut)
- Legend hiển thị ở dưới với point style tròn
- Tooltip tùy chỉnh hiển thị giá trị và phần trăm
- Hover effects với border width và offset

## Lưu Ý

- **Destroy Chart**: Luôn destroy chart cũ trước khi tạo mới để tránh memory leak
- **Null Safety**: Sử dụng `??` operator để xử lý giá trị null trong tooltip callbacks
- **Font Weight**: Sử dụng number (600) thay vì string ('600') cho font weight
- **Responsive**: Đặt `maintainAspectRatio: false` để chart tự động điều chỉnh kích thước
