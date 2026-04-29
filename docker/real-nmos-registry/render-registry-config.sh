#!/bin/sh
set -eu

HOST_ADDRESS="${NMOS_REGISTRY_HOST_ADDRESS:-}"
HTTP_PORT="${NMOS_REGISTRY_HTTP_PORT:-8081}"
QUERY_WS_PORT="${NMOS_REGISTRY_QUERY_WS_PORT:-8082}"
REG_EXPIRY="${NMOS_REGISTRY_REGISTRATION_EXPIRY_INTERVAL:-30}"
PRIORITY="${NMOS_REGISTRY_PRIORITY:-99}"
LOG_LEVEL="${NMOS_REGISTRY_LOGGING_LEVEL:--20}"
HTTP_TRACE="${NMOS_REGISTRY_HTTP_TRACE:-false}"
ALLOW_INVALID="${NMOS_REGISTRY_ALLOW_INVALID_RESOURCES:-false}"
LABEL="${NMOS_REGISTRY_LABEL:-nmos-controller-registry}"

if [ -z "$HOST_ADDRESS" ]; then
  echo "NMOS_REGISTRY_HOST_ADDRESS is required and must be set in .env" >&2
  exit 1
fi

cat > /home/registry.json <<EOF
{
  "pri": ${PRIORITY},
  "logging_level": ${LOG_LEVEL},
  "http_trace": ${HTTP_TRACE},
  "allow_invalid_resources": ${ALLOW_INVALID},
  "label": "${LABEL}",
  "host_address": "${HOST_ADDRESS}",
  "http_port": ${HTTP_PORT},
  "query_ws_port": ${QUERY_WS_PORT},
  "registration_expiry_interval": ${REG_EXPIRY}
}
EOF

exec /home/entrypoint.sh
