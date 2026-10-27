# Architecture Specification — HaveTickets

## System Topology & C4 Architecture

HaveTickets is organized as a modular Clean Architecture system backed by an SQLite relational persistence layer with partial unique constraint enforcement.

```mermaid
graph TB
    subgraph Clients["Presentation Clients"]
        VueClient["HaveTickets.Client<br/><code>Vue 3 + TypeScript (Port 5391)</code>"]
        BlazorClient["HaveTickets.WebUI<br/><code>Blazor WebAssembly (Port 5390)</code>"]
    end

    subgraph ApiBoundary["API Layer (:8190)"]
        ApiEndpoints["HaveTickets.Api<br/><code>Minimal APIs &amp; Route Handlers</code>"]
        ProblemDetails["ProblemDetails Exception Filter<br/><i>RFC 7807 Domain Error Mapping</i>"]
        RateLimiter["Sliding Window Rate Limiter"]
    end

    subgraph ApplicationLayer["Application Layer"]
        Queries["Catalog &amp; Ticket Queries"]
        Commands["ReserveSeat &amp; CheckIn Handlers"]
        Ports["Persistence Ports &amp; QR Generator Abstraction"]
    end

    subgraph DomainLayer["Domain Layer (Zero Dependencies)"]
        Aggregates["Showtime &amp; Order Aggregate Roots"]
        Entities["Ticket Entity &amp; Lifecycle State Machine"]
        ValueObjects["Money, SeatNumber, ConfirmationCode"]
        DomainRules["Door Grace Period [-30m, +30m]"]
    end

    subgraph InfrastructureLayer["Infrastructure Layer"]
        EfContext["HaveTicketsDbContext<br/><code>EF Core SQLite</code>"]
        PartialIndex[("UX_Tickets_ShowtimeId_Seat_Active<br/><i>WHERE Status &lt;&gt; 'Cancelled'</i>")]
        QrService["QRCoder Adapter"]
        SystemClock["TimeProvider Adapter"]
    end

    VueClient -->|"HTTP REST / JSON"| ApiEndpoints
    BlazorClient -->|"HTTP REST / JSON"| ApiEndpoints
    ApiEndpoints --> RateLimiter
    RateLimiter --> ProblemDetails
    ProblemDetails --> Commands
    ProblemDetails --> Queries
    Commands --> Ports
    Queries --> Ports
    Ports --> DomainLayer
    Commands --> DomainLayer
    InfrastructureLayer --> Ports
    EfContext --> PartialIndex
```

---

## Layer Separation & Responsibilities

```text
HaveTickets.slnx
├── HaveTickets.Contracts          # Independent DTOs and API boundary contracts (V1)
├── HaveTickets.Domain             # Pure enterprise domain: entities, aggregates, value objects, rules
├── HaveTickets.Application        # CQRS use cases: handlers, queries, commands, abstractions
├── HaveTickets.Infrastructure     # EF Core, SQLite persistence, migrations, read models, QR generator
├── HaveTickets.Api                # Minimal APIs, Scalar OpenAPI, rate limiting, ProblemDetails
├── HaveTickets.Domain.Tests       # xUnit v3 + Shouldly + CsCheck (Property-Based Testing)
├── HaveTickets.Api.Tests          # Isolated in-memory WebApplicationFactory integration suite
├── HaveTickets.WebUI              # Blazor WebAssembly frontend (Port 5390)
└── HaveTickets.Client             # Vue 3 + TypeScript Vite frontend (Port 5391)
```

### Dependency Inversion Rules
1. **Domain Purity**: `HaveTickets.Domain` contains no dependencies on EF Core, ASP.NET Core, or external packages. All validations and business rules execute in-memory.
2. **Contract Decoupling**: `HaveTickets.Contracts` provides serializable request/response DTOs shared between the API Gateway and frontend clients, insulating domain aggregates from serialization formats.
3. **Database Concurrency Arbiter**: Concurrency guarantees rely on database engine semantics:
   ```sql
   CREATE UNIQUE INDEX "UX_Tickets_ShowtimeId_Seat_Active"
   ON "Tickets" ("ShowtimeId", "Seat")
   WHERE "Status" <> 'Cancelled';
   ```
4. **Time Decoupling**: All time inspections utilize `System.TimeProvider` rather than static `DateTime.UtcNow`, guaranteeing 100% deterministic test execution.
