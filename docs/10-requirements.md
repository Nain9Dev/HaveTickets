# Requirements Specification — HaveTickets

## Overview
This document specifies the functional and operational requirements for HaveTickets, formulated adhering to Easy Approach to Requirements Syntax (EARS).

---

## Reservation & Concurrency Requirements

### REQ-TKT-001: Zero Overbooking under Concurrent Purchases
- **Type**: Unwanted Behavior / Concurrency
- **Statement**: If two or more concurrent transactions attempt to reserve the same seat for a showtime, the system shall permit exactly one reservation to succeed and reject conflicting attempts with HTTP `409 Conflict` and error code `seat.already_taken`.

### REQ-TKT-002: Automatic Seat Re-Circulation upon Cancellation
- **Type**: Event-driven
- **Statement**: When an active ticket is cancelled prior to the sales cutoff time, the system shall release the seat reservation immediately, permitting subsequent customers to reserve the seat without tombstone purge.

### REQ-TKT-003: Exact Monetary Math
- **Type**: Ubiquitous / Integrity
- **Statement**: The system shall compute all financial transactions using integer minor units (`long AmountInMinorUnits`) and ISO 4217 currency codes, preventing decimal floating-point rounding degradation in calculation and storage.

---

## Door Check-In & Access Control Requirements

### REQ-TKT-004: Door Check-In Time Window
- **Type**: State-driven
- **Statement**: While the current timestamp is within the range `[ShowtimeStart - 30 minutes, ShowtimeStart + 30 minutes]`, the door access system shall accept valid tickets for admission; otherwise, it shall refuse check-in with `admitted: false` and reason `ticket.outside_window`.

### REQ-TKT-005: Idempotent Door Redemption
- **Type**: Unwanted Behavior / Security
- **Statement**: If a previously admitted ticket QR or confirmation code is presented for entry a second time, the access system shall refuse admission with `admitted: false` and reason `ticket.already_checked_in`.

### REQ-TKT-006: Non-Crashing Inspection Responses
- **Type**: Ubiquitous / Protocol
- **Statement**: The door check-in API shall return HTTP `200 OK` with a structured `CheckInResponse` payload (`admitted: boolean`, `reason: string?`, `admittedAt: DateTimeOffset?`) for both successful admissions and operational rejections.

---

## Client & Architecture Requirements

### REQ-TKT-007: Dual Client Support
- **Type**: Ubiquitous
- **Statement**: The platform shall expose REST endpoints consumed interchangeably by the Blazor WebAssembly client (port `5390`) and the Vue 3 + TypeScript client (port `5391`).

### REQ-TKT-008: Time Virtualization for Verification
- **Type**: Ubiquitous / Testability
- **Statement**: All domain time calculations shall consume `TimeProvider`, enabling deterministic simulated time-travel in automated tests without thread sleeps.

### REQ-TKT-009: Bilingual Interface & Ecosystem Synchronization
- **Type**: Ubiquitous / Accessibility
- **Statement**: The client interface shall provide Spanish and English language switching (`[ES | EN]`), persist user selection, synchronize with `nain:locale-change` custom events, and comply with WCAG 2.2 AA standards.

---

## Discoverability Requirements

The public demo is the Blazor WebAssembly shell published to GitHub Pages; these requirements apply to the static files in `HaveTickets.WebUI/wwwroot/` that the browser receives.

### REQ-TKT-010: Canonical URL and Description
- **Type**: Ubiquitous / Discoverability
- **Statement**: The entry page shall declare exactly one canonical link equal to `https://tickets.naindev.com/` and exactly one meta description of 70 to 160 characters.

### REQ-TKT-011: Social Cards
- **Type**: Event-driven
- **Statement**: When a link to the demo is shared, the entry page shall expose Open Graph tags (`og:url` equal to the canonical, absolute `og:image`) and a `summary_large_image` Twitter card.

### REQ-TKT-012: Structured Data and Author Link
- **Type**: Ubiquitous / Discoverability
- **Statement**: The entry page shall carry one `SoftwareApplication` JSON-LD block, with `<` escaped, whose author is `https://www.naindev.com/#person`, and a `rel="author"` link to `https://www.naindev.com/`.

### REQ-TKT-013: Robots File
- **Type**: Ubiquitous / Discoverability
- **Statement**: The site root shall serve `robots.txt` that allows all crawlers, blocks nothing and advertises `https://tickets.naindev.com/sitemap.xml`.

### REQ-TKT-014: Sitemap File
- **Type**: Ubiquitous / Discoverability
- **Statement**: The site root shall serve `sitemap.xml` listing the canonical root URL, as a real file and not the `404.html` single-page fallback.

### REQ-TKT-015: LLMs File
- **Type**: Ubiquitous / Discoverability
- **Statement**: The site root shall serve `llms.txt` as Markdown whose first line is the H1 product name, followed by a one-line summary and key links.
