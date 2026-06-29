# Modal Design Guide - QLNS ERP

## 📋 Mục lục
1. [Tổng quan](#tổng-quan)
2. [Cấu trúc HTML](#cấu-trúc-html)
3. [Styling SCSS](#styling-scss)
4. [TypeScript Logic](#typescript-logic)
5. [Best Practices](#best-practices)
6. [Examples](#examples)

---

## 🎯 Tổng quan

Modal popup là component UI hiển thị nội dung overlay trên trang hiện tại. Modal tuân thủ Design System với các đặc điểm:

- **Z-index layers**: Backdrop (1050), Modal (1055)
- **Animation**: Fade in/out với transition 0.15s
- **Responsive**: Tự động điều chỉnh theo màn hình
- **Accessibility**: Hỗ trợ keyboard (ESC để đóng), focus trap

---

## 📦 Cấu trúc HTML

### 1. Cấu trúc cơ bản

```html
<!-- MODAL BACKDROP -->
<div class="modal-backdrop fade" 
     [class.show]="showModal" 
     *ngIf="showModal" 
     (click)="closeModal()">
</div>

<!-- MODAL DIALOG -->
<div class="modal fade" 
     [class.show]="showModal" 
     [style.display]="showModal ? 'block' : 'none'"
     tabindex="-1" 
     *ngIf="showModal">
  
  <div class="modal-dialog modal-lg modal-dialog-scrollable">
    <div class="modal-content">
      
      <!-- MODAL HEADER -->
      <div class="modal-header">
        <h5 class="modal-title">
          <i class="bi bi-icon-name me-2"></i>
          Tiêu đề Modal
        </h5>
        <button type="button" class="btn-close" (click)="closeModal()"></button>
      </div>

      <!-- MODAL BODY -->
      <div class="modal-body">
        <!-- Loading Overlay (Optional) -->
        <div class="loading-overlay" *ngIf="loading">
          <div class="spinner-container">
            <div class="spinner-border" role="status"></div>
            <div class="loading-text">Đang tải dữ liệu...</div>
          </div>
        </div>

        <!-- Nội dung chính -->
        <div *ngIf="!loading">
          <!-- Your content here -->
        </div>
      </div>

      <!-- MODAL FOOTER (Optional) -->
      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" (click)="closeModal()">
          Đóng
        </button>
        <button type="button" class="btn btn-primary" (click)="submitAction()">
          Xác nhận
        </button>
      </div>

    </div>
  </div>
</div>
```

### 2. Các biến thể Modal

#### Modal Size
```html
<!-- Small Modal -->
<div class="modal-dialog modal-sm">

<!-- Default Modal -->
<div class="modal-dialog">

<!-- Large Modal -->
<div class="modal-dialog modal-lg">

<!-- Extra Large Modal -->
<div class="modal-dialog modal-xl">

<!-- Full Width Modal -->
<div class="modal-dialog modal-fullscreen">
```

#### Modal với Scrollable Body
```html
<div class="modal-dialog modal-dialog-scrollable">
  <!-- Body sẽ scroll, header/footer cố định -->
</div>
```

#### Modal Centered
```html
<div class="modal-dialog modal-dialog-centered">
  <!-- Modal căn giữa màn hình theo chiều dọc -->
</div>
```

### 3. Phân tích chi tiết các thuộc tính

#### Modal Backdrop
```html
<div class="modal-backdrop fade" 
     [class.show]="showModal"     <!-- Thêm class 'show' khi modal mở -->
     *ngIf="showModal"             <!-- Chỉ render khi modal mở -->
     (click)="closeModal()">       <!-- Click backdrop để đóng -->
</div>
```

**Quan trọng**: Backdrop phải đặt **NGOÀI** modal dialog để tránh bị đè z-index.

#### Modal Container
```html
<div class="modal fade"                                    <!-- Base classes -->
     [class.show]="showModal"                              <!-- Toggle visibility -->
     [style.display]="showModal ? 'block' : 'none'"        <!-- Display control -->
     tabindex="-1"                                         <!-- Accessibility -->
     *ngIf="showModal">                                    <!-- Conditional render -->
```

**Giải thích**:
- `fade`: Animation class
- `show`: Active state class
- `display: block/none`: Điều khiển hiển thị
- `tabindex="-1"`: Cho phép focus vào modal
- `*ngIf`: Remove khỏi DOM khi đóng (cleanup)

---

## 🎨 Styling SCSS

### 1. Variables cơ bản

```scss
// Design System Variables
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

### 2. Modal Backdrop

```scss
.modal-backdrop {
  position: fixed;           // Cố định vị trí
  top: 0;
  left: 0;
  z-index: 1050;            // Z-index thấp hơn modal
  width: 100vw;             // Full width viewport
  height: 100vh;            // Full height viewport
  background-color: rgba(0, 0, 0, 0.5);  // Nền đen mờ 50%

  &.fade {
    opacity: 0;             // Trạng thái ẩn
    transition: opacity 0.15s linear;
  }

  &.show {
    opacity: 1;             // Trạng thái hiện
  }
}
```

### 3. Modal Container

```scss
.modal {
  position: fixed;          // Cố định vị trí
  top: 0;
  left: 0;
  z-index: 1055;           // Z-index cao hơn backdrop
  width: 100%;
  height: 100%;
  overflow-x: hidden;      // Ẩn scroll ngang
  overflow-y: auto;        // Cho phép scroll dọc
  outline: 0;

  &.fade {
    transition: opacity 0.15s linear;
  }

  &.fade:not(.show) {
    opacity: 0;
  }

  &.show {
    opacity: 1;
  }
}
```

### 4. Modal Dialog

```scss
.modal-dialog {
  position: relative;
  width: auto;
  margin: 1.75rem auto;     // Margin trên/dưới
  pointer-events: none;     // Cho phép click qua vùng transparent
  max-width: 800px;         // Max width mặc định

  &.modal-lg {
    max-width: 900px;       // Large modal
  }

  &.modal-xl {
    max-width: 1140px;      // Extra large modal
  }

  &.modal-sm {
    max-width: 400px;       // Small modal
  }

  &.modal-dialog-scrollable {
    max-height: calc(100% - 3.5rem);

    .modal-content {
      max-height: calc(100vh - 3.5rem);
      overflow: hidden;
    }

    .modal-body {
      overflow-y: auto;     // Body scrollable
    }
  }

  &.modal-dialog-centered {
    display: flex;
    align-items: center;
    min-height: calc(100% - 3.5rem);
  }
}
```

### 5. Modal Content

```scss
.modal-content {
  position: relative;
  display: flex;
  flex-direction: column;
  width: 100%;
  pointer-events: auto;     // Bật lại pointer events
  background-color: $card-bg;
  background-clip: padding-box;
  border: 1px solid $border-color;
  border-radius: $radius-lg;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.15);  // Shadow mạnh
  outline: 0;
}
```

### 6. Modal Header

```scss
.modal-header {
  display: flex;
  flex-shrink: 0;           // Không co lại khi scroll
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid $border-color;
  border-radius: $radius-lg $radius-lg 0 0;

  .modal-title {
    margin: 0;
    font-size: 1.125rem;    // 18px
    font-weight: 600;
    color: $text-dark;
    line-height: 1.4;
  }

  .btn-close {
    padding: 8px;
    margin: -8px -8px -8px auto;
    background: transparent;
    border: 0;
    cursor: pointer;
    opacity: 0.5;
    transition: opacity 0.2s ease;

    &:hover {
      opacity: 1;
    }
  }
}
```

### 7. Modal Body

```scss
.modal-body {
  position: relative;
  flex: 1 1 auto;           // Chiếm toàn bộ không gian còn lại
  padding: 24px;
}
```

### 8. Modal Footer (Optional)

```scss
.modal-footer {
  display: flex;
  flex-wrap: wrap;
  flex-shrink: 0;
  align-items: center;
  justify-content: flex-end;  // Buttons sang phải
  padding: 16px 24px;
  border-top: 1px solid $border-color;
  border-radius: 0 0 $radius-lg $radius-lg;
  gap: 8px;                   // Khoảng cách giữa buttons
}
```

### 9. Loading Overlay trong Modal

```scss
.loading-overlay {
  position: absolute;
  inset: 0;                   // Phủ toàn bộ
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(2px); // Blur background
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10;                // Trên content
  border-radius: $radius-lg;

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

---

## 💻 TypeScript Logic

### 1. Component Properties

```typescript
export class YourComponent implements OnInit {
  // Modal state
  showModal = false;          // Điều khiển hiển thị modal
  loading = false;            // Loading state
  selectedId = 0;             // ID của item đang xem
  selectedItem: any = null;   // Data của item đang xem

  constructor(
    private apiService: YourApiService
  ) {}
}
```

### 2. Open Modal Method

```typescript
openModal(id: number): void {
  this.selectedId = id;
  this.showModal = true;
  this.loading = true;
  this.selectedItem = null;

  this.apiService.getDetail(id).subscribe({
    next: (data) => {
      this.selectedItem = data;
      this.loading = false;
    },
    error: (err) => {
      console.error('Error loading detail:', err);
      alert('Có lỗi khi tải dữ liệu');
      this.closeModal();
    }
  });
}
```

### 3. Close Modal Method

```typescript
closeModal(): void {
  this.showModal = false;
  this.selectedId = 0;
  this.selectedItem = null;
  this.loading = false;
  
  // Reset form nếu có
  // this.myForm.reset();
}
```

### 4. Submit Action Method

```typescript
submitAction(): void {
  if (!this.canProcess()) {
    alert('Không thể xử lý');
    return;
  }

  this.loading = true;

  const payload = {
    // Your payload data
  };

  this.apiService.submitAction(this.selectedId, payload).subscribe({
    next: () => {
      alert('Xử lý thành công!');
      this.closeModal();
      this.loadData(); // Reload danh sách
    },
    error: (err) => {
      console.error('Error:', err);
      alert('Có lỗi xảy ra');
      this.loading = false;
    }
  });
}
```

### 5. Helper Methods

```typescript
canProcess(): boolean {
  // Logic kiểm tra điều kiện xử lý
  return this.selectedItem?.status === 'PENDING';
}
```

### 6. Keyboard Support (Optional)

```typescript
import { HostListener } from '@angular/core';

@HostListener('document:keydown.escape', ['$event'])
onEscapeKey(event: KeyboardEvent): void {
  if (this.showModal) {
    this.closeModal();
  }
}
```

---

## ✅ Best Practices

### 1. Z-index Hierarchy

```
Modal Dialog:        z-index: 1055
Modal Backdrop:      z-index: 1050
Loading Overlay:     z-index: 10 (relative trong modal)
Toast Notifications: z-index: 9999
```

**Quan trọng**: Backdrop phải có z-index thấp hơn Modal dialog để modal luôn hiển thị trên backdrop.

### 2. Structure Order

```html
<!-- ✅ ĐÚNG: Backdrop ngoài Modal -->
<div class="modal-backdrop"></div>
<div class="modal">...</div>

<!-- ❌ SAI: Backdrop trong Modal -->
<div class="modal">
  <div class="modal-backdrop"></div>
  ...
</div>
```

### 3. Performance Optimization

```typescript
// ✅ ĐÚNG: Dùng *ngIf để remove khỏi DOM khi không dùng
<div class="modal" *ngIf="showModal">

// ❌ TRÁNH: Chỉ dùng [hidden] - vẫn render DOM
<div class="modal" [hidden]="!showModal">
```

### 4. Accessibility

```html
<!-- Thêm ARIA attributes -->
<div class="modal" 
     role="dialog" 
     aria-modal="true" 
     [attr.aria-labelledby]="'modalTitle'">
  
  <div class="modal-header">
    <h5 id="modalTitle" class="modal-title">Tiêu đề</h5>
  </div>
</div>
```

### 5. Loading States

```html
<!-- Hiển thị loading overlay khi đang tải data -->
<div class="modal-body">
  <div class="loading-overlay" *ngIf="loading">
    <div class="spinner-container">
      <div class="spinner-border"></div>
      <div class="loading-text">Đang tải...</div>
    </div>
  </div>

  <!-- Content chỉ hiện khi không loading -->
  <div *ngIf="!loading && selectedItem">
    {{ selectedItem.name }}
  </div>
</div>
```

### 6. Form trong Modal

```html
<form [formGroup]="myForm" (ngSubmit)="submitAction()">
  <div class="modal-body">
    <!-- Form fields -->
  </div>
  
  <div class="modal-footer">
    <button type="button" class="btn btn-secondary" 
            (click)="closeModal()" [disabled]="loading">
      Đóng
    </button>
    <button type="submit" class="btn btn-primary" 
            [disabled]="myForm.invalid || loading">
      <span *ngIf="loading" class="spinner-border spinner-border-sm me-2"></span>
      Xác nhận
    </button>
  </div>
</form>
```

### 7. Responsive Design

```scss
// Mobile optimization
@media (max-width: 576px) {
  .modal-dialog {
    margin: 0.5rem;
    max-width: calc(100% - 1rem);
  }

  .modal-header,
  .modal-body,
  .modal-footer {
    padding: 16px;
  }
}
```

---

## 📚 Examples

### Example 1: Modal Chi tiết + Duyệt (Project Approval)

```html
<!-- Component HTML -->
<div class="modal-backdrop fade" [class.show]="showDetailModal" 
     *ngIf="showDetailModal" (click)="closeDetailModal()">
</div>

<div class="modal fade" [class.show]="showDetailModal" 
     [style.display]="showDetailModal ? 'block' : 'none'"
     tabindex="-1" *ngIf="showDetailModal">
  
  <div class="modal-dialog modal-lg modal-dialog-scrollable">
    <div class="modal-content">
      
      <div class="loading-overlay" *ngIf="detailLoading">
        <div class="spinner-container">
          <div class="spinner-border" role="status"></div>
          <div class="loading-text">Đang tải chi tiết...</div>
        </div>
      </div>

      <div class="modal-header">
        <h5 class="modal-title">
          <i class="bi bi-file-earmark-text me-2"></i>
          Chi tiết dự án
        </h5>
        <button type="button" class="btn-close" (click)="closeDetailModal()"></button>
      </div>

      <div class="modal-body" *ngIf="!detailLoading && selectedId > 0">
        <!-- Project Info -->
        <div class="d-flex align-items-start justify-content-between mb-3">
          <div>
            <h5 class="fw-semibold mb-1">{{ selected.tenDuAn }}</h5>
            <div class="text-muted">{{ selected.maDuAn }}</div>
          </div>
          <span class="badge" [ngClass]="badgeClass(selected.trangThaiDuAn)">
            {{ selected.trangThaiDuAn }}
          </span>
        </div>

        <!-- Description -->
        <div class="mb-3">
          <label class="form-label fw-semibold">
            <i class="bi bi-card-text me-1"></i>Mô tả
          </label>
          <div class="p-3 bg-light rounded">{{ selected.moTa || '—' }}</div>
        </div>

        <!-- Dates -->
        <div class="row g-3 mb-3">
          <div class="col-md-6">
            <label class="form-label fw-semibold">Ngày bắt đầu</label>
            <div class="p-2 bg-light rounded">
              {{ (selected.ngayBatDau | date:'dd/MM/yyyy') ?? '—' }}
            </div>
          </div>
          <div class="col-md-6">
            <label class="form-label fw-semibold">Ngày kết thúc</label>
            <div class="p-2 bg-light rounded">
              {{ (selected.ngayKetThuc | date:'dd/MM/yyyy') ?? '—' }}
            </div>
          </div>
        </div>

        <hr />

        <!-- Approval Form -->
        <div class="fw-semibold mb-3">
          <i class="bi bi-check2-square me-2"></i>Quyết định duyệt
        </div>

        <div *ngIf="!canProcess()" class="alert alert-warning">
          Dự án không ở trạng thái chờ duyệt.
        </div>

        <form [formGroup]="approveForm" (ngSubmit)="submitApprove()">
          <div class="d-flex gap-2 mb-3">
            <button class="btn flex-fill" type="button" 
              [class.btn-success]="approveForm.get('dongY')?.value===true"
              [class.btn-outline-success]="approveForm.get('dongY')?.value!==true"
              (click)="approveForm.patchValue({ dongY: true })" 
              [disabled]="!canProcess() || saving">
              <i class="bi bi-hand-thumbs-up me-1"></i> Đồng ý
            </button>

            <button class="btn flex-fill" type="button" 
              [class.btn-danger]="approveForm.get('dongY')?.value===false"
              [class.btn-outline-danger]="approveForm.get('dongY')?.value!==false"
              (click)="approveForm.patchValue({ dongY: false })" 
              [disabled]="!canProcess() || saving">
              <i class="bi bi-hand-thumbs-down me-1"></i> Từ chối
            </button>
          </div>

          <div *ngIf="approveForm.get('dongY')?.value===false" class="mb-3">
            <label class="form-label fw-semibold">
              Lý do từ chối <span class="text-danger">*</span>
            </label>
            <textarea class="form-control" rows="3" 
              formControlName="lyDoTuChoi"
              placeholder="Nhập lý do từ chối..."></textarea>
          </div>

          <div class="d-flex gap-2">
            <button type="button" class="btn btn-secondary" 
              (click)="closeDetailModal()" [disabled]="saving">
              <i class="bi bi-x-lg me-1"></i>Đóng
            </button>
            <button class="btn btn-primary flex-fill" type="submit" 
              [disabled]="!canProcess() || saving">
              <span *ngIf="saving" class="spinner-border spinner-border-sm me-2"></span>
              <i class="bi bi-check-lg me-1" *ngIf="!saving"></i>
              Xác nhận
            </button>
          </div>
        </form>
      </div>

    </div>
  </div>
</div>
```

```typescript
// Component TypeScript
export class DuyetDuAnComponent implements OnInit {
  showDetailModal = false;
  detailLoading = false;
  selectedId = 0;
  selected: DuAnDetailDto = {} as DuAnDetailDto;
  saving = false;

  approveForm = this.fb.group({
    dongY: [null as boolean | null],
    lyDoTuChoi: ['']
  });

  constructor(
    private fb: FormBuilder,
    private duAnApi: DuAnApiService
  ) {}

  openDetail(id: number): void {
    this.selectedId = id;
    this.showDetailModal = true;
    this.detailLoading = true;

    this.duAnApi.getDetail(id).subscribe({
      next: (data) => {
        this.selected = data;
        this.detailLoading = false;
      },
      error: (err) => {
        console.error(err);
        alert('Lỗi khi tải chi tiết');
        this.closeDetailModal();
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal = false;
    this.selectedId = 0;
    this.selected = {} as DuAnDetailDto;
    this.approveForm.reset();
  }

  canProcess(): boolean {
    return this.selected.trangThaiDuAn === 'CHO_DUYET_GIAM_DOC';
  }

  submitApprove(): void {
    if (!this.canProcess()) return;

    const dongY = this.approveForm.get('dongY')?.value;
    if (dongY === null) {
      alert('Vui lòng chọn đồng ý hoặc từ chối');
      return;
    }

    if (dongY === false && !this.approveForm.get('lyDoTuChoi')?.value?.trim()) {
      alert('Vui lòng nhập lý do từ chối');
      return;
    }

    this.saving = true;

    const payload = {
      dongY,
      lyDoTuChoi: dongY === false ? this.approveForm.get('lyDoTuChoi')?.value : null
    };

    this.duAnApi.approve(this.selectedId, payload).subscribe({
      next: () => {
        alert('Đã duyệt thành công!');
        this.closeDetailModal();
        this.loadData();
      },
      error: (err) => {
        console.error(err);
        alert('Có lỗi xảy ra');
        this.saving = false;
      }
    });
  }
}
```

### Example 2: Simple Confirmation Modal

```html
<div class="modal-backdrop fade" [class.show]="showConfirmModal" 
     *ngIf="showConfirmModal" (click)="closeConfirm()">
</div>

<div class="modal fade" [class.show]="showConfirmModal" 
     [style.display]="showConfirmModal ? 'block' : 'none'"
     tabindex="-1" *ngIf="showConfirmModal">
  
  <div class="modal-dialog modal-dialog-centered">
    <div class="modal-content">
      
      <div class="modal-header">
        <h5 class="modal-title">
          <i class="bi bi-exclamation-triangle me-2 text-warning"></i>
          Xác nhận
        </h5>
        <button type="button" class="btn-close" (click)="closeConfirm()"></button>
      </div>

      <div class="modal-body">
        <p>Bạn có chắc chắn muốn xóa mục này?</p>
        <p class="text-muted small mb-0">Hành động này không thể hoàn tác.</p>
      </div>

      <div class="modal-footer">
        <button type="button" class="btn btn-secondary" (click)="closeConfirm()">
          Hủy
        </button>
        <button type="button" class="btn btn-danger" (click)="confirmDelete()">
          Xóa
        </button>
      </div>

    </div>
  </div>
</div>
```

---

## 🔧 Troubleshooting

### Issue 1: Modal bị đè dưới backdrop

**Nguyên nhân**: Backdrop nằm trong modal dialog hoặc z-index sai

**Giải pháp**:
```html
<!-- ✅ ĐÚNG -->
<div class="modal-backdrop" [class.show]="showModal"></div>
<div class="modal" [class.show]="showModal">
  <div class="modal-dialog">...</div>
</div>

<!-- ❌ SAI -->
<div class="modal">
  <div class="modal-backdrop"></div>
  <div class="modal-dialog">...</div>
</div>
```

### Issue 2: Không scroll được modal body

**Giải pháp**:
```html
<div class="modal-dialog modal-dialog-scrollable">
  <!-- Thêm class modal-dialog-scrollable -->
</div>
```

### Issue 3: Modal không đóng khi click backdrop

**Kiểm tra**:
```html
<div class="modal-backdrop" (click)="closeModal()">
  <!-- Đảm bảo có event handler -->
</div>
```

### Issue 4: Animation không hoạt động

**Kiểm tra CSS**:
```scss
.modal.fade {
  transition: opacity 0.15s linear;
}

.modal-backdrop.fade {
  transition: opacity 0.15s linear;
}
```

---

## 📝 Checklist khi tạo Modal mới

- [ ] Backdrop đặt **ngoài** modal dialog
- [ ] Backdrop có z-index = 1050
- [ ] Modal dialog có z-index = 1055
- [ ] Thêm class `fade` và `show` cho animation
- [ ] Dùng `*ngIf` để conditional render
- [ ] Thêm `(click)="closeModal()"` cho backdrop
- [ ] Có button close (×) trong header
- [ ] Loading overlay khi fetch data
- [ ] Reset state khi đóng modal (closeModal method)
- [ ] Disable buttons khi đang xử lý
- [ ] Validation cho form (nếu có)
- [ ] Handle error cases
- [ ] Responsive cho mobile
- [ ] Accessibility attributes (aria-*)

---

## 🎓 Summary

### HTML Structure
```
modal-backdrop (z-1050)
modal (z-1055)
  └─ modal-dialog
       └─ modal-content
            ├─ modal-header
            ├─ modal-body
            └─ modal-footer (optional)
```

### Key CSS Properties
- **Backdrop**: `position: fixed`, `z-index: 1050`, `rgba(0,0,0,0.5)`
- **Modal**: `position: fixed`, `z-index: 1055`, fade animation
- **Dialog**: `max-width` variants, `modal-dialog-scrollable`
- **Content**: `border-radius: 12px`, box-shadow

### TypeScript Pattern
```typescript
showModal = false;        // State
openModal(id) {...}      // Open + fetch data
closeModal() {...}       // Close + cleanup
submitAction() {...}     // Submit + reload
```

---

**Version**: 1.0  
**Last Updated**: January 6, 2026  
**Author**: QLNS ERP Team
