# Hướng Dẫn Tooltip Nâng Cao với Chart.js

## Tổng quan

Tài liệu này hướng dẫn cách tùy chỉnh Tooltip trong **Chart.js** để hiển thị nhiều thông tin hơn khi người dùng di chuột lên biểu đồ.

Tooltip có thể hiển thị:

- Giá trị gốc
- Tỷ lệ phần trăm
- Tổng cộng
- Đơn vị dữ liệu
- Nội dung nhiều dòng
- Định dạng theo điều kiện
- HTML Tooltip tùy chỉnh

---

# Cấu hình cơ bản

## Tooltip mặc định

```typescript
tooltip: {
  backgroundColor: 'rgba(0, 0, 0, 0.8)',
  padding: 12,
  titleFont: {
    size: 14,
    weight: 'bold'
  },
  bodyFont: {
    size: 13
  },
  footerFont: {
    size: 12,
    weight: 'bold'
  },
  displayColors: true,
  borderColor: '#ffffff',
  borderWidth: 1,
}
```

### Các thuộc tính chính

| Thuộc tính | Mô tả |
|------------|------|
| `backgroundColor` | Màu nền Tooltip |
| `padding` | Khoảng cách nội dung |
| `titleFont` | Font của tiêu đề |
| `bodyFont` | Font của nội dung |
| `footerFont` | Font của footer |
| `displayColors` | Hiển thị ô màu của Dataset |
| `borderColor` | Màu viền |
| `borderWidth` | Độ dày viền |

---

# Hiển thị phần trăm

Thông thường cần hiển thị thêm phần trăm của giá trị so với tổng.

```typescript
callbacks: {
  label: (context) => {
    const value = context.parsed.y ?? 0;
    const total = calculateTotal();

    const percent = total > 0
      ? ((value / total) * 100).toFixed(1)
      : '0';

    return `${context.label}: ${value} (${percent}%)`;
  }
}
```

Ví dụ:

```
Đi làm: 23 (92.0%)
```

---

# Hiển thị tổng cộng

Footer thường dùng để hiển thị tổng dữ liệu.

```typescript
callbacks: {
  footer: (tooltipItems) => {
    const total = tooltipItems.reduce(
      (sum, item) => sum + item.parsed.y,
      0
    );

    return `Tổng: ${total}`;
  }
}
```

Ví dụ:

```
Tổng: 25
```

---

# Ví dụ hoàn chỉnh cho Bar Chart

```typescript
const totalAttendance = data.diLam + data.vang;

const config: ChartConfiguration<'bar'> = {
  type: 'bar',

  data: {
    labels: ['Đi làm', 'Vắng'],
    datasets: [{
      label: 'Ngày',
      data: [
        data.diLam,
        data.vang
      ],
      backgroundColor: [
        '#2563eb',
        '#94a3b8'
      ],
      borderRadius: 6
    }]
  },

  options: {
    responsive: true,
    maintainAspectRatio: false,

    plugins: {
      legend: {
        display: false
      },

      tooltip: {
        backgroundColor: 'rgba(0,0,0,.8)',

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
    }
  }
};
```

---

# Tooltip nhiều dòng

Tooltip có thể trả về một mảng chuỗi để hiển thị nhiều dòng.

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

Kết quả:

```
Giá trị: 85

Phần trăm: 68%

Trạng thái: Đạt
```

---

# Tooltip HTML tùy chỉnh

Chart.js cho phép thay Tooltip mặc định bằng HTML.

```typescript
tooltip: {

  enabled: false,

  external: (context) => {

    let tooltipEl = document.getElementById('chartjs-tooltip');

    if (!tooltipEl) {

      tooltipEl = document.createElement('div');

      tooltipEl.id = 'chartjs-tooltip';

      tooltipEl.innerHTML = '<table></table>';

      document.body.appendChild(tooltipEl);
    }

    const tooltipModel = context.tooltip;

    if (tooltipModel.opacity === 0) {

      tooltipEl.style.opacity = '0';

      return;
    }

    // Custom render...

  }

}
```

HTML Tooltip phù hợp khi cần:

- Card đẹp
- Icon
- Hình ảnh
- Button
- Layout tùy chỉnh

---

# Tooltip theo điều kiện

Có thể hiển thị nội dung khác nhau tùy giá trị.

```typescript
callbacks: {

  label: (context) => {

    const value = context.parsed.y ?? 0;

    if (value > 100) {

      return `⚠️ ${context.label}: ${value} (Cao)`;

    }

    if (value > 50) {

      return `✓ ${context.label}: ${value} (Trung bình)`;

    }

    return `ℹ️ ${context.label}: ${value} (Thấp)`;

  }

}
```

---

# Tooltip với đơn vị

```typescript
callbacks: {

  label: (context) => {

    const value = context.parsed.y ?? 0;

    const unit = getUnit(context.datasetIndex);

    return `${context.label}: ${value} ${unit}`;

  },

  footer: (items) => {

    const total = items.reduce(
      (sum, item) => sum + item.parsed.y,
      0
    );

    return `Tổng cộng: ${total.toLocaleString('vi-VN')} VNĐ`;

  }

}
```

---

# Tùy chỉnh giao diện

## Background và Border

```typescript
tooltip: {

  backgroundColor: 'rgba(0,0,0,.9)',

  borderColor: '#2563eb',

  borderWidth: 2,

  cornerRadius: 8,

  padding: 16

}
```

---

## Font

```typescript
tooltip: {

  titleFont: {

    size: 16,

    weight: 'bold',

    family: 'Segoe UI'

  },

  bodyFont: {

    size: 14

  },

  footerFont: {

    size: 12,

    weight: 'bold',

    style: 'italic'

  }

}
```

---

## Màu chữ

```typescript
tooltip: {

  titleColor: '#ffffff',

  bodyColor: '#e0e0e0',

  footerColor: '#fbbf24'

}
```

---

## Padding

```typescript
tooltip: {

  padding: {

    top: 12,

    right: 16,

    bottom: 12,

    left: 16

  }

}
```

---

# Callback phổ biến

## beforeTitle / afterTitle

```typescript
callbacks: {

  beforeTitle: () => 'Thông tin chi tiết',

  title: (items) => items[0].label,

  afterTitle: () => '────────────'

}
```

---

## beforeBody / afterBody

```typescript
callbacks: {

  beforeBody: () => ['Dữ liệu'],

  afterBody: () => [

    '',

    'Nhấn để xem chi tiết'

  ]

}
```

---

## beforeFooter / footer / afterFooter

```typescript
callbacks: {

  beforeFooter: () => '────────────',

  footer: (items) => `Tổng: ${calculateTotal(items)}`,

  afterFooter: () => {

    return 'Cập nhật: ' +
      new Date().toLocaleDateString('vi-VN');

  }

}
```

---

# Ví dụ thực tế

## Tooltip cho Bar Chart

Hiển thị:

- Số ngày
- Phần trăm
- Tổng ngày

Ví dụ:

```
Đi làm: 23 ngày (92%)

--------------------

Tổng: 25 ngày
```

---

## Tooltip cho Pie Chart

Hiển thị:

```
Dự án: 15 (37.5%)
```

---

## Tooltip cho Doughnut Chart

Hiển thị:

```
OT: 8 giờ (20%)

Đơn phép: 12 (30%)

Dự án: 20 (50%)
```

---

# Best Practices

## Sử dụng Null Safety

```typescript
const value = context.parsed.y ?? 0;
```

---

## Không tính toán trong Callback

Nên tính trước:

```typescript
const total = calculateTotal();
```

Thay vì:

```typescript
callbacks: {
  label() {
    calculateTotal();
  }
}
```

---

## Làm tròn số

```typescript
const percent =
  ((value / total) * 100).toFixed(1);
```

---

## Định dạng số

```typescript
value.toLocaleString('vi-VN');
```

Ví dụ:

```
1.250.000
```

---

# Các thuộc tính thường dùng của Context

| Thuộc tính | Ý nghĩa |
|------------|----------|
| `context.label` | Nhãn của Data Point |
| `context.parsed` | Giá trị đã parse |
| `context.dataset` | Dataset hiện tại |
| `context.datasetIndex` | Vị trí Dataset |
| `context.dataIndex` | Vị trí Data Point |
| `context.chart` | Đối tượng Chart hiện tại |

---

# Lưu ý

- Tooltip Callback luôn nhận đối tượng `context`.
- `context.parsed` khác nhau tùy loại biểu đồ:
  - Bar Chart: `context.parsed.y`
  - Line Chart: `context.parsed.y`
  - Pie Chart: `context.parsed`
  - Doughnut Chart: `context.parsed`
- Nên tính toán tổng dữ liệu trước khi khởi tạo Chart để tăng hiệu năng.
- Luôn sử dụng `??` để tránh lỗi khi dữ liệu null.
- Sử dụng `toLocaleString('vi-VN')` khi hiển thị tiền hoặc số lớn.
- Destroy Chart cũ trước khi tạo mới để tránh Memory Leak.
```
