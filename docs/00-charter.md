# Charter — HaveTickets

## Mission
HaveTickets is a deterministic, high-concurrency cinema ticketing and seat reservation platform built with .NET 10, Clean Architecture, and dual client frontends (Blazor WebAssembly and Vue 3 + TypeScript). It guarantees mathematical invariant enforcement, zero overbooking under race conditions, exact monetary math without rounding drift, and idempotent door check-in validation.

## Strategic Objectives
1. **Mathematical Overbooking Prevention**: Eliminate double-booking race conditions through database-enforced partial unique indexing rather than optimistic or volatile in-memory locking.
2. **Exact Monetary Integrity**: Enforce integer minor-unit arithmetic for all currency operations, eliminating floating-point rounding degradation in relational persistence.
3. **Deterministic Ticket Lifecycle & Door Check-In**: Provide idempotent, time-windowed ticket redemption for venue access control, returning typed operational responses rather than infrastructure exceptions.
4. **Clean Layered Architecture**: Strictly decouple pure domain rules from database persistence, HTTP presentation, and client frameworks.
5. **Dual Presentation Verification**: Validate domain API contracts against both modern single-page client architectures (Vue 3 + TypeScript) and compiled browser-native runtimes (Blazor WebAssembly).

## Core Architectural Invariants
- **Database-Level Concurrency Arbiter**: The uniqueness of an active seat reservation `(ShowtimeId, Seat)` is enforced by a partial unique index where `Status <> 'Cancelled'`. Cancelling a ticket immediately frees the seat without tombstone cleanup.
- **Pure Domain Boundary**: Entities, value objects (`Money`, `SeatNumber`, `ConfirmationCode`), and domain aggregates have zero dependencies on external frameworks or EF Core attributes.
- **Time Virtualization**: All time-sensitive operations (door check-in grace period `[-30m, +30m]`, sales cutoff) depend on `TimeProvider`, enabling deterministic property-based and integration test coverage.
- **Typed Error Contracts**: API endpoints translate domain failures into RFC 7807 `ProblemDetails` with machine-readable error codes (`seat.already_taken`, `ticket.already_checked_in`, `ticket.outside_window`).
