# QLNS_ERP_FE

Frontend cho hệ thống quản lý nhân sự, xây dựng bằng Angular 16.

## 1. Công nghệ và phiên bản chính

- Angular CLI: 16.2.16
- Angular framework: 16.2.x
- TypeScript: ~5.1.3
- RxJS: ~7.8.0
- SCSS + Bootstrap 5 + Bootstrap Icons

## 2. Yêu cầu môi trường

Khuyến nghị dùng đúng phiên bản để tránh lỗi lệch thư viện.

- Node.js: 18 LTS (khuyên dùng 18.20.x)
- npm: 9.x hoặc 10.x

Kiểm tra nhanh:

```bash
node -v
npm -v
npx ng version
```

Luu y:

- Khong bat buoc cai Angular CLI global vi du an da co @angular/cli trong devDependencies.
- Co the dung npx ng ... de chay CLI local theo dung version cua du an.

## 3. Cai dat va chay du an

Di chuyen vao thu muc FE:

```bash
cd QLNS_ERP_FE
```

Cai dependencies:

```bash
npm ci
```

Neu can cai theo cach thong thuong:

```bash
npm install
```

Chay local:

```bash
npm start
```

Ung dung mac dinh chay tai:

- http://localhost:4200

## 4. Cac script npm dang co

- npm run ng: chay Angular CLI
- npm start: chay dev server (ng serve)
- npm run build: build ung dung
- npm run watch: build watch voi cau hinh development
- npm test: chay unit test voi Karma/Jasmine

## 5. Cau hinh moi truong (environment)

Du an su dung 2 file moi truong:

- src/environments/environment.development.ts
- src/environments/environment.ts

Noi dung chinh:

- Development:
  - production = false
  - apiBaseUrl = http://localhost:5042
- Production:
  - production = true
  - apiBaseUrl = https://YOUR_RENDER_APP.onrender.com

### Co che thay the file environment

Trong angular.json, cau hinh serve mac dinh la development.

Khi chay local bang npm start, Angular su dung file replacement:

- thay src/environments/environment.ts
- bang src/environments/environment.development.ts

Khi build production, su dung gia tri trong environment.ts.

### Can chinh gi khi ket noi Backend

- Chay local BE: giu apiBaseUrl trong environment.development.ts la http://localhost:5042 (hoac cap nhat theo cong BE that su)
- Deploy production: doi apiBaseUrl trong environment.ts thanh URL backend thuc te

## 6. Cau truc thu muc du an va tac dung

### Thu muc goc

- .angular/: cache va du lieu tam cua Angular CLI
- .vscode/: cau hinh VS Code cho workspace
- node_modules/: toan bo goi npm da cai
- dist/: output sau khi build
- src/: ma nguon chinh cua ung dung
- angular.json: cau hinh build/serve/test cua Angular
- package.json: scripts va danh sach dependencies
- package-lock.json: khoa phien ban goi de dong bo moi truong
- tsconfig.json, tsconfig.app.json, tsconfig.spec.json: cau hinh TypeScript
- vercel.json: cau hinh deploy Vercel (neu dung)

### Thu muc src

- src/main.ts: diem vao khoi dong Angular app
- src/index.html: HTML host goc
- src/styles.scss: global styles
- src/assets/: tai nguyen tinh (anh, icon, file static)
- src/scss/: cac file SCSS dung chung
- src/environments/: cau hinh moi truong (dev/prod)
- src/app/: logic ung dung

### Thu muc src/app

- src/app/app.module.ts: module goc cua ung dung
- src/app/app-routing.module.ts: khai bao route tong
- src/app/app.component.\*: component goc
- src/app/core/: cac thanh phan dung toan app, thuong chi import 1 lan
- src/app/features/: module/man hinh theo nghiep vu
- src/app/shared/: thanh phan tai su dung chung cho nhieu feature

### Thu muc src/app/core

- core.module.ts: module gom tai nguyen core
- guards/: route guard (bao ve route)
- interceptors/: chan request/response HTTP
- layout/: layout khung ung dung
- models/: model dung chung cap app
- services/: service cap he thong (auth, api, ...)

### Thu muc src/app/features

- admin/: chuc nang quan tri
- attendance-kiosk/: cham cong kiosk
- auth/: dang nhap, xac thuc, phan quyen
- employee/: nghiep vu nhan vien
- gd/: nhom chuc nang lien quan giam doc
- hr/: nhom chuc nang nhan su
- not-found/: trang 404

### Thu muc src/app/shared

- components/: UI components dung lai
- directives/: custom directives
- enums/: danh sach enum dung chung
- pipes/: custom pipes
- services/: service dung chung
- material.module.ts: tap trung import/export Angular Material
- shared.module.ts: module chia se tai su dung

## 7. Cac goi npm quan trong

Nhom Angular co ban:

- @angular/core
- @angular/common
- @angular/forms
- @angular/router
- @angular/platform-browser

Nhom UI va bieu do:

- @angular/material
- @angular/cdk
- bootstrap
- bootstrap-icons
- chart.js
- @swimlane/ngx-charts

Nhom xuat bao cao:

- exceljs
- xlsx
- jspdf
- html2canvas

Nhom giao tiep realtime:

- @microsoft/signalr

## 8. Loi thuong gap khi setup

### Loi ng is not recognized

```bash
npm ci
npx ng serve
```

### Loi khong tuong thich Node

- Chuyen sang Node 18 LTS
- Xoa node_modules va cai lai dependencies

PowerShell:

```powershell
Remove-Item -Recurse -Force node_modules
npm ci
```

## 9. Quy trinh nhanh cho may moi

```bash
cd QLNS_ERP_FE
npm ci
npm start
```

Neu can thong tin setup chi tiet hon ve dependencies va cach xu ly loi, xem them file SetUp.md.
