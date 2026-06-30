# Chart Design Guidelines

## Overview

To maintain consistency and improve readability across the Human Resource Management System, **all statistical visualizations are implemented using Bar Charts**.

This design standard ensures a uniform user experience and simplifies data comparison throughout the application.

---

# Color Palette

The following color palette should be used consistently across all charts.

```scss
$chart-primary:  #2563eb;  // Primary metric (Blue)
$chart-success:  #16a34a;  // Success / Completed (Green)
$chart-warning:  #ea580c;  // Pending / Attention (Orange)
$chart-danger:   #ef4444;  // Error / Rejected (Red)
$chart-info:     #7c3aed;  // Secondary information (Purple)
$chart-gray:     #94a3b8;  // Neutral / Inactive (Gray)
```

---

# Standard Chart Configurations

## Single Bar Chart

Use this configuration when displaying a single metric across multiple categories.

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
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          color: '#f1f5f9'
        },
        ticks: {
          font: {
            size: 12
          }
        }
      },
      x: {
        grid: {
          display: false
        },
        ticks: {
          font: {
            size: 12,
            weight: 'bold'
          }
        }
      }
    }
  }
};
```

---

## Grouped Bar Chart

Use this configuration to compare multiple datasets within the same category.

```typescript
const config: ChartConfiguration<'bar'> = {
  type: 'bar',
  data: {
    labels: ['Category 1', 'Category 2', 'Category 3'],
    datasets: [
      {
        label: 'Pending',
        data: [...],
        backgroundColor: '#ea580c'
      },
      {
        label: 'Approved',
        data: [...],
        backgroundColor: '#16a34a'
      },
      {
        label: 'Rejected',
        data: [...],
        backgroundColor: '#ef4444'
      }
    ]
  },
  options: {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom',
        labels: {
          usePointStyle: true,
          padding: 16
        }
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        grid: {
          color: '#f1f5f9'
        }
      },
      x: {
        grid: {
          display: false
        }
      }
    }
  }
};
```

---

## Horizontal Bar Chart

Use a horizontal layout when category labels are long or when ranking information is displayed.

```typescript
const config: ChartConfiguration<'bar'> = {
  type: 'bar',
  data: {
    ...
  },
  options: {
    indexAxis: 'y',
    responsive: true,
    maintainAspectRatio: false,
    ...
  }
};
```

---

# Chart Container Size

The following container dimensions are recommended for all dashboard charts.

```scss
.chart-container {
  height: 280px;
  min-height: 240px;
  max-height: 360px;
  position: relative;
}
```

---

# Design Guidelines

To maintain a consistent appearance throughout the system, follow these recommendations:

1. **Use Bar Charts Only**
   - Avoid Pie Charts and Doughnut Charts.
   - Bar Charts provide better readability and comparison.

2. **Use the Standard Color Palette**
   - Always apply the predefined colors.
   - Avoid introducing additional colors unless absolutely necessary.

3. **Rounded Corners**
   - Set `borderRadius: 6` for all bars to achieve a modern and consistent appearance.

4. **Legend Placement**
   - Hide the legend for single-dataset charts.
   - Place the legend at the bottom for grouped charts.

5. **Grid Lines**
   - Display light gray (`#f1f5f9`) grid lines on the Y-axis.
   - Hide grid lines on the X-axis to reduce visual clutter.

6. **Responsive Layout**
   - Enable `responsive: true`.
   - Set `maintainAspectRatio: false` to allow flexible resizing within the container.

7. **Consistent Typography**
   - Use a font size of **12px** for axis labels.
   - Use **bold** text for X-axis category labels where appropriate.

---

# Summary

These chart design guidelines establish a consistent visualization standard for the Human Resource Management ERP system. Following these recommendations improves readability, enhances the user experience, and ensures that all dashboard components maintain a unified visual style.
