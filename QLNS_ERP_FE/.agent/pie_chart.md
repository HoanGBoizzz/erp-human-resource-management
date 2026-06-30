# Pie Chart Guide - Chart.js

## Overview

Tài liệu này hướng dẫn cách triển khai **Pie Chart** bằng **Chart.js** trong Angular.

Pie Chart dùng để hiển thị tỷ lệ của từng thành phần trong một tổng thể. Mỗi phần của biểu đồ tương ứng với một tỷ lệ phần trăm của dữ liệu.

Nếu muốn sử dụng **Doughnut Chart**, chỉ cần thêm thuộc tính `cutout` vào phần `options`.

---

# Import Chart.js

```typescript
import { Chart, ChartConfiguration } from 'chart.js/auto';
```

---

# Canvas Reference

```typescript
@ViewChild('pieChart', { static: false })
pieChart!: ElementRef<HTMLCanvasElement>;

private chart?: Chart;
```

---

# Basic Pie Chart Configuration

```typescript
const config: ChartConfiguration<'pie'> = {

    type: 'pie',

    data: {

        labels: [

            'Nhãn 1',

            'Nhãn 2',

            'Nhãn 3'

        ],

        datasets: [

            {

                data: [30, 50, 20],

                backgroundColor: [

                    '#2563eb',

                    '#7c3aed',

                    '#ea580c'

                ],

                borderWidth: 3,

                borderColor: '#ffffff',

                hoverBorderWidth: 4,

                hoverOffset: 8

            }

        ]

    },

    options: {

        responsive: true,

        maintainAspectRatio: false,

        plugins: {

            legend: {

                display: true,

                position: 'bottom',

                labels: {

                    padding: 15,

                    font: {

                        size: 13,

                        weight: 600

                    },

                    color: '#1e293b',

                    usePointStyle: true,

                    pointStyle: 'circle'

                }

            },

            tooltip: {

                backgroundColor: 'rgba(0,0,0,.8)',

                padding: 12,

                titleFont: {

                    size: 14,

                    weight: 'bold'

                },

                bodyFont: {

                    size: 13

                },

                displayColors: true,

                callbacks: {

                    label: (context) => {

                        const value = context.parsed;

                        const total = context.dataset.data.reduce((a, b) => a + b, 0);

                        const percent = total > 0
                            ? ((value / total) * 100).toFixed(1)
                            : '0';

                        return `${context.label}: ${value} (${percent}%)`;

                    }

                }

            }

        }

    }

};

this.chart = new Chart(this.pieChart.nativeElement, config);
```

---

# Pie Chart vs Doughnut Chart

## Pie Chart

```typescript
const config: ChartConfiguration<'pie'> = {

    type: 'pie'

};
```

Không cần khai báo `cutout`.

---

## Doughnut Chart

```typescript
const config: ChartConfiguration<'doughnut'> = {

    type: 'doughnut',

    options: {

        cutout: '60%'

    }

};
```

---

# Cutout Values

| Giá trị | Kết quả |
|---------|----------|
| `0%` hoặc không khai báo | Pie Chart |
| `50%` | Doughnut vừa |
| `70%` | Doughnut mỏng |

---

# Hover Effects

```typescript
hoverBorderWidth: 4,

hoverOffset: 8
```

| Thuộc tính | Ý nghĩa |
|------------|----------|
| `hoverBorderWidth` | Độ dày viền khi rê chuột |
| `hoverOffset` | Khoảng cách phần được nâng lên khi hover |

---

# Custom Tooltip

```typescript
callbacks: {

    label: (context) => {

        const value = context.parsed;

        const dataset = context.dataset.data as number[];

        const total = dataset.reduce((a, b) => a + b, 0);

        const percent = total > 0
            ? ((value / total) * 100).toFixed(1)
            : '0';

        return `${context.label}: ${value} (${percent}%)`;

    }

}
```

Tooltip sẽ hiển thị:

```text
Tên mục: Giá trị (Tỷ lệ%)
```

Ví dụ:

```text
OT: 45 (35.2%)
```

---

# Legend Configuration

```typescript
legend: {

    display: true,

    position: 'bottom',

    labels: {

        padding: 15,

        font: {

            size: 13,

            weight: 600

        },

        color: '#1e293b',

        usePointStyle: true,

        pointStyle: 'circle'

    }

}
```

## Legend Position

- `top`
- `bottom`
- `left`
- `right`

---

# Color Palette

```typescript
const colors = {

    primary: '#2563eb',

    purple: '#7c3aed',

    orange: '#ea580c',

    green: '#16a34a',

    danger: '#ef4444',

    gray: '#94a3b8'

};
```

| Color | Hex |
|--------|-----|
| Primary | `#2563eb` |
| Purple | `#7c3aed` |
| Orange | `#ea580c` |
| Green | `#16a34a` |
| Danger | `#ef4444` |
| Gray | `#94a3b8` |

---

# Gradient Colors

Chart.js hỗ trợ sử dụng Gradient thay cho màu đơn.

Ví dụ:

```typescript
const gradient = ctx.createLinearGradient(0, 0, 0, 400);

gradient.addColorStop(0, '#2563eb');

gradient.addColorStop(1, '#7c3aed');

backgroundColor: [

    gradient,

    ...

];
```

---

# Responsive Layout

## HTML

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

---

## SCSS

```scss
.chart-body {

    padding: 20px 24px 24px;

    min-height: 280px;

    display: flex;

    justify-content: center;

    align-items: center;

}
```

---

# Practical Example

Có thể tham khảo implementation trong:

```text
dashboard.component.ts
```

Đặc điểm của implementation:

- Sử dụng `type: 'pie'`.
- Không sử dụng `cutout`.
- Legend hiển thị phía dưới.
- Tooltip hiển thị giá trị và phần trăm.
- Hover Effect bằng `hoverBorderWidth` và `hoverOffset`.
- Responsive với `maintainAspectRatio: false`.

---

# Best Practices

- Luôn gọi `destroy()` trước khi tạo Chart mới để tránh Memory Leak.

```typescript
this.chart?.destroy();
```

- Luôn đặt:

```typescript
maintainAspectRatio: false
```

để biểu đồ tự co giãn theo container.

- Dùng toán tử `??` hoặc kiểm tra `null` trong Tooltip Callback để tránh lỗi.

- Nên sử dụng `font.weight` dưới dạng Number.

Đúng:

```typescript
weight: 600
```

Không nên:

```typescript
weight: '600'
```

- Sử dụng cùng một Color Palette trong toàn bộ hệ thống để đảm bảo tính nhất quán.

---

# Checklist

- [ ] Import Chart.js.
- [ ] Tạo `@ViewChild` cho Canvas.
- [ ] Destroy Chart cũ trước khi tạo mới.
- [ ] Sử dụng `type: 'pie'`.
- [ ] Không khai báo `cutout` nếu muốn Pie Chart.
- [ ] Bật Responsive.
- [ ] Đặt `maintainAspectRatio: false`.
- [ ] Hiển thị Legend ở dưới.
- [ ] Tooltip hiển thị cả giá trị và phần trăm.
- [ ] Sử dụng Color Palette thống nhất.

---

# Summary

## Pie Chart

- Biểu đồ hình tròn đầy đủ.
- Không sử dụng `cutout`.

## Doughnut Chart

- Biểu đồ có lỗ giữa.
- Sử dụng thuộc tính:

```typescript
cutout: '60%'
```

## Recommended Settings

```typescript
responsive: true

maintainAspectRatio: false

legend.position: 'bottom'

hoverBorderWidth: 4

hoverOffset: 8
```

---

**Version:** 1.0

**Project:** QLNS ERP

**Library:** Chart.js

**Last Updated:** January 2026
