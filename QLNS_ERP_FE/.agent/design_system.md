# QLNS ERP - Design System (Clean UI)

## Overview

This document defines the design system used throughout the QLNS ERP application. It provides consistent guidelines for colors, layout, typography, components, spacing, and interaction patterns to ensure a modern, clean, and maintainable user interface.

---

# 1. Design Philosophy

The user interface follows these core principles:

- **Clean & Modern** – Use generous whitespace and soft rounded corners (12px).
- **Card-Based Layout** – Pages use a light gray background (`#f3f6f9`) with white content cards.
- **Interactive Experience** – Components include subtle hover animations and elevation effects.
- **Consistency** – Colors, typography, spacing, and component behaviors remain consistent throughout the application.

---

# 2. Color Palette

| Variable | Color | Usage |
|----------|--------|-------|
| `$primary` | `#2563eb` | Primary brand color, buttons, links |
| `$bg-page` | `#f3f6f9` | Page background |
| `$card-bg` | `#ffffff` | Card background |
| `$text-dark` | `#1e293b` | Primary text |
| `$text-muted` | `#64748b` | Secondary text and labels |
| `$border-color` | `#e2e8f0` | Borders and dividers |

---

# 3. UI Components

## 3.1 Header Card

The page header contains the page title, subtitle, and action buttons.

```scss
.header-card {
  background: $card-bg;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  padding: 24px 28px;
  margin-bottom: 24px;
}
```

---

## 3.2 Content Card

Content cards are used to display the main information on each page.

```scss
.content-card {
  background: $card-bg;
  border-radius: 12px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
  padding: 24px;
  transition: all 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  border: 1px solid transparent;

  &:hover {
    box-shadow: 0 8px 25px rgba(0, 0, 0, 0.1);
    transform: translateY(-2px);
    border-color: rgba($primary, 0.1);
  }
}
```

---

## 3.3 KPI Card

KPI Cards display summary statistics with animated gradient backgrounds.

### HTML Structure

```html
<div class="kpi-card kpi-primary">
  <div class="kpi-icon">
    <i class="bi bi-wallet2"></i>
  </div>

  <div class="kpi-content">
    <div class="kpi-label">Total Annual Salary</div>
    <div class="kpi-value">{{ value | number }}</div>
    <div class="kpi-unit">VND</div>
  </div>
</div>
```

### SCSS

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

    &::before {
      transform: translateX(0);
    }

    .kpi-icon {
      background: rgba(255, 255, 255, 0.2) !important;
      color: white !important;
    }

    .kpi-label,
    .kpi-value,
    .kpi-unit {
      color: white !important;
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

### Implementation Notes

- Display elements in the following order:
  - Label
  - Value
  - Unit (optional)
- Use `.kpi-content` instead of `.kpi-info`.
- Icon backgrounds should use two-color linear gradients.
- Supported variants:
  - `kpi-primary`
  - `kpi-success`
  - `kpi-warning`
  - `kpi-danger`
  - `kpi-info`
- The `.kpi-unit` element is optional and should only be displayed when a measurement unit is required.

---

## 3.4 Action Button

Action buttons include a left-to-right sliding background animation.

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
    top: 0;
    left: 0;
    width: 0;
    height: 100%;
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
      width: 100%;
    }
  }
}
```

---

## 3.5 Loading Overlay

A loading overlay is displayed during asynchronous operations such as searching, pagination, refreshing, or loading details.

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

### Example

```html
<div class="content-card">
  <div class="loading-overlay" *ngIf="loadingList">
    <div class="spinner-container">
      <div class="spinner-border"></div>
      <div class="loading-text">Loading data...</div>
    </div>
  </div>

  <!-- Main content -->
</div>
```

---

# 4. Typography

| Element | Font Size | Weight |
|----------|-----------|--------|
| Page Title | 1.5rem | 700 |
| Section Title | 1.1rem | 700 |
| Label | 0.8rem | Normal |
| Value | 0.95rem | 600 |

**Font Family**

```scss
'Segoe UI', system-ui, sans-serif
```

---

# 5. Layout Guidelines

## Grid System

Use the Bootstrap Grid system (`row`, `col-*`) for page layouts.

## Spacing

| Element | Value |
|----------|-------|
| Page padding | 24px |
| Card spacing | 24px |
| Card padding | 24px |

---

# 6. Guidelines for New Modules

When developing new pages for HR, Directors, or other modules:

1. Reuse the base SCSS from `dashboard.component.scss` or `ho-so.component.scss`.
2. Follow the standard page layout:

```html
<div class="page-container">
  <div class="header-card">
    ...
  </div>

  <div class="row">
    <div class="col-md-X">
      <div class="content-card">
        ...
      </div>
    </div>
  </div>
</div>
```

3. Always use the shared color variables defined in this design system to maintain a consistent appearance across the entire application.

---

# Design Principles Summary

- Keep the interface clean and spacious.
- Use white cards on a light gray background.
- Apply subtle animations and elevation on hover.
- Maintain consistent spacing and typography.
- Reuse shared SCSS components whenever possible.
- Follow the standardized color palette across all modules.
