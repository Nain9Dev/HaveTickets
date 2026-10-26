<picture>
  <source media="(prefers-color-scheme: dark)" srcset="docs/assets/banner-dark.svg">
  <source media="(prefers-color-scheme: light)" srcset="docs/assets/banner-light.svg">
  <img alt="HaveTickets Architectural Blueprint" src="docs/assets/banner-dark.svg" width="100%">
</picture>

# HaveTickets — Plataforma de Venta de Entradas y Reserva de Cines

[![English](https://img.shields.io/badge/Language-English-blue?style=flat-square)](README.md)
[![Español](https://img.shields.io/badge/Idioma-Espa%C3%B1ol-emerald?style=flat-square)](README.es.md)
[![ci](https://github.com/Nain9Dev/HaveTickets/actions/workflows/ci.yml/badge.svg)](https://github.com/Nain9Dev/HaveTickets/actions/workflows/ci.yml)
[![CodeQL](https://github.com/Nain9Dev/HaveTickets/actions/workflows/codeql.yml/badge.svg)](https://github.com/Nain9Dev/HaveTickets/actions/workflows/codeql.yml)
[![Demo Online](https://img.shields.io/badge/demo-online-blueviolet?style=flat&logo=githubpages&logoColor=white)](https://tickets.naindev.com/)
[![Ecosystem Status](https://img.shields.io/badge/status-100%25%20operational-success)](https://status.naindev.com)
[![Engineering Handbook](https://img.shields.io/badge/paved%20road-.NET%2010%20%2B%20Blazor-blue)](https://github.com/Nain9Dev/engineering-handbook)
[![Cost](https://img.shields.io/badge/cost-0.00_%E2%82%AC-22c55e)](https://github.com/Nain9Dev/engineering-handbook/blob/main/governance/zero-cost-philosophy.md)
[![Architecture Tests](https://img.shields.io/badge/architecture%20tests-NetArchTest%20enforced-blueviolet)](HaveTickets.Api.Tests/ArchitectureTests.cs)
[![Open in GitHub Codespaces](https://img.shields.io/badge/Open%20in-Codespaces-1f2937?style=flat&logo=github&logoColor=white)](https://codespaces.new/Nain9Dev/HaveTickets)
[![Tests](https://img.shields.io/badge/tests-120%20passing-success)](HaveTickets.Domain.Tests/)
[![.NET 10](https://img.shields.io/badge/.NET%2010-Blazor%20WASM-blue?style=flat-square)](https://tickets.naindev.com/)

**Demo Online Interactiva:** [https://tickets.naindev.com/](https://tickets.naindev.com/) (Blazor WebAssembly en cliente con motor determinista de asignación de butacas, coste 0,00 €)

Una plataforma determinista de venta de entradas y reserva de butacas de alta concurrencia construida con .NET 10, Clean Architecture y doble frontend (Blazor WebAssembly y Vue 3 + TypeScript).

Forma parte del portafolio técnico de **NainDev**.

---

## Características

- **Asignación Determinista de Butacas**: Evita condiciones de carrera y sobreventa en la capa de datos mediante índices únicos parciales (`UX_Tickets_ShowtimeId_Seat_Active`).
- **Contabilidad Exacta en Unidades Menores**: Elimina imprecisiones de coma flotante y truncado de céntimos utilizando aritmética entera de unidades menores (`Money`).
- **Ciclo de Vida Unidireccional de Entradas**: Progresión de estado estricta (`Issued` -> `CheckedIn` | `Cancelled`) con liberación automática de butaca ante cancelación.
- **Validación de Acceso en Torno**: Ventana de validación idempotente [-30m, +30m] con resultados de admisión tipados.
- **Generación Dinámica de Códigos QR**: Endpoint de renderizado PNG para códigos de confirmación con codificación determinista de bytes.
- **Hardening de API**: Rate limiting de ventana fija particionado por IP, lista blanca CORS, `ProblemDetails` RFC 7807 y documentación Scalar OpenAPI.
- **Frontend Dual**:
  - **Vue 3 + TypeScript**: Mapa interactivo de selección de butacas, dark glassmorphism, cartera de entradas y consola de escaneo de accesos.
  - **Blazor WebAssembly**: Cliente consumidor .NET 10.

---

## Arquitectura y Diagrama C4 de Contenedores

```mermaid
graph TD
    Blazor["HaveTickets.WebUI<br/>(Blazor WebAssembly / .NET 10)"]
    VueClient["HaveTickets.Client<br/>(Vue 3 + TypeScript / Vite)"]
    API["HaveTickets.Api<br/>(ASP.NET Core Minimal APIs :5005)"]
    Contracts["HaveTickets.Contracts<br/>(Contratos DTO Independientes)"]
    AppLayer["HaveTickets.Application<br/>(CQRS / Handlers / Servicios)"]
    DomainLayer["HaveTickets.Domain<br/>(Raíces Agregadas / Objetos de Valor / Money)"]
    InfraLayer["HaveTickets.Infrastructure<br/>(EF Core / SQLite / QRCoder)"]
    DB[("Base de Datos SQLite<br/>(Índice Único Parcial UX_ActiveSeat)")]

    Blazor -->|HTTP / JSON| API
    VueClient -->|HTTP / JSON| API
    API --> Contracts
    API --> AppLayer
    AppLayer --> DomainLayer
    AppLayer --> InfraLayer
    InfraLayer --> DB

    style DomainLayer fill:#182B49,stroke:#fff,stroke-width:2px,color:#fff
    style AppLayer fill:#2A4878,stroke:#fff,color:#fff
    style InfraLayer fill:#2A4878,stroke:#fff,color:#fff
    style API fill:#512BD4,stroke:#fff,stroke-width:2px,color:#fff
    style Blazor fill:#10B981,stroke:#fff,stroke-width:2px,color:#fff
    style VueClient fill:#0284C7,stroke:#fff,stroke-width:2px,color:#fff
    style DB fill:#336791,stroke:#fff,color:#fff
```

### Proyectos de la Solución

```
HaveTickets.slnx
├── HaveTickets.Contracts          # Contratos DTO independientes (V1)
├── HaveTickets.Domain             # Dominio empresarial puro: entidades, agregados, reglas
├── HaveTickets.Application        # Casos de uso CQRS: handlers, queries, comandos
├── HaveTickets.Infrastructure     # EF Core, persistencia SQLite, modelos de lectura, QR
├── HaveTickets.Api                # Minimal APIs, Scalar OpenAPI, rate limiting
├── HaveTickets.Domain.Tests       # xUnit v3 + Shouldly + CsCheck (Property-Based Testing)
├── HaveTickets.Api.Tests          # Suite de integración en memoria + guardarraíles NetArchTest
├── HaveTickets.WebUI              # Cliente Blazor WebAssembly
└── HaveTickets.Client             # Cliente Vue 3 + TypeScript con Vite
```

---

## Desarrollo Local y Puertos

Puertos estrictamente definidos en `D:/Development/Tools/dev-ports.json`:

| Servicio | Puerto | Comando de Inicio |
| :--- | :--- | :--- |
| **API** | `8190` | `$env:ASPNETCORE_URLS='http://localhost:8190'; dotnet run --project HaveTickets.Api --no-launch-profile` |
| **Cliente Vue 3** | `5391` | `npm --prefix HaveTickets.Client run dev` |
| **Blazor WebUI** | `5390` | `$env:ASPNETCORE_URLS='http://localhost:5390'; dotnet run --project HaveTickets.WebUI --no-launch-profile` |

La documentación de la API se sirve en: `http://localhost:8190/docs` (Scalar UI) y `http://localhost:8190/openapi/v1.json`.

---

## Pruebas

```powershell
# Compilar la solución bajo la regla estricta de cero advertencias
dotnet build HaveTickets.slnx -warnaserror

# Ejecutar pruebas unitarias de dominio y basadas en propiedades (74 pruebas)
dotnet test HaveTickets.Domain.Tests/HaveTickets.Domain.Tests.csproj --no-build

# Ejecutar pruebas de integración de API (42 pruebas)
dotnet test HaveTickets.Api.Tests/HaveTickets.Api.Tests.csproj --no-build

# Compilar y validar el cliente Vue 3 + TypeScript
npm --prefix HaveTickets.Client run build
```

---

## FinOps y Arquitectura de Coste Cero (0,00 €)

En consonancia con la [Filosofía de Infraestructura de Coste Cero de NainDev](https://github.com/Nain9Dev/engineering-handbook/blob/main/governance/zero-cost-philosophy.md), este sistema opera con un coste recurrente en la nube estrictamente de **0,00 €/mes**.

---

## Licencia

MIT — ver [`LICENSE`](LICENSE).
