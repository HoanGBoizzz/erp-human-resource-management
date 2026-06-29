# SetUp FE - QLNS_ERP_FE

File nay duoc tao lai de setup nhanh Frontend.

## 1. Yeu cau moi truong

- Node.js 18 LTS (khuyen nghi 18.20.x)
- npm 9.x hoac 10.x
- Angular CLI 16.2.x (co the dung local CLI trong du an)

Kiem tra:

```bash
node -v
npm -v
npx ng version
```

## 2. Cai dat dependencies

```bash
cd QLNS_ERP_FE
npm ci
```

Neu khong dung npm ci:

```bash
npm install
```

## 3. Chay du an

```bash
npm start
```

Mac dinh app chay tai:
- http://localhost:4200

## 4. Cau hinh API

- Moi truong dev: src/environments/environment.development.ts
- Moi truong prod: src/environments/environment.ts

Thong so chinh can quan tam:
- apiBaseUrl

## 5. Scripts

- npm run ng
- npm start
- npm run build
- npm run watch
- npm test

## 6. Tai lieu day du

Noi dung chi tiet ve cau hinh moi truong va cau truc thu muc da duoc cap nhat trong README.md.
