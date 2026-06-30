# Modal Design Guide - QLNS ERP

## Overview

Tài liệu này quy định tiêu chuẩn thiết kế và triển khai Modal trong hệ thống **QLNS ERP** nhằm đảm bảo giao diện đồng nhất, dễ bảo trì và mang lại trải nghiệm người dùng tốt.

Các mục tiêu chính:

- Thiết kế đồng nhất với Design System.
- Hỗ trợ Responsive.
- Dễ tái sử dụng.
- Hỗ trợ Accessibility.
- Có Loading State.
- Hỗ trợ Keyboard (ESC).

---

# Modal Architecture

Mỗi Modal nên gồm 2 phần độc lập:

1. **Backdrop**
2. **Modal Dialog**

Thứ tự đúng:

```text
modal-backdrop
modal
 └── modal-dialog
      └── modal-content
           ├── modal-header
           ├── modal-body
           └── modal-footer
```

**Lưu ý**

- Backdrop phải nằm ngoài Modal.
- Không đặt Backdrop bên trong `.modal`.

---

# Z-Index Hierarchy

| Thành phần | Z-Index |
|------------|---------|
| Toast Notification | 9999 |
| Modal Dialog | 1055 |
| Modal Backdrop | 1050 |
| Loading Overlay | 10 (relative trong Modal) |

---

# HTML Structure

## Backdrop

```html
<div class="modal-backdrop fade"
     [class.show]="showModal"
     *ngIf="showModal"
     (click)="closeModal()">
</div>
```

---

## Modal

```html
<div class="modal fade"
     [class.show]="showModal"
     [style.display]="showModal ? 'block' : 'none'"
     tabindex="-1"
     *ngIf="showModal">

    <div class="modal-dialog modal-lg modal-dialog-scrollable">

        <div class="modal-content">

            <div class="modal-header">
                ...
            </div>

            <div class="modal-body">
                ...
            </div>

            <div class="modal-footer">
                ...
            </div>

        </div>

    </div>

</div>
```

---

# Modal Sizes

## Small

```html
<div class="modal-dialog modal-sm">
```

## Default

```html
<div class="modal-dialog">
```

## Large

```html
<div class="modal-dialog modal-lg">
```

## Extra Large

```html
<div class="modal-dialog modal-xl">
```

## Full Screen

```html
<div class="modal-dialog modal-fullscreen">
```

---

# Scrollable Modal

Nếu nội dung dài:

```html
<div class="modal-dialog modal-dialog-scrollable">
```

Body sẽ tự scroll, Header và Footer cố định.

---

# Centered Modal

```html
<div class="modal-dialog modal-dialog-centered">
```

Modal sẽ căn giữa theo chiều dọc.

---

# SCSS Guidelines

## Variables

```scss
$primary: #2563eb;
$bg-page: #f3f6f9;
$card-bg: #ffffff;

$text-dark: #1e293b;
$text-muted: #64748b;

$border-color: #e2e8f0;

$radius-lg: 12px;
$radius-md: 8px;
$radius-sm: 6px;
```

---

## Backdrop

```scss
.modal-backdrop {
    position: fixed;
    inset: 0;

    z-index: 1050;

    background: rgba(0,0,0,.5);

    &.fade {
        opacity: 0;
        transition: opacity .15s linear;
    }

    &.show {
        opacity: 1;
    }
}
```

---

## Modal

```scss
.modal {
    position: fixed;
    inset: 0;

    width: 100%;
    height: 100%;

    overflow-y: auto;
    overflow-x: hidden;

    z-index: 1055;

    outline: 0;

    &.fade {
        transition: opacity .15s linear;
    }

    &.fade:not(.show) {
        opacity: 0;
    }

    &.show {
        opacity: 1;
    }
}
```

---

## Dialog

```scss
.modal-dialog {
    position: relative;

    margin: 1.75rem auto;

    max-width: 800px;

    pointer-events: none;

    &.modal-sm {
        max-width: 400px;
    }

    &.modal-lg {
        max-width: 900px;
    }

    &.modal-xl {
        max-width: 1140px;
    }

    &.modal-dialog-centered {
        display: flex;
        align-items: center;
        min-height: calc(100% - 3.5rem);
    }

    &.modal-dialog-scrollable {

        max-height: calc(100% - 3.5rem);

        .modal-content {
            max-height: calc(100vh - 3.5rem);
            overflow: hidden;
        }

        .modal-body {
            overflow-y: auto;
        }
    }
}
```

---

## Modal Content

```scss
.modal-content {

    display: flex;
    flex-direction: column;

    width: 100%;

    pointer-events: auto;

    background: $card-bg;

    border: 1px solid $border-color;

    border-radius: $radius-lg;

    box-shadow: 0 10px 40px rgba(0,0,0,.15);
}
```

---

## Header

```scss
.modal-header {

    display: flex;

    justify-content: space-between;

    align-items: center;

    padding: 20px 24px;

    border-bottom: 1px solid $border-color;

    .modal-title {

        margin: 0;

        font-size: 1.125rem;

        font-weight: 600;

        color: $text-dark;
    }

    .btn-close {

        border: 0;

        background: transparent;

        cursor: pointer;

        opacity: .5;

        transition: .2s;

        &:hover {
            opacity: 1;
        }
    }
}
```

---

## Body

```scss
.modal-body {

    position: relative;

    flex: 1 1 auto;

    padding: 24px;
}
```

---

## Footer

```scss
.modal-footer {

    display: flex;

    justify-content: flex-end;

    gap: 8px;

    padding: 16px 24px;

    border-top: 1px solid $border-color;
}
```

---

## Loading Overlay

```scss
.loading-overlay {

    position: absolute;

    inset: 0;

    display: flex;

    justify-content: center;

    align-items: center;

    background: rgba(255,255,255,.85);

    backdrop-filter: blur(2px);

    z-index: 10;

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

            font-size: .875rem;

            font-weight: 500;
        }
    }
}
```

---

# TypeScript Pattern

## Component State

```typescript
showModal = false;

loading = false;

selectedId = 0;

selectedItem: any = null;
```

---

## Open Modal

```typescript
openModal(id: number): void {

    this.selectedId = id;

    this.showModal = true;

    this.loading = true;

    this.selectedItem = null;

    this.apiService.getDetail(id).subscribe({

        next: data => {

            this.selectedItem = data;

            this.loading = false;
        },

        error: err => {

            console.error(err);

            alert("Có lỗi khi tải dữ liệu");

            this.closeModal();
        }

    });

}
```

---

## Close Modal

```typescript
closeModal(): void {

    this.showModal = false;

    this.loading = false;

    this.selectedId = 0;

    this.selectedItem = null;
}
```

---

## Submit

```typescript
submitAction(): void {

    if (!this.canProcess()) {

        alert("Không thể xử lý");

        return;
    }

    this.loading = true;

    this.apiService.submitAction(...).subscribe({

        next: () => {

            this.closeModal();

            this.loadData();
        },

        error: err => {

            console.error(err);

            this.loading = false;
        }

    });

}
```

---

## ESC Support

```typescript
@HostListener("document:keydown.escape")
onEscapeKey(): void {

    if (this.showModal) {

        this.closeModal();

    }

}
```

---

# Accessibility

Nên bổ sung:

```html
<div
    class="modal"
    role="dialog"
    aria-modal="true"
    aria-labelledby="modalTitle">

    <div class="modal-header">

        <h5 id="modalTitle">
            Tiêu đề
        </h5>

    </div>

</div>
```

---

# Responsive

```scss
@media (max-width:576px){

    .modal-dialog{

        margin:.5rem;

        max-width:calc(100% - 1rem);

    }

    .modal-header,
    .modal-body,
    .modal-footer{

        padding:16px;

    }

}
```

---

# Best Practices

- Backdrop luôn đặt ngoài Modal.
- Sử dụng `*ngIf` thay vì `[hidden]`.
- Thêm animation `fade`.
- Có Loading Overlay khi tải dữ liệu.
- Reset toàn bộ state khi đóng Modal.
- Disable các nút khi đang xử lý.
- Hỗ trợ phím ESC.
- Sử dụng `modal-dialog-scrollable` nếu nội dung dài.
- Đảm bảo Responsive trên Mobile.
- Bổ sung ARIA attributes để hỗ trợ Accessibility.

---

# Troubleshooting

## Modal nằm dưới Backdrop

Kiểm tra:

- Backdrop phải nằm ngoài Modal.
- Backdrop có `z-index: 1050`.
- Modal có `z-index: 1055`.

---

## Không scroll được Body

Thêm:

```html
<div class="modal-dialog modal-dialog-scrollable">
```

---

## Click Backdrop không đóng

Kiểm tra:

```html
(click)="closeModal()"
```

---

## Animation không hoạt động

Kiểm tra:

```scss
.modal.fade {
    transition: opacity .15s linear;
}

.modal-backdrop.fade {
    transition: opacity .15s linear;
}
```

---

# Checklist

- [ ] Backdrop nằm ngoài Modal.
- [ ] Z-index đúng.
- [ ] Có animation Fade.
- [ ] Dùng `*ngIf`.
- [ ] Có Loading Overlay.
- [ ] Có nút Close.
- [ ] Reset State khi đóng.
- [ ] Disable Button khi Loading.
- [ ] Validate Form.
- [ ] Responsive.
- [ ] Accessibility.
- [ ] Keyboard Support (ESC).

---

# Summary

## HTML Structure

```text
modal-backdrop
modal
 └── modal-dialog
      └── modal-content
           ├── modal-header
           ├── modal-body
           └── modal-footer
```

## TypeScript Flow

```text
Open Modal
      │
      ▼
Fetch Data
      │
      ▼
Loading
      │
      ▼
Display Data
      │
      ▼
Submit
      │
      ▼
Reload List
      │
      ▼
Close Modal
```

---

**Version:** 1.0

**Project:** QLNS ERP

**Last Updated:** January 2026
