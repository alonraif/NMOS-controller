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

## Prerequisites

- Docker Engine 24+ (or current stable)
- Docker Compose v2 plugin (`docker compose ...`)
- Linux host with open ports required by your deployment

Verify Compose v2 is installed:

```bash
docker compose version
```

If your host only has legacy `docker-compose` v1, install the Compose v2 plugin before deploying. Compose v1 can fail on newer Docker engines with errors like `KeyError: 'ContainerConfig'`.

## Quick Start (Local/Lab)

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

## Production Deployment

1. Prepare environment file:

```bash
cp .env.example .env
```

2. Edit `.env` for your real environment (registry URLs, CORS, host/IP bindings, ports).

3. Build and start:

```bash
docker compose up -d --build
```

4. Verify services:

```bash
docker compose ps
docker compose logs --since=10m
```

5. Verify NMOS registry reachability from the controller host:

```bash
curl -sS -o /dev/null -w "HTTP %{http_code}\n" \
  http://127.0.0.1:8081/x-nmos/query/v1.3/nodes
```

Expected result: `HTTP 200`.

6. Stop stack when needed:

```bash
docker compose down
```

If you run into old-container conflicts during upgrades, clean and recreate:

```bash
docker compose down --remove-orphans
docker rm -f nmos-controller-real-registry nmos-controller-postgres nmos-controller-backend nmos-controller-frontend 2>/dev/null || true
docker compose up -d --build
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

## Configuration Reference

Set these in `.env` for deployment:

- `BACKEND_PORT`: backend listen port (default `8080`)
- `FRONTEND_PORT`: frontend service port (default `80`)
- `FRONTEND_BIND_IP`: bind address for frontend process (default `127.0.0.1`)
- `NMOS_CONTROLLER__POSTGRES__CONNECTIONSTRING`: PostgreSQL connection string used by backend
- `NMOS_CONTROLLER__REGISTRY__NAME`: display name for configured registry
- `NMOS_CONTROLLER__REGISTRY__BASEURL`: IS-04 Query API base URL (required for real deployments)
- `NMOS_CONTROLLER__REGISTRY__CONNECTIONBASEURL`: optional single IS-05 Connection API override
- `NMOS_CONTROLLER__REGISTRY__CONNECTIONBASEURLS`: optional comma-separated IS-05 fallback URLs
- `NMOS_CONTROLLER__CORS__ALLOWEDORIGINS`: comma-separated allowed UI origins

For built-in lab registry container settings (when using `real-nmos-registry` service):

- `NMOS_REGISTRY_HOST_ADDRESS`
- `NMOS_REGISTRY_HTTP_PORT`
- `NMOS_REGISTRY_QUERY_WS_PORT`
- `NMOS_REGISTRY_LABEL`

Templates:

- `.env.example`: baseline defaults
- `.env.live.example`: live deployment-oriented defaults

After first start, complete the setup wizard in the UI to persist controller settings in PostgreSQL.

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

## Versioning and Releases

- Project release versioning uses SemVer tags (`vMAJOR.MINOR.PATCH`).
- This release baseline is `v1.0.0`.
- Backend runtime version is exposed at:
  - `GET /version`
  - `GET /health`
  - `GET /ready`
  - `GET /`

Create and push the `v1.0.0` tag:

```bash
git tag -a v1.0.0 -m "NmosController v1.0.0"
git push origin v1.0.0
```

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
