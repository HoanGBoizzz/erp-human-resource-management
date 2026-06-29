# Chart Design Guidelines

## Overview
Tất cả biểu đồ trong hệ thống sử dụng **Bar Chart** để đảm bảo tính nhất quán và dễ đọc.

---

## Color Palette
```scss
$chart-primary:  #2563eb;  // Blue - main metric
$chart-success:  #16a34a;  // Green - positive/completed
$chart-warning:  #ea580c;  // Orange - pending/attention
$chart-danger:   #ef4444;  // Red - rejected/error
$chart-info:     #7c3aed;  // Purple - secondary info
$chart-gray:     #94a3b8;  // Gray - inactive/neutral
```

---

## Standard Configurations

### Single Bar Chart (Comparison)
```typescript
const config: ChartConfiguration<'bar'> = {
  type: 'bar',
  data: {
    labels: ['Label 1', 'Label 2', 'Label 3'],
    datasets: [{
      label: 'Metric',
      data: [value1, value2, value3],
      backgroundColor: ['#2563eb', '#16a34a', '#ea580c'],
      borderRadius: 6,
      borderSkipped: false,
    }]
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: { color: '#f1f5f9' },
        ticks: { font: { size: 12 } }
      },
      x: {
        grid: { display: false },
        ticks: { font: { size: 12, weight: 'bold' } }
      }
    }
  }
};
```

### Grouped Bar Chart (Multi-Dataset)
```typescript
const config: ChartConfiguration<'bar'> = {
  type: 'bar',
  data: {
    labels: ['Category 1', 'Category 2', 'Category 3'],
    datasets: [
      { label: 'Pending', data: [...], backgroundColor: '#ea580c' },
      { label: 'Approved', data: [...], backgroundColor: '#16a34a' },
      { label: 'Rejected', data: [...], backgroundColor: '#ef4444' }
    ]
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { 
        position: 'bottom',
        labels: { usePointStyle: true, padding: 16 }
      }
    },
    scales: {
      y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
      x: { grid: { display: false } }
    }
  }
};
```

### Horizontal Bar Chart
```typescript
const config: ChartConfiguration<'bar'> = {
  type: 'bar',
  data: { ... },
  options: {
    indexAxis: 'y',  // Makes it horizontal
    responsive: true,
    maintainAspectRatio: false,
    ...
  }
};
```

---

## Chart Container Sizing
```scss
.chart-container {
  height: 280px;
  min-height: 240px;
  max-height: 360px;
  position: relative;
}
```

---

## Usage Rules
1. **No Pie/Doughnut Charts** - Always use bar charts
2. **Consistent Colors** - Use palette above
3. **Border Radius** - Use `borderRadius: 6` for modern look
4. **Legend Position** - Bottom for grouped charts, hidden for single
5. **Grid Lines** - Light gray (#f1f5f9) for Y-axis, none for X-axis
