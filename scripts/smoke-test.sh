#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

ENV_FILE="${1:-.env.example}"

echo "Using env file: ${ENV_FILE}"
echo "Starting stack..."
docker compose --env-file "${ENV_FILE}" up --build -d

cleanup() {
  echo "Stopping stack..."
  docker compose --env-file "${ENV_FILE}" down
}

trap cleanup EXIT

wait_for_url() {
  local name="$1"
  local url="$2"
  local attempts="${3:-40}"

  for ((i=1; i<=attempts; i++)); do
    if curl -fsS "$url" >/dev/null 2>&1; then
      echo "${name} is ready: ${url}"
      return 0
    fi

    sleep 3
  done

  echo "Timed out waiting for ${name}: ${url}" >&2
  return 1
}

wait_for_url "backend health" "http://localhost:8080/health"
wait_for_url "backend readiness" "http://localhost:8080/ready"
wait_for_url "frontend" "http://localhost:8088"
wait_for_url "mock-nmos" "http://localhost:8081/health.json"

echo "Checking controller API endpoints..."
curl -fsS http://localhost:8080/api/v1/registry | head -c 400 && echo
curl -fsS http://localhost:8080/api/v1/topology | head -c 400 && echo
curl -fsS http://localhost:8080/api/v1/presets | head -c 400 && echo
curl -fsS http://localhost:8080/api/v1/audit | head -c 400 && echo

echo "Smoke test completed successfully."
