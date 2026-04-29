# Real NMOS Registry Container

This directory contains runtime configuration helpers for the live NMOS Registry service.

The Compose service uses the `rhastie/nmos-cpp` image, which packages the Sony `nmos-cpp` implementation. It runs in registry/controller mode with `RUN_NODE=FALSE`.

Default endpoints:

- IS-04 Registration API: `http://<host>:8081/x-nmos/registration/v1.3/`
- IS-04 Query API: `http://<host>:8081/x-nmos/query/v1.3/`
- Query WebSocket API: `ws://<host>:8082/`
- nmos-cpp admin UI, if enabled by the image: `http://<host>:8081/admin`

The live registry listens on host port `8081` and can be run with the compose file below.

```bash
docker-compose -f docker-compose.live-registry.yml up -d
```

Then point the controller at the registry:

```env
NMOS_CONTROLLER__REGISTRY__BASEURL=http://192.168.170.2:8081
```

The registry bind/advertised address is driven by `.env`:

```env
NMOS_REGISTRY_HOST_ADDRESS=192.168.170.2
NMOS_REGISTRY_HTTP_PORT=8081
NMOS_REGISTRY_QUERY_WS_PORT=8082
NMOS_REGISTRY_REGISTRATION_EXPIRY_INTERVAL=30
```

`docker/real-nmos-registry/render-registry-config.sh` renders `/home/registry.json`
inside the container at startup, then executes the image entrypoint.
