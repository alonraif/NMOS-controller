# NmosController Architecture

## Objective

NmosController is a controller-only platform for NMOS-managed broadcast IP environments. It integrates with external NMOS registries and device connection APIs to provide discovery, normalized topology, validation, routing control, presets, auditability, and mock lab simulation.

The system boundary is explicit:

- In scope: NMOS discovery, controller state modeling, validation, connection control, operator UI, presets, audit, observability, persistence of controller-owned data.
- Out of scope: implementing an NMOS Registry, Node, Sender, Receiver, or media-plane transport behavior.

## Architecture Summary

The repository will use a modular monolith with clean architecture boundaries.

### Backend

- `NmosController.Api`
  - ASP.NET Core Web API entrypoint
  - REST endpoints for the operator UI
  - OpenAPI/Swagger
  - health, readiness, metrics
  - problem+json error responses
  - correlation ID middleware
- `NmosController.Application`
  - use cases and orchestration
  - topology query services
  - validation services
  - connection workflows
  - preset and audit services
  - DTOs for internal API responses
  - interfaces for persistence and external integrations
- `NmosController.Domain`
  - core entities and value objects
  - routing compatibility rules
  - connection request semantics
  - audit and alarm models
  - domain invariants
- `NmosController.Infrastructure`
  - EF Core persistence
  - PostgreSQL implementation
  - outbound NMOS HTTP clients
  - mock lab adapters
  - Serilog configuration support
  - repository implementations
  - mapping from NMOS DTOs to domain models
- `NmosController.Contracts`
  - shared contracts for API payloads
  - request/response models used across API and tests

### Frontend

- React + TypeScript + Vite
- React Query for server state
- typed API client generated or hand-maintained from controller API contracts
- operator-focused dark UI
- topology, routing, registry status, presets, audit, settings

### Persistence Model

PostgreSQL persists controller-owned state only:

- registry settings
- presets and salvos
- audit log entries
- user sessions
- alarm events
- optional cached topology snapshots for diagnostics or faster UI warm-up

Live NMOS topology remains external truth and is refreshed from NMOS APIs.

### Observability

- Serilog structured JSON logs
- correlation IDs propagated through request scope and outbound NMOS calls
- Prometheus-compatible `/metrics`
- `/health` and `/ready`
- audit log stream for operator actions

### Mock Lab Mode

Mock mode replaces live NMOS integration with fixture-backed query and connection adapters while preserving the same application service interfaces. This keeps the backend and frontend behavior consistent between demo and live environments.

## Runtime Component Model

```text
+---------------------------+        +---------------------------+
| React Operator UI         |        | External NMOS Registry    |
| nmos-controller-ui        |<------>| IS-04 Query API           |
+-------------+-------------+        +---------------------------+
              |
              v
+-------------+-------------+        +---------------------------+
| ASP.NET Core API          |<------>| External NMOS Devices     |
| NmosController.Api        |        | IS-05 Connection API      |
+-------------+-------------+        +---------------------------+
              |
      +-------+--------+
      |                |
      v                v
+-----+------+   +-----+----------------+
| Application |   | Infrastructure      |
| Use Cases   |   | EF Core + NMOS      |
+-----+------+   | Clients + Mock Adpt. |
      |          +-----+----------------+
      v                |
+-----+----------------+-----+
| Domain Model               |
+---------------------------+
              |
              v
+---------------------------+
| PostgreSQL                |
| Controller-owned state    |
+---------------------------+
```

## Proposed Solution Tree

```text
.
|-- src
|   |-- backend
|   |   |-- NmosController.sln
|   |   |-- Directory.Build.props
|   |   |-- Directory.Packages.props
|   |   |-- NmosController.Api
|   |   |   |-- Controllers
|   |   |   |-- Endpoints
|   |   |   |-- Middleware
|   |   |   |-- Configuration
|   |   |   |-- Extensions
|   |   |   |-- Program.cs
|   |   |   `-- appsettings*.json
|   |   |-- NmosController.Application
|   |   |   |-- Abstractions
|   |   |   |-- Topology
|   |   |   |-- Routing
|   |   |   |-- Presets
|   |   |   |-- Audit
|   |   |   |-- Settings
|   |   |   `-- Common
|   |   |-- NmosController.Domain
|   |   |   |-- Entities
|   |   |   |-- ValueObjects
|   |   |   |-- Enums
|   |   |   |-- Services
|   |   |   `-- Rules
|   |   |-- NmosController.Infrastructure
|   |   |   |-- Persistence
|   |   |   |   |-- Configurations
|   |   |   |   |-- Migrations
|   |   |   |   `-- Repositories
|   |   |   |-- Nmos
|   |   |   |   |-- Dtos
|   |   |   |   |-- Clients
|   |   |   |   |-- Mapping
|   |   |   |   `-- Mock
|   |   |   |-- Observability
|   |   |   `-- Configuration
|   |   `-- NmosController.Contracts
|   |       |-- Requests
|   |       `-- Responses
|   `-- frontend
|       `-- nmos-controller-ui
|           |-- src
|           |   |-- app
|           |   |-- api
|           |   |-- components
|           |   |-- features
|           |   |-- hooks
|           |   |-- pages
|           |   |-- routes
|           |   |-- styles
|           |   `-- types
|           |-- public
|           `-- package.json
|-- tests
|   |-- NmosController.Domain.Tests
|   |-- NmosController.Application.Tests
|   |-- NmosController.Infrastructure.Tests
|   `-- NmosController.Api.IntegrationTests
|-- docker
|   |-- backend
|   |   `-- Dockerfile
|   |-- frontend
|   |   `-- Dockerfile
|   `-- mock-nmos
|       |-- fixtures
|       `-- Dockerfile
|-- docs
|   |-- architecture.md
|   |-- api.md
|   |-- deployment.md
|   `-- nmos-interop-notes.md
|-- scripts
|   `-- smoke-test.sh
|-- docker-compose.yml
|-- .env.example
`-- README.md
```

## Major Design Choices And Rationale

### 1. Modular monolith instead of microservices

This controller has clear domain boundaries but does not yet justify distributed operational overhead. A modular monolith keeps deployment simple for Ubuntu and Docker, preserves strong internal boundaries, and leaves room to split services later if scale or organizational needs require it.

### 2. Controller-owned persistence only

The database will not be treated as authoritative for live topology. That avoids stale state becoming the operational truth and keeps the controller aligned with NMOS architecture, where the registry and device APIs remain source systems for live resource state.

### 3. Dedicated NMOS integration layer

IS-04 and IS-05 payloads are verbose and versioned. Keeping raw DTOs and HTTP clients isolated in infrastructure prevents NMOS schema details from leaking into the domain model and makes version-aware handling and mock replacement straightforward.

### 4. Normalized internal topology model

The UI and routing workflows should not depend on raw NMOS payload shapes. The application layer will construct a normalized graph of nodes, devices, sources, flows, senders, and receivers with explicit relationships and summarized compatibility attributes for fast operator workflows.

### 5. Validation before connect

Connection operations will run through a validation service that checks transport compatibility, format assumptions, receiver constraints, sender capabilities, and obvious missing prerequisites before any IS-05 call is issued. This is critical for operator trust and safer routing behavior.

### 6. Immediate support with scheduled activation readiness

The first UI will expose immediate activation, but the domain and API contracts will include activation models that can carry scheduled activations later. This avoids repainting the core connection flow when deferred activation is added.

### 7. Mock lab mode behind the same interfaces

A fixture-backed mock integration allows the full stack to run without external NMOS systems. Using the same application interfaces for live and mock mode keeps the code path realistic and makes integration tests and demos reliable.

### 8. Operator API instead of direct frontend-to-NMOS access

The frontend will call only the controller API. This centralizes validation, audit logging, retry logic, credential handling, and future authorization. It also prevents browser-origin issues and raw NMOS payload complexity from leaking into the UI.

### 9. PostgreSQL as primary persistence store

PostgreSQL is sufficient for controller-owned relational data and works well with EF Core, Docker, and Ubuntu deployment. Redis is intentionally deferred unless event fan-out, distributed locking, or cache pressure becomes a demonstrated need.

### 10. Observability from the first implementation

Broadcast control tooling needs diagnosability during interoperability issues. Structured logs, correlation IDs, outbound request logging, health/readiness probes, metrics, and auditable user actions will be built in from the beginning rather than bolted on later.

## Phase 2 Build Plan

Phase 2 will create:

- .NET solution and projects
- package/version management
- backend startup wiring
- initial contracts and configuration objects
- frontend Vite app scaffold
- repository-wide ignore files and environment templates

That establishes a compileable skeleton before domain and integration details are added.
