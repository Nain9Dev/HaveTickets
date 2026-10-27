# Runbook — HaveTickets

## Assigned Ports & Endpoints

Assigned development ports strictly follow `dev-ports.json`:

| Service | Technology | Port | Assigned URL |
| :--- | :--- | :--- | :--- |
| **HaveTickets.Api** | ASP.NET Core 10 Minimal APIs | `8190` | `http://localhost:8190` |
| **HaveTickets.Client** | Vue 3 + TypeScript + Vite | `5391` | `http://localhost:5391` |
| **HaveTickets.WebUI** | Blazor WebAssembly (.NET 10) | `5390` | `http://localhost:5390` |

If port collisions occur:

```powershell
netstat -ano | findstr :8190
Get-Process -Id <pid>
```

---

## Service Startup Instructions

### 1. Start the Backend API Gateway

```powershell
$env:ASPNETCORE_URLS='http://localhost:8190'
dotnet run --project HaveTickets.Api --no-launch-profile
```

Available endpoints:
- OpenAPI / Scalar Documentation: <http://localhost:8190/scalar/v1>
- Health Check: <http://localhost:8190/healthz>

### 2. Start the Showcase Vue 3 Client

In a separate terminal:

```bash
cd HaveTickets.Client
npm install
npm run dev
```

Access the interactive ticketing and door check-in client at <http://localhost:5391>.

### 3. Start the Blazor WebAssembly Client (Optional)

```powershell
$env:ASPNETCORE_URLS='http://localhost:5390'
dotnet run --project HaveTickets.WebUI --no-launch-profile
```

---

## Verification & Testing

### Automated Test Suites

```bash
# Run all 120 automated unit, domain property, and API integration tests
dotnet test HaveTickets.slnx

# Strict TypeScript check and production build for Vue client
cd HaveTickets.Client
npm run build
```

### Health Check Probe

```bash
curl http://localhost:8190/healthz
```

Expected response: `Healthy` (HTTP 200).

---

## Operational Boundaries

1. **SQLite Database Concurrency**: In development, SQLite runs in WAL mode with connection pooling. The partial unique index guarantees mathematical safety against concurrent overbooking.
2. **Door Check-In Grace Window**: Tickets are valid for scanning strictly between `ShowtimeStart - 30 minutes` and `ShowtimeStart + 30 minutes`. Outside this window, check-in returns `admitted: false`.
3. **Idempotent Turnstile Scans**: A ticket admitted once cannot be reused. Secondary scans immediately return `admitted: false` with reason `ticket.already_checked_in`.
