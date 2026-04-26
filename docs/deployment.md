# Deployment

## Local Docker Startup

1. Copy the environment file:

```bash
cp .env.example .env
```

2. Start the stack:

```bash
docker compose up --build
```

3. Open:

- Frontend: `http://localhost`
- Backend API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`

## Stack Components

- `postgres`
  - controller-owned persistence
- `backend`
  - ASP.NET Core API
  - initializes database schema on startup with `EnsureCreated`
  - seeds default registry settings and a demo preset
- `frontend`
  - static React application served by Nginx

## Real Registry Host Role

The deployment server is expected to host both:

- the NMOS Controller stack from this repository
- a real NMOS Registry service, running as a separate process or container on the same server

The controller application remains controller-only. It does not implement the real registry itself. In live deployments, the real registry service on this server is the authoritative IS-04 registration/query endpoint for NMOS nodes, senders, receivers, devices, sources, and flows.

For live operation:

- run the real registry service on the appropriate media/control network interface
- set `NMOS_CONTROLLER__REGISTRY__BASEURL` to the real registry Query API base URL
- prefer per-device IS-05 discovery from IS-04 `device.controls[].href`
- set `NMOS_CONTROLLER__REGISTRY__CONNECTIONBASEURL` only for single-endpoint IS-05 override scenarios
- set `NMOS_CONTROLLER__REGISTRY__CONNECTIONBASEURLS` (comma-separated) when fallback IS-05 gateways are needed across multiple devices
- after first startup, manage registry settings through `/api/v1/registry`; persisted DB settings are the runtime source of truth

This repository includes a Compose definition for the separate real registry service:

```bash
docker-compose -f docker-compose.live-registry.yml up -d
```

The default live registry config uses the `rhastie/nmos-cpp` container image with `RUN_NODE=FALSE`, host networking, and `docker/real-nmos-registry/registry.json`. It exposes the IS-04 Registration and Query APIs on `http://<host>:8081/x-nmos/...` and the Query WebSocket API on `8082`.

Use `.env.live.example` as the starting point for a live controller environment. If your real registry needs a different host IP or port, update both `.env.live` and `docker/real-nmos-registry/registry.json`.

## Ubuntu Notes

Install prerequisites:

```bash
sudo apt-get update
sudo apt-get install -y docker.io docker-compose-plugin
sudo systemctl enable --now docker
```

Clone the repository, then:

```bash
cp .env.example .env
docker compose up --build -d
```

Operational commands:

```bash
docker compose ps
docker compose logs -f backend
docker compose down
```

To persist data across restarts:

- keep the `postgres-data` Docker volume

To rebuild after code changes:

```bash
docker compose up --build -d
```

## Configuration

Primary environment variables:

- `NMOS_CONTROLLER__REGISTRY__BASEURL`
- `NMOS_CONTROLLER__REGISTRY__QUERYAPIVERSION`
- `NMOS_CONTROLLER__REGISTRY__CONNECTIONAPIVERSION`
- `NMOS_CONTROLLER__POSTGRES__CONNECTIONSTRING`
- `NMOS_CONTROLLER__HTTP__TIMEOUTSECONDS`
- `NMOS_CONTROLLER__CORS__ALLOWEDORIGINS`
- `VITE_API_BASE_URL`
- `NMOS_REGISTRY_IMAGE`

## Production Guidance

- Put TLS termination in front of the frontend and backend.
- Host the real NMOS Registry service on this server as a separate service from the controller.
- Point `NMOS_CONTROLLER__REGISTRY__BASEURL` at that real NMOS registry or registry gateway.
- Review CORS origins and do not leave broad development origins enabled in production.
- Replace demo seed data if operator environments require a clean initial state.

## Current Bootstrap Limitation

The backend currently uses `DatabaseInitializationHostedService` with `EnsureCreated` rather than generated EF migrations. That keeps the stack runnable with one command in this repository state, but a production hardening pass should replace that with checked-in migrations before release.
