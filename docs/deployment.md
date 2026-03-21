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

- Frontend: `http://localhost:8088`
- Backend API: `http://localhost:8080`
- Swagger: `http://localhost:8080/swagger`
- Mock NMOS fixture service: `http://localhost:8081`

## Stack Components

- `postgres`
  - controller-owned persistence
- `backend`
  - ASP.NET Core API
  - initializes database schema on startup with `EnsureCreated`
  - seeds default registry settings and a demo preset
- `frontend`
  - static React application served by Nginx
- `mock-nmos`
  - serves SDP fixture assets used by the mock lab workflow

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

- `NMOS_CONTROLLER__MODE`
- `NMOS_CONTROLLER__REGISTRY__BASEURL`
- `NMOS_CONTROLLER__REGISTRY__QUERYAPIVERSION`
- `NMOS_CONTROLLER__REGISTRY__CONNECTIONAPIVERSION`
- `NMOS_CONTROLLER__POSTGRES__CONNECTIONSTRING`
- `NMOS_CONTROLLER__HTTP__TIMEOUTSECONDS`
- `NMOS_CONTROLLER__MOCKLAB__FIXTUREPATH`
- `NMOS_CONTROLLER__CORS__ALLOWEDORIGINS`
- `VITE_API_BASE_URL`

## Production Guidance

- Put TLS termination in front of the frontend and backend.
- Set `NMOS_CONTROLLER__MODE=Live` for real registries.
- Point `NMOS_CONTROLLER__REGISTRY__BASEURL` at the real NMOS registry or gateway.
- Review CORS origins and do not leave broad development origins enabled in production.
- Replace demo seed data if operator environments require a clean initial state.

## Current Bootstrap Limitation

The backend currently uses `DatabaseInitializationHostedService` with `EnsureCreated` rather than generated EF migrations. That keeps the stack runnable with one command in this repository state, but a production hardening pass should replace that with checked-in migrations before release.
