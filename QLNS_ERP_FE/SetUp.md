# SetUp FE - QLNS_ERP_FE

To ensure compatibility and avoid dependency issues, it is recommended to use the following environment:

- **Node.js:** 18 LTS (recommended: 18.20.x)
- **npm:** 9.x or 10.x
- **Angular CLI:** 16.2.x (using the local CLI included in the project is recommended)

Verify your environment:

```bash
node -v
npm -v
npx ng version
```

> **Note**
>
> Installing Angular CLI globally is optional because the project already includes `@angular/cli` in `devDependencies`. It is recommended to use `npx ng` to execute the project's local Angular CLI version.

---

## 2. Install Dependencies

Navigate to the frontend project directory:

```bash
cd QLNS_ERP_FE
```

Install all required dependencies:

```bash
npm ci
```

Alternatively:

```bash
npm install
```

---

## 3. Run the Application

Start the development server:

```bash
npm start
```

By default, the application will be available at:

```
http://localhost:4200
```

---

## 4. API Configuration

The project uses two environment configuration files:

- Development: `src/environments/environment.development.ts`
- Production: `src/environments/environment.ts`

The main configuration value is:

- `apiBaseUrl`

Update this value according to the backend API endpoint you want to connect to.

---

## 5. Available Scripts

| Command | Description |
| ------- | ----------- |
| `npm run ng` | Run Angular CLI commands |
| `npm start` | Start the Angular development server |
| `npm run build` | Build the application for production |
| `npm run watch` | Build in watch mode |
| `npm test` | Run unit tests using Karma and Jasmine |

---

## 6. Additional Documentation

For more detailed information about:

- Project structure
- Environment configuration
- Dependencies
- Installation
- Troubleshooting

please refer to **README.md**.
