# NmosController

Production-grade NMOS Controller for SMPTE ST 2110 environments.

This repository implements an NMOS controller that discovers, models, monitors, validates, and controls NMOS-capable registries and devices. It does not implement an NMOS Registry, Node, Sender, or Receiver.

For lab and production deployments, the controller host is expected to host a real NMOS Registry service as a separate process or container. The controller backend points at that real registry.

## Product Scope

Current implementation includes:

- NMOS IS-04 Query API integration for discovery and live topology snapshots
- NMOS IS-05 Connection API integration for route control
- Topology modeling of nodes, devices, senders, receivers, and routing destinations
- Route compatibility validation prior to connection operations
- Receiver-level and destination-level routing operations
- Detailed history/audit trail with correlation grouping and metadata details
- PostgreSQL-backed controller configuration and audit persistence
- Dockerized runtime for local/lab/Ubuntu deployment workflows

## Operator UI Scope

The React operator UI currently provides:

- `Dashboard`
  - system counts and live summary stats
  - recent History stream
- `Inventory`
  - discovered NMOS resources with drill-down
- `Senders/Receivers`
  - grouped-by-device endpoint management
  - connect/disconnect workflows
- `Soft Panel` (`/routing`)
  - two routing modes:
    - `Audio Follow Video` (default): grouped endpoint cards (V/A/ANC bundle per endpoint)
    - `Per Signal`: independent Video / Audio / Ancillary cards
  - layout switch: side-by-side or top-to-bottom
  - visual route state cues (selected/linked/success/failure)
- `History` (formerly Audit)
  - terminal-style timeline
  - grouped events by correlation ID
  - expandable event details with metadata and label resolution
- `Settings`
  - controller/registry settings management

## Quick Start

```bash
cp .env.example .env
docker compose up --build
```

Open:

- Frontend: `http://localhost`
- Backend API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

On a fresh deployment, the UI automatically redirects to a first-run setup wizard (`/setup-wizard`).

Required wizard inputs:

- Controller UI URL
- Controller API URL (optional if same-origin)
- Registry Name
- NMOS IS-04 Base URL

Defaults applied by the wizard flow:

- Registry enabled
- Query/Connection API versions remain backend defaults (`v1.3` / `v1.1`)
- IS-05 connection override fields remain `.env`-driven
- CORS defaults to allow all origins (`*`)

After completion, wizard-required settings are persisted in the controller database.

To rerun the wizard manually: open `Settings` and click `Run Setup Wizard`.

Emergency bypass (if a deployment gets stuck in wizard routing):

```js
localStorage.setItem("nmos_controller_wizard_bypass", "true");
location.href = "/settings";
```

## Live UI Dev Mode (No Rebuilds)

For frontend work with hot reload inside Docker, run compose with the dev override:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml up -d
# or: docker-compose -f docker-compose.yml -f docker-compose.dev.yml up -d
```

Open:

- Frontend (Vite dev server): `http://localhost:5173`
- Backend API: `http://localhost:8080`

Notes:

- UI file edits under `src/frontend/nmos-controller-ui` are reflected live without rebuilding images.
- Stop with:

```bash
docker compose -f docker-compose.yml -f docker-compose.dev.yml down
# or: docker-compose -f docker-compose.yml -f docker-compose.dev.yml down
```

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
|   `-- frontend
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
  - topology, routing matrix, validation, history/audit, registry, and session services
- `NmosController.Domain`
  - normalized NMOS controller model and compatibility logic
- `NmosController.Infrastructure`
  - PostgreSQL persistence
  - live NMOS HTTP clients
  - startup bootstrap and configuration
- `nmos-controller-ui`
  - React + TypeScript operator UI using React Query

## Registry Configuration

Set the backend to your real NMOS registry endpoints:

- `NMOS_CONTROLLER__REGISTRY__BASEURL=<real registry IS-04 Query API host>`
- `NMOS_CONTROLLER__REGISTRY__CONNECTIONBASEURL=<single IS-05 base URL override, optional>`
- `NMOS_CONTROLLER__REGISTRY__CONNECTIONBASEURLS=<comma-separated IS-05 fallback bases for multi-device estates, optional>`

Other commonly used runtime options are included in `.env.example` and `.env.live.example`.

## History and Event Model

The History stream includes core lifecycle and routing events such as:

- connection validation and blocking validation failures
- route request started/completed/failed
- receiver state changes (`old sender -> new sender`)
- topology refresh started/completed/failed
- registry connectivity transitions (online/offline)
- controller-side API request failures
- user session started/ended

History details are metadata-driven and resolve known resource IDs to labels in the UI when possible.

## Documentation

- Architecture: [docs/architecture.md](docs/architecture.md)
- API: [docs/api.md](docs/api.md)
- Deployment: [docs/deployment.md](docs/deployment.md)
- Interop notes: [docs/nmos-interop-notes.md](docs/nmos-interop-notes.md)

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
