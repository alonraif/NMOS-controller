# Real NMOS Registry Container

This directory contains the runtime configuration for the live NMOS Registry service.

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
