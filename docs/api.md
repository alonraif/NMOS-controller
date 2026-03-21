# API Surface

## Overview

The frontend communicates only with the controller API. It does not call raw NMOS IS-04 or IS-05 endpoints directly.

Base path:

- `http://localhost:8080/api/v1`

Envelope format:

- Most successful responses return:

```json
{
  "data": {},
  "utc": "2026-03-21T12:00:00Z"
}
```

Errors:

- Validation and unexpected failures return RFC `application/problem+json`
- `traceId` is included for correlation with structured logs

## Endpoints

### Registry

- `GET /registry`
- `PUT /registry`

Request body for `PUT /registry`:

```json
{
  "name": "Local Mock Registry",
  "baseUrl": "http://mock-nmos:8081",
  "queryApiVersion": "v1.3",
  "connectionApiVersion": "v1.1",
  "mode": "Mock",
  "isEnabled": true
}
```

### Topology

- `GET /topology?refresh=false`
- `GET /senders?refresh=false`
- `GET /receivers?refresh=false`
- `GET /resources/{resourceId}`

### Routing

- `POST /routing/validate`
- `POST /routing/receivers/{receiverId}/connect`
- `POST /routing/receivers/{receiverId}/disconnect`

Validation request:

```json
{
  "senderId": "sender-audio-a",
  "receiverId": "receiver-audio-b",
  "activationMode": "Immediate"
}
```

Connect request:

```json
{
  "senderId": "sender-audio-a",
  "requestedBy": "operator",
  "activationMode": "Immediate"
}
```

Disconnect request:

```json
{
  "requestedBy": "operator",
  "activationMode": "Immediate"
}
```

### Presets / Salvos

- `GET /presets`
- `GET /presets/{id}`
- `POST /presets`
- `DELETE /presets/{id}`
- `POST /presets/{id}/execute`

Create or update preset:

```json
{
  "name": "Demo Audio Route",
  "description": "Route audio to multiviewer",
  "routes": [
    {
      "receiverId": "receiver-audio-b",
      "senderId": "sender-audio-a",
      "activationMode": "Immediate"
    }
  ]
}
```

### Audit

- `GET /audit?limit=100`

### Platform

- `GET /health`
- `GET /ready`
- `GET /metrics`
- `GET /`

## Notes

- Scheduled activation is part of the request model, but the UI currently drives immediate activation first.
- In `Mock` mode, the controller uses its fixture-backed adapters rather than live IS-04 or IS-05 calls.
- Enum values are serialized as strings so the UI and API remain explicit and stable.
