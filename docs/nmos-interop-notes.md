# NMOS Interop Notes

## Current Assumptions

- Registry discovery starts from a configured base URL rather than DNS-SD.
- The controller models live topology from IS-04 Query responses and does not treat PostgreSQL as authority for NMOS resource state.
- The current compatibility evaluator checks obvious transport, format, media type, and transport-file mismatches.
- IS-05 operations target single receiver staged endpoints.
- Scheduled activation is represented internally, but the initial UI focuses on immediate activation.

## Mock Lab Mode

Mock lab mode is the default bootstrap path.

It consists of:

- internal fixture-backed query and connection adapters
- mutable in-memory connection state built from `topology-snapshot.json`
- seeded registry settings set to `Mock`
- a small sidecar service that serves SDP manifests referenced by the fixtures

This means the UI and API can be demonstrated without a real registry or device estate.

## Known Interoperability Gaps

- DNS-SD registry discovery is not yet implemented.
- Multi-registry coordination is not yet implemented.
- IS-08, IS-10, IS-11, and BCP-003 are not yet implemented.
- 2022-7 path-awareness is not modeled yet.
- Constraint handling is intentionally conservative and not exhaustive across vendor-specific payloads.
- The live IS-05 client currently assumes receiver `single` endpoints are reachable from the configured base URL.

## Broadcast Engineering Considerations

- Some NMOS deployments expose registry and connection APIs through different gateways. The current configuration model uses one base URL and version pair; split endpoint configuration is a likely next production step.
- SDP and transport parameter richness varies between vendors. The controller preserves transport files and exposes active/staged state to help operators inspect vendor-specific details rather than flattening them away.
- The compatibility layer should expand with vendor capture data once interoperability testing begins against real estates.
