# Traceability Matrix — HaveTickets

This matrix traces each functional, domain invariant, and architectural requirement to its implementing components and automated test verifications.

| Requirement ID | Summary | Implementing Component | Test Verification | Status |
| :--- | :--- | :--- | :--- | :--- |
| **REQ-TKT-001** | Zero overbooking under concurrent purchases | `TicketConfiguration.cs` (Partial Unique Index), `TicketRepository.cs` | `ReservationConcurrencyTests.cs` (Api.Tests) | Verified |
| **REQ-TKT-002** | Automatic seat re-circulation upon cancellation | `Ticket.cs` (`Cancel()` method), `TicketConfiguration.cs` | `TicketLifecycleTests.cs` (Domain.Tests) | Verified |
| **REQ-TKT-003** | Exact integer minor-unit monetary math | `Money.cs` Value Object | `MoneyPropertyTests.cs` (CsCheck Property-Based Tests) | Verified |
| **REQ-TKT-004** | Door check-in time window `[-30m, +30m]` | `CheckInTicketHandler.cs`, `Showtime.cs` | `DoorCheckInTimeWindowTests.cs` (Api.Tests) | Verified |
| **REQ-TKT-005** | Idempotent door redemption | `Ticket.cs` (`CheckIn()` state guard) | `TicketCheckInIdempotencyTests.cs` (Domain.Tests) | Verified |
| **REQ-TKT-006** | Non-crashing inspection response protocol | `CheckInResponse.cs`, `CheckInEndpoint.cs` | `CheckInEndpointTests.cs` (Api.Tests) | Verified |
| **REQ-TKT-007** | Dual client support (Blazor WASM + Vue 3) | `HaveTickets.WebUI` & `HaveTickets.Client` | `npm run build` (Client), `dotnet build` (WebUI) | Verified |
| **REQ-TKT-008** | Time virtualization via `TimeProvider` | `MutableTimeProvider`, DI registration in `Program.cs` | `TimeProviderIntegrationTests.cs` (Api.Tests) | Verified |
| **REQ-TKT-009** | Bilingual i18n & ecosystem event sync | `src/locales/i18n.ts`, `App.vue` | `vue-tsc -b` & Vite build | Verified |
| **REQ-TKT-010** | Canonical URL and description | `HaveTickets.WebUI/wwwroot/index.html` | `SeoBaselineTests.cs` (`Canonical_is_the_host_root`, `Description_is_single_and_within_snippet_length`) | Verified |
| **REQ-TKT-011** | Social cards | `HaveTickets.WebUI/wwwroot/index.html` | `SeoBaselineTests.cs` (`Open_graph_matches_the_canonical`, `Twitter_card_is_complete`) | Verified |
| **REQ-TKT-012** | Structured data and author link | `HaveTickets.WebUI/wwwroot/index.html` | `SeoBaselineTests.cs` (`Json_ld_parses_and_names_the_author`, `Author_link_points_to_the_main_site`) | Verified |
| **REQ-TKT-013** | Robots file | `HaveTickets.WebUI/wwwroot/robots.txt` | `SeoBaselineTests.cs` (`Robots_advertises_the_sitemap_and_blocks_nothing`) | Verified |
| **REQ-TKT-014** | Sitemap file | `HaveTickets.WebUI/wwwroot/sitemap.xml` | `SeoBaselineTests.cs` (`Sitemap_lists_the_canonical_root`) | Verified |
| **REQ-TKT-015** | LLMs file | `HaveTickets.WebUI/wwwroot/llms.txt` | `SeoBaselineTests.cs` (`Llms_txt_starts_with_a_heading`) | Verified |
