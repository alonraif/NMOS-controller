# NmosController

Production-grade NMOS Controller for SMPTE ST 2110 environments.

This repository implements an NMOS controller that discovers, models, monitors, validates, and controls external NMOS-capable registries and devices. It does not implement an NMOS Registry, Node, Sender, or Receiver.

## Scope

- IS-04 Query API client behavior
- IS-05 Connection API client behavior
- Operator-facing routing matrix, topology, XY, and inspector UI
- Compatibility validation before route application
- Presets and salvos
- Audit trail
- Mock lab mode for development and demos
- Dockerized local and Ubuntu deployment workflow

## Current Status

Current implementation includes:

- backend API with controller routes
- React operator UI with shared routing state across tabbed workspaces
- PostgreSQL persistence
- mock lab mode with fixture-backed NMOS behavior and mutable route state
- logical routing destinations with video, audio, and ancillary breakaway
- topology graph data and routing matrix APIs
- Dockerfiles and `docker-compose.yml`
- deployment and interoperability documentation

## Quick Start

```bash
cp .env.example .env
docker compose up --build
```

Open:

- Frontend: `http://localhost:8088`
- Backend API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Mock NMOS fixture service: `http://localhost:8081`

The default landing workflow is the `/routing` UI, which is split into `Router`, `Topology`, `XY Panel`, and `Inspector` tabs backed by one synchronized routing state layer.

## Repository Structure

```text
.
|-- src
|   |-- backend
|   |   |-- NmosController.sln
|   |   |-- NmosController.Api
|   |   |-- NmosController.Application
|   |   |-- NmosController.Domain
|   |   |-- NmosController.Infrastructure
|   |   `-- NmosController.Contracts
|   `-- frontend
|       `-- nmos-controller-ui
|-- docker
|   |-- backend
|   |-- frontend
|   `-- mock-nmos
|-- docs
|   |-- architecture.md
|   |-- api.md
|   |-- deployment.md
|   `-- nmos-interop-notes.md
|-- docker-compose.yml
|-- .env.example
`-- README.md
```

## Architecture

- `NmosController.Api`
  - controller-facing REST API
  - Swagger, health, readiness, metrics, problem+json
- `NmosController.Application`
  - topology, routing matrix, presets, audit, registry services
- `NmosController.Domain`
  - normalized NMOS controller model and compatibility logic
- `NmosController.Infrastructure`
  - PostgreSQL persistence
  - live NMOS HTTP clients
  - mock lab adapters
  - startup bootstrap and configuration
- `nmos-controller-ui`
  - React + TypeScript operator UI using React Query

## Mock Lab Mode

Default startup mode is `Mock`.

That gives you:

- fixture-backed topology
- mutable mock connect/disconnect behavior
- seeded registry configuration
- a demo preset
- SDP assets served by the `mock-nmos` sidecar
- logical destinations and grouped sources for matrix and XY workflows
- 2022-7 A/B path metadata for graph and route inspection views

## Documentation

- Architecture: [docs/architecture.md](/Users/alonrliveu.tv/Dev/NMOS-controller/docs/architecture.md)
- API: [docs/api.md](/Users/alonrliveu.tv/Dev/NMOS-controller/docs/api.md)
- Deployment: [docs/deployment.md](/Users/alonrliveu.tv/Dev/NMOS-controller/docs/deployment.md)
- Interop notes: [docs/nmos-interop-notes.md](/Users/alonrliveu.tv/Dev/NMOS-controller/docs/nmos-interop-notes.md)

## Testing

Current test projects:

- `tests/NmosController.Domain.Tests`
- `tests/NmosController.Application.Tests`
- `tests/NmosController.Infrastructure.Tests`
- `tests/NmosController.Api.IntegrationTests`

Smoke test:

```bash
./scripts/smoke-test.sh
```

If the local machine has the .NET 8 SDK installed:

```bash
dotnet test src/backend/NmosController.sln
```

If the local machine has Node.js installed:

```bash
cd src/frontend/nmos-controller-ui
npm install
npm run build
```

## Important Note

The current Docker bootstrap uses `EnsureCreated` on startup so the stack can come up with one command in this repository state. A production hardening pass should replace that with checked-in EF Core migrations before release.
