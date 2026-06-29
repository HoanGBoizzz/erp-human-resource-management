# 📅 Hướng dẫn xử lý Date Format trong Đơn Nghỉ Phép

## ⚠️ VẤN ĐỀ THƯỜNG GẶP

### Người dùng thấy gì?
- **Danh sách đơn**: `25/12/2024` (dd/mm/yyyy) ✅
- **Form input**: Có thể thấy `12/25/2024` (mm/dd/yyyy) hoặc `25/12/2024` tùy browser ⚠️

### Tại sao lại như vậy?

**Input `type="date"` trong HTML5 hoạt động như sau:**
```html
<!-- Value LUÔN phải là yyyy-MM-dd -->
<input type="date" value="2024-12-25">
```

**Nhưng hiển thị:**
- Browser ở **Việt Nam**: Hiển thị `25/12/2024`
- Browser ở **Mỹ**: Hiển thị `12/25/2024`
- Browser ở **Nhật**: Hiển thị `2024/12/25`

---

## ✅ GIẢI PHÁP ĐÃ TRIỂN KHAI

### 1. **Format nhất quán trong code**

```typescript
// Backend trả về ISO string
const fromBackend = "2024-12-25T00:00:00";

// Convert sang yyyy-MM-dd cho input
const forInput = formatIsoToInputDate(fromBackend); // "2024-12-25"

// Hiển thị cho user (dd/mm/yyyy)
const forDisplay = dateVnPipe.transform(fromBackend); // "25/12/2024"
```

### 2. **Helper Method**

```typescript
private formatIsoToInputDate(isoDateString: string): string {
  // "2024-12-25T00:00:00" -> "2024-12-25"
  return isoDateString.split('T')[0];
}
```

### 3. **Pipe hiển thị**

```typescript
// src/app/shared/pipes/date-vn.pipe.ts
{{ date | dateVn }}  // "25/12/2024"
```

---

## 🎯 CHUẨN SỬ DỤNG

### ✅ ĐÚNG

```typescript
// 1. Set value cho input[type="date"]
this.form.patchValue({
  tuNgay: this.formatIsoToInputDate(data.tuNgay) // "2024-12-25"
});

// 2. Hiển thị trong template
{{ data.tuNgay | dateVn }} // "25/12/2024"

// 3. Gửi lên server
const payload = {
  tuNgay: this.form.value.tuNgay // "2024-12-25" (browser tự convert)
};
```

### ❌ SAI

```typescript
// ❌ Set value dd/mm/yyyy cho input[type="date"]
this.form.patchValue({
  tuNgay: "25/12/2024" // SAI - browser sẽ không hiểu
});

// ❌ Không dùng pipe khi hiển thị
{{ data.tuNgay }} // "2024-12-25T00:00:00" - không thân thiện

// ❌ Convert thủ công không cần thiết
const parts = date.split('/');
const formatted = `${parts[2]}-${parts[1]}-${parts[0]}`; // Phức tạp không cần
```

---

## 🔍 DEBUG

### Console Log để kiểm tra
```typescript
console.log('📝 Date formats:', {
  fromBackend: data.tuNgay,           // "2024-12-25T00:00:00"
  forInput: formatted,                // "2024-12-25"
  userSees: dateVnPipe.transform(),   // "25/12/2024"
  formValue: this.form.value.tuNgay   // "2024-12-25"
});
```

### Kiểm tra trong Browser DevTools
1. Inspect input element
2. Check `value` attribute → Luôn là `yyyy-MM-dd`
3. Check hiển thị → Tùy theo locale của browser

---

## 📚 TÀI LIỆU THAM KHẢO

- [MDN: input type="date"](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/date)
- [ISO 8601 Date Format](https://en.wikipedia.org/wiki/ISO_8601)
- Angular DatePipe: https://angular.io/api/common/DatePipe

---

## 🚨 LƯU Ý QUAN TRỌNG

**KHÔNG BAO GIỜ:**
1. ❌ Dùng `dd/mm/yyyy` cho input value
2. ❌ Hiển thị raw date từ backend
3. ❌ Giả định browser sẽ hiển thị format cố định

**LUÔN LUÔN:**
1. ✅ Dùng `yyyy-MM-dd` cho input value
2. ✅ Dùng `dateVn` pipe để hiển thị
3. ✅ Test trên nhiều browser/locale khác nhau

---

**Cập nhật lần cuối:** 25/12/2024
**Người maintain:** Dev Team
