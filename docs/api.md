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

`GET /topology` now also returns:

- `routingDestinations`: logical broadcast destinations that can fan into separate video, audio, and ancillary NMOS receivers
- `routeEdges`: active, staged, and preview-capable route edges with `path` `A|B`, `layer`, `redundancyGroup`, and metadata used by the topology graph

### Routing

- `GET /routing/matrix?refresh=false`
- `POST /routing/validate`
- `POST /routing/connect`
- `POST /routing/disconnect`
- `POST /routing/receivers/{receiverId}/connect`
- `POST /routing/receivers/{receiverId}/disconnect`

Matrix response shape:

```json
{
  "sources": [],
  "destinations": [],
  "crosspoints": [],
  "refreshedAtUtc": "2026-03-21T12:00:00Z"
}
```

Validation request:

```json
{
  "senderId": "sender-audio-program-b",
  "receiverId": "receiver-dest-audio-room-audio",
  "activationMode": "Immediate"
}
```

Broadcast routing connect request:

```json
{
  "destinationId": "dest-studio-a",
  "requestedBy": "operator",
  "videoSourceId": "src-video-cam1",
  "audioSourceId": "src-audio-program",
  "ancillarySourceId": "src-anc-cam1",
  "activationMode": "Immediate"
}
```

Broadcast routing disconnect request:

```json
{
  "destinationId": "dest-studio-a",
  "requestedBy": "operator",
  "disconnectVideo": true,
  "disconnectAudio": false,
  "disconnectAncillary": false,
  "activationMode": "Immediate"
}
```

Connect request:

```json
{
  "senderId": "sender-audio-program-b",
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
      "receiverId": "receiver-dest-audio-room-audio",
      "senderId": "sender-audio-program-b",
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

- The `/routing` page is now tabbed into focused workspaces: `Router`, `Topology`, `XY Panel`, and `Inspector`.
- All tabs share one routing state layer, so preview selections, current destination focus, and executed routes remain synchronized while operators switch views.
- The `/routing` UI uses a preview/take workflow. Preview state is frontend-managed in v1 and `TAKE` submits immediate IS-05 style activation.
- Breakaway routing is modeled by separate `videoSourceId`, `audioSourceId`, and `ancillarySourceId` fields on `/routing/connect`.
- 2022-7 awareness is modeled through grouped senders and graph edges that expose `path` `A|B` plus redundancy health badges like `A/B OK`, `A only`, `B only`, and `No signal`.
- In `Mock` mode, the controller uses its fixture-backed adapters rather than live IS-04 or IS-05 calls.
- Enum values are serialized as strings so the UI and API remain explicit and stable.
