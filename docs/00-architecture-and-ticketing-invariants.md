# HaveTickets: Architecture & Ticketing Invariants

A deterministic, high-concurrency cinema ticketing and seat reservation platform built with .NET 10, Clean Architecture, and dual frontends (Blazor WASM and Vue 3 + TypeScript).

---

## 1. System Topology & Development Ports

Assigned development ports strictly follow `D:/Development/Tools/dev-ports.json`:

| Service | Technology | Port | Purpose |
| :--- | :--- | :--- | :--- |
| **HaveTickets.Api** | ASP.NET Core 10 Minimal APIs | `8190` | Core transactional API, Scalar OpenAPI, HealthChecks |
| **HaveTickets.Client** | Vue 3 + TypeScript + Vite | `5391` | Dark glassmorphic ticketing client & door inspector |
| **HaveTickets.WebUI** | Blazor WebAssembly (.NET 10) | `5390` | WebAssembly client demo |

### Running the Services

```powershell
# 1. Start the Backend API
$env:ASPNETCORE_URLS='http://localhost:8190'; dotnet run --project HaveTickets.Api --no-launch-profile

# 2. Start the Vue 3 Client (dev server with API proxy on 8190)
npm --prefix HaveTickets.Client run dev

# 3. Start the Blazor WebAssembly Client (optional)
$env:ASPNETCORE_URLS='http://localhost:5390'; dotnet run --project HaveTickets.WebUI --no-launch-profile
```

---

## 2. Architectural Layers

The solution is organized following modular Clean Architecture principles:

```
HaveTickets.slnx
├── HaveTickets.Contracts          # Independent DTOs and API boundary contracts (V1)
├── HaveTickets.Domain             # Pure enterprise domain: entities, aggregates, value objects, rules
├── HaveTickets.Application        # CQRS use cases: handlers, queries, commands, abstractions
├── HaveTickets.Infrastructure     # EF Core, SQLite persistence, migrations, read models, QR generator
├── HaveTickets.Api                # Minimal APIs, Scalar OpenAPI, rate limiting, ProblemDetails
├── HaveTickets.Domain.Tests       # xUnit v3 + Shouldly + CsCheck (Property-Based Testing)
├── HaveTickets.Api.Tests          # Isolated in-memory WebApplicationFactory integration suite
├── HaveTickets.WebUI              # Blazor WebAssembly frontend
└── HaveTickets.Client             # Vue 3 + TypeScript Vite frontend
```

---

## 3. Core Domain Invariants

### 3.1 Overbooking Prevention & Concurrency Arbiter
In ticketing systems, software checks for seat availability before purchase are subject to race conditions. If two users check seat `C7` simultaneously, both see it as available.

HaveTickets resolves this deterministically at the database level:
- In `TicketConfiguration.cs`, a partial unique index is enforced on `(ShowtimeId, Seat)`:
  ```sql
  CREATE UNIQUE INDEX "UX_Tickets_ShowtimeId_Seat_Active"
  ON "Tickets" ("ShowtimeId", "Seat")
  WHERE "Status" <> 'Cancelled';
  ```
- Any second conflicting transaction attempting to reserve the same seat triggers a unique constraint violation.
- The unit of work intercepts this violation and maps it cleanly to the domain error `seat.already_taken`, returning an HTTP `409 Conflict` response with ProblemDetails.
- Because cancelled tickets are excluded from the index, cancelling a ticket immediately returns its seat to circulation without tombstone deletion.

### 3.2 Exact Monetary Math
Floating-point numbers (`float`, `double`) and plain `decimal` columns in SQLite lose precision or degrade into floating-point representation (`REAL`).

- `Money` value object stores amounts as integer minor units (`long AmountInMinorUnits`) and an ISO 4217 currency (`string Currency`).
- Arithmetic is exact minor-unit integer calculation.
- Round-trip commutativity and addition correctness are verified with property-based testing (`CsCheck`).

### 3.3 Ticket Lifecycle & Door Check-In Window
Each ticket undergoes deterministic one-way status transitions:

```
[Issued] ──(CheckIn at door within [-30m, +30m] window)──> [CheckedIn]
   │
   └──(Cancel before sales cutoff time)──────────────────> [Cancelled]
```

1. **Idempotency**: An admitted ticket cannot be used a second time by a screenshot or copied QR code. Secondary attempts yield an immediate rejection: `ticket.already_checked_in`.
2. **Door Grace Period**: Check-in is strictly valid starting 30 minutes before showtime and closing 30 minutes after start.
3. **Transport Decoupling**: Door scan results return HTTP 200 with `admitted: false` and a typed failure reason (`CheckInResponse`), reflecting that a refused scan is an expected operational event, not an HTTP infrastructure failure.

---

## 4. Verification & Testing

Every domain and API rule is bracketed by automated test coverage:

- **Domain Tests (`HaveTickets.Domain.Tests`)**: 74 tests passing.
  - Value object invariants (Money, SeatNumber, ConfirmationCode).
  - Property-based testing via `CsCheck` for arbitrary entropy and algebraic laws.
  - Aggregate root order validation and seat caps.
- **API Tests (`HaveTickets.Api.Tests`)**: 42 integration tests passing.
  - End-to-end integration via `ApiFactory` against isolated throw-away SQLite instances.
  - Deterministic time-travel using `MutableTimeProvider` without thread sleeps.
  - Concurrency conflict handling, rate limiting, and ProblemDetails verification.
