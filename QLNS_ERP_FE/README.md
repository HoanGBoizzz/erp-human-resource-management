# QLNS_ERP_FE

Frontend application for the **ERP Human Resource Management System**, developed using **Angular 16**. This project provides the user interface for employee management, attendance tracking, payroll, authentication, and other HR-related features.

---

# 1. Technologies

- Angular CLI: 16.2.16
- Angular Framework: 16.2.x
- TypeScript: ~5.1.3
- RxJS: ~7.8.0
- SCSS
- Bootstrap 5
- Bootstrap Icons

---

# 2. System Requirements

To ensure compatibility and avoid dependency issues, it is recommended to use the following versions:

- Node.js 18 LTS (recommended: 18.20.x)
- npm 9.x or 10.x

Verify your environment:

```bash
node -v
npm -v
npx ng version
```

> **Note**
>
> - Installing Angular CLI globally is optional because the project already includes `@angular/cli` in `devDependencies`.
> - It is recommended to use `npx ng` to execute the local Angular CLI version.

---

# 3. Installation

Navigate to the frontend directory:

```bash
cd QLNS_ERP_FE
```

Install dependencies:

```bash
npm ci
```

Or:

```bash
npm install
```

Run the development server:

```bash
npm start
```

The application will be available at:

```
http://localhost:4200
```

---

# 4. Available npm Scripts

| Command | Description |
|---------|-------------|
| `npm start` | Start the Angular development server |
| `npm run build` | Build the application |
| `npm run watch` | Build in watch mode |
| `npm test` | Run unit tests using Karma & Jasmine |
| `npm run ng` | Execute Angular CLI commands |

---

# 5. Environment Configuration

The project uses two environment configuration files:

```
src/environments/environment.development.ts
src/environments/environment.ts
```

### Development

```text
production = false
apiBaseUrl = http://localhost:5042
```

### Production

```text
production = true
apiBaseUrl = https://YOUR_RENDER_APP.onrender.com
```

## Environment Replacement

When running the application locally:

```bash
npm start
```

Angular automatically replaces:

```
src/environments/environment.ts
```

with

```
src/environments/environment.development.ts
```

During a production build, Angular uses:

```
environment.ts
```

### Backend Configuration

For local development:

```
apiBaseUrl = http://localhost:5042
```

For production deployment:

Replace the API URL in `environment.ts` with the actual backend endpoint.

---

# 6. Project Structure

## Root Directory

```
.angular/           Angular CLI cache
.vscode/            VS Code workspace settings
node_modules/       Installed npm packages
dist/               Production build output
src/                Application source code
angular.json        Angular workspace configuration
package.json        Project metadata and dependencies
package-lock.json   Dependency lock file
tsconfig*.json      TypeScript configuration
vercel.json         Vercel deployment configuration
```

## Source Folder

```
src/
│── app/
│── assets/
│── environments/
│── scss/
│── index.html
│── main.ts
│── styles.scss
```

### app/

```
core/
features/
shared/
app-routing.module.ts
app.module.ts
app.component.*
```

### core/

```
guards/
interceptors/
layout/
models/
services/
```

### features/

```
admin/
attendance-kiosk/
auth/
employee/
gd/
hr/
not-found/
```

### shared/

```
components/
directives/
enums/
pipes/
services/
material.module.ts
shared.module.ts
```

---

# 7. Main Dependencies

## Angular

- @angular/core
- @angular/common
- @angular/forms
- @angular/router
- @angular/platform-browser

## UI

- @angular/material
- @angular/cdk
- Bootstrap 5
- Bootstrap Icons

## Charts

- Chart.js
- @swimlane/ngx-charts

## Reports

- ExcelJS
- xlsx
- jsPDF
- html2canvas

## Real-time Communication

- @microsoft/signalr

---

# 8. Common Setup Issues

## 'ng' is not recognized

Run:

```bash
npm ci
npx ng serve
```

---

## Node.js Version Compatibility

Switch to **Node.js 18 LTS**, remove the existing dependencies, and reinstall them.

PowerShell:

```powershell
Remove-Item -Recurse -Force node_modules
npm ci
```

---

# 9. Quick Start

```bash
cd QLNS_ERP_FE
npm ci
npm start
```

---

# Additional Information

For more detailed installation instructions, dependency configuration, and troubleshooting, please refer to **SetUp.md**.
