# Hướng Dẫn Tooltip Nâng Cao với Chart.js

Tài liệu này mô tả cách tùy chỉnh tooltip trong Chart.js để hiển thị thông tin chi tiết hơn, bao gồm phần trăm và tổng.

## Tổng Quan

Tooltip nâng cao cho phép hiển thị nhiều thông tin hơn khi người dùng hover vào biểu đồ, bao gồm:
- Giá trị gốc
- Phần trăm so với tổng
- Tổng cộng (trong footer)
- Định dạng tùy chỉnh

## Cấu Hình Cơ Bản

### 1. Tooltip Styling

```typescript
tooltip: {
  backgroundColor: 'rgba(0, 0, 0, 0.8)',
  padding: 12,
  titleFont: { size: 14, weight: 'bold' },
  bodyFont: { size: 13 },
  footerFont: { size: 12, weight: 'bold' },
  displayColors: true,
  borderColor: '#ffffff',
  borderWidth: 1,
}
```

### 2. Custom Label với Phần Trăm

```typescript
callbacks: {
  label: (context) => {
    const value = context.parsed.y ?? 0;
    const total = calculateTotal(); // Tính tổng
    const percent = total > 0 
      ? ((value / total) * 100).toFixed(1) 
      : '0';
    return `${context.label}: ${value} (${percent}%)`;
  }
}
```

### 3. Footer với Tổng Cộng

```typescript
callbacks: {
  footer: (tooltipItems) => {
    const total = tooltipItems.reduce((sum, item) => sum + item.parsed.y, 0);
    return `Tổng: ${total}`;
  }
}
```

## Ví Dụ Cho Bar Chart

### Bar Chart với Tooltip Nâng Cao

```typescript
const totalAttendance = data.diLam + data.vang;

const config: ChartConfiguration<'bar'> = {
  type: 'bar',
  data: {
    labels: ['Đi làm', 'Vắng'],
    datasets: [{
      label: 'Ngày',
      data: [data.diLam, data.vang],
      backgroundColor: ['#2563eb', '#94a3b8'],
      borderRadius: 6,
    }],
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { 
      legend: { display: false },
      tooltip: {
        backgroundColor: 'rgba(0, 0, 0, 0.8)',
        padding: 12,
        titleFont: { size: 14, weight: 'bold' },
        bodyFont: { size: 13 },
        footerFont: { size: 12, weight: 'bold' },
        displayColors: true,
        callbacks: {
          label: (context) => {
            const value = context.parsed.y ?? 0;
            const percent = totalAttendance > 0 
              ? ((value / totalAttendance) * 100).toFixed(1) 
              : '0';
            return `${context.label}: ${value} ngày (${percent}%)`;
          },
          footer: () => {
            return `Tổng: ${totalAttendance} ngày`;
          }
        }
      }
    },
    scales: {
      y: { beginAtZero: true },
      x: { grid: { display: false } }
    }
  },
};
```

## Tùy Chỉnh Nâng Cao

### 1. Multi-line Tooltip

```typescript
callbacks: {
  label: (context) => {
    return [
      `Giá trị: ${context.parsed.y}`,
      `Phần trăm: ${calculatePercent(context)}%`,
      `Trạng thái: ${getStatus(context)}`
    ];
  }
}
```

### 2. Custom HTML Tooltip (External)

```typescript
tooltip: {
  enabled: false,
  external: (context) => {
    // Tạo custom HTML tooltip
    let tooltipEl = document.getElementById('chartjs-tooltip');
    
    if (!tooltipEl) {
      tooltipEl = document.createElement('div');
      tooltipEl.id = 'chartjs-tooltip';
      tooltipEl.innerHTML = '<table></table>';
      document.body.appendChild(tooltipEl);
    }
    
    // Cập nhật nội dung và vị trí
    const tooltipModel = context.tooltip;
    if (tooltipModel.opacity === 0) {
      tooltipEl.style.opacity = '0';
      return;
    }
    
    // ... custom logic
  }
}
```

### 3. Conditional Formatting

```typescript
callbacks: {
  label: (context) => {
    const value = context.parsed.y ?? 0;
    const label = context.label;
    
    // Định dạng khác nhau dựa trên giá trị
    if (value > 100) {
      return `⚠️ ${label}: ${value} (Cao)`;
    } else if (value > 50) {
      return `✓ ${label}: ${value} (Trung bình)`;
    } else {
      return `ℹ️ ${label}: ${value} (Thấp)`;
    }
  }
}
```

### 4. Tooltip với Đơn Vị Tùy Chỉnh

```typescript
callbacks: {
  label: (context) => {
    const value = context.parsed.y ?? 0;
    const unit = getUnit(context.datasetIndex);
    return `${context.label}: ${value} ${unit}`;
  },
  footer: (items) => {
    const total = items.reduce((sum, item) => sum + item.parsed.y, 0);
    return `Tổng cộng: ${total.toLocaleString('vi-VN')} VNĐ`;
  }
}
```

## Styling Options

### Background và Border

```typescript
tooltip: {
  backgroundColor: 'rgba(0, 0, 0, 0.9)',
  borderColor: '#2563eb',
  borderWidth: 2,
  cornerRadius: 8,
  padding: 16,
}
```

### Font Customization

```typescript
tooltip: {
  titleFont: { 
    size: 16, 
    weight: 'bold',
    family: 'Segoe UI'
  },
  bodyFont: { 
    size: 14,
    weight: 'normal'
  },
  footerFont: { 
    size: 12,
    weight: 'bold',
    style: 'italic'
  },
}
```

### Colors và Spacing

```typescript
tooltip: {
  titleColor: '#ffffff',
  bodyColor: '#e0e0e0',
  footerColor: '#fbbf24',
  titleSpacing: 8,
  bodySpacing: 6,
  footerSpacing: 8,
  padding: {
    top: 12,
    right: 16,
    bottom: 12,
    left: 16
  }
}
```

## Callbacks Phổ Biến

### 1. beforeTitle / afterTitle

```typescript
callbacks: {
  beforeTitle: (items) => 'Thông tin chi tiết',
  title: (items) => items[0].label,
  afterTitle: (items) => '─────────────'
}
```

### 2. beforeBody / afterBody

```typescript
callbacks: {
  beforeBody: (items) => ['Dữ liệu:'],
  afterBody: (items) => ['', 'Nhấn để xem chi tiết']
}
```

### 3. beforeFooter / afterFooter

```typescript
callbacks: {
  beforeFooter: (items) => '─────────────',
  footer: (items) => `Tổng: ${calculateTotal(items)}`,
  afterFooter: (items) => 'Cập nhật: ' + new Date().toLocaleDateString('vi-VN')
}
```

## Ví Dụ Thực Tế

### Tooltip cho Biểu Đồ Chấm Công

Xem implementation trong `dashboard.component.ts`:
- **Chấm công tháng** (dòng 144-168): Hiển thị số ngày và phần trăm
- **Trạng thái đơn từ** (dòng 192-216): Hiển thị số đơn và phần trăm

### Tooltip cho Doughnut Chart

Xem implementation trong `dashboard.component.ts` (dòng 256-268): Hiển thị giá trị và phần trăm cho từng phần của pie chart.

## Best Practices

1. **Null Safety**: Luôn sử dụng `??` operator để xử lý giá trị null
   ```typescript
   const value = context.parsed.y ?? 0;
   ```

2. **Performance**: Tránh tính toán phức tạp trong callbacks
   ```typescript
   // Tính total một lần trước khi tạo chart
   const total = calculateTotal();
   ```

3. **Formatting**: Sử dụng `toFixed()` cho số thập phân
   ```typescript
   const percent = ((value / total) * 100).toFixed(1);
   ```

4. **Localization**: Sử dụng `toLocaleString()` cho định dạng số
   ```typescript
   return value.toLocaleString('vi-VN') + ' VNĐ';
   ```

## Lưu Ý

- Tooltip callbacks nhận `context` object chứa thông tin về data point
- `context.parsed` chứa giá trị đã parse (x, y cho bar/line, value cho pie)
- `context.label` chứa nhãn của data point
- `context.dataset` chứa toàn bộ dataset
- Footer callbacks nhận array of tooltip items thay vì single context
