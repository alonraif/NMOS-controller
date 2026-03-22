import copy
import json
import os
import re
import sys
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import Lock
from urllib.parse import urlparse


ROOT = Path(__file__).resolve().parent
PUBLIC_DIR = ROOT / "public"
FIXTURE_PATH = ROOT / "topology-snapshot.json"
HOST = os.getenv("MOCK_NMOS_HOST", "0.0.0.0")
PORT = int(os.getenv("MOCK_NMOS_PORT", "80"))
INTERNAL_BASE_URL = os.getenv("MOCK_NMOS_INTERNAL_BASE_URL", "http://mock-nmos:80")


def parse_rate(value):
    if not value:
        return None

    parts = str(value).split("/", 1)
    numerator = int(parts[0])
    denominator = int(parts[1]) if len(parts) > 1 and parts[1] else 1
    return {"numerator": numerator, "denominator": denominator}


class MockRegistryState:
    def __init__(self, fixture_path: Path):
        snapshot = json.loads(fixture_path.read_text())
        self._lock = Lock()
        self.nodes = snapshot["nodes"]
        self.devices = snapshot["devices"]
        self.sources = snapshot["sources"]
        self.flows = snapshot["flows"]
        self.senders = snapshot["senders"]
        self.receivers = snapshot["receivers"]
        self.node_health = {}
        self.receivers_by_id = {receiver["id"]: receiver for receiver in self.receivers}
        self.senders_by_id = {sender["id"]: sender for sender in self.senders}

    def register_resource(self, resource_type: str, payload: dict):
        mapper = {
            "node": self._map_registered_node,
            "device": self._map_registered_device,
            "source": self._map_registered_source,
            "flow": self._map_registered_flow,
            "sender": self._map_registered_sender,
            "receiver": self._map_registered_receiver,
        }.get(resource_type)

        if mapper is None:
            return None

        with self._lock:
            existing_receiver = None
            existing_sender = None
            if resource_type == "receiver":
                existing_receiver = self.receivers_by_id.get(payload["id"])
            elif resource_type == "sender":
                existing_sender = self.senders_by_id.get(payload["id"])

            resource = mapper(payload)
            resource_id = resource["id"]

            if existing_receiver is not None:
                resource["constraints"] = copy.deepcopy(existing_receiver.get("constraints", resource.get("constraints", {})))
                resource["active"] = copy.deepcopy(existing_receiver.get("active", resource.get("active", {})))
                resource["staged"] = copy.deepcopy(existing_receiver.get("staged", resource.get("staged", {})))

            if existing_sender is not None:
                resource["subscribedReceiverId"] = existing_sender.get("subscribedReceiverId")
                resource["transportFile"] = copy.deepcopy(existing_sender.get("transportFile"))
                resource["sourceGroupId"] = existing_sender.get("sourceGroupId", resource.get("sourceGroupId"))

            if resource_type == "node":
                self.nodes = self._upsert(self.nodes, resource)
            elif resource_type == "device":
                self.devices = self._upsert(self.devices, resource)
            elif resource_type == "source":
                self.sources = self._upsert(self.sources, resource)
            elif resource_type == "flow":
                self.flows = self._upsert(self.flows, resource)
            elif resource_type == "sender":
                self.senders = self._upsert(self.senders, resource)
                self._attach_device_sender(resource["deviceId"], resource_id)
            elif resource_type == "receiver":
                self.receivers = self._upsert(self.receivers, resource)
                self._attach_device_receiver(resource["deviceId"], resource_id)

            self.receivers_by_id = {receiver["id"]: receiver for receiver in self.receivers}
            self.senders_by_id = {sender["id"]: sender for sender in self.senders}
            return resource_id

    def record_node_health(self, node_id: str):
        with self._lock:
            self.node_health[node_id] = True

    def delete_resource(self, resource_type: str, resource_id: str):
        with self._lock:
            normalized_type = {
                "nodes": "node",
                "devices": "device",
                "sources": "source",
                "flows": "flow",
                "senders": "sender",
                "receivers": "receiver",
            }.get(resource_type, resource_type)

            if normalized_type == "node":
                removed = self._remove_by_id(self.nodes, resource_id)
                if not removed:
                    return False
                self.devices = [device for device in self.devices if device.get("nodeId") != resource_id]
            elif normalized_type == "device":
                removed = self._remove_by_id(self.devices, resource_id)
                if not removed:
                    return False
                self.sources = [source for source in self.sources if source.get("deviceId") != resource_id]
                self.flows = [flow for flow in self.flows if flow.get("deviceId") != resource_id]
                removed_sender_ids = {sender["id"] for sender in self.senders if sender.get("deviceId") == resource_id}
                self.senders = [sender for sender in self.senders if sender.get("deviceId") != resource_id]
                self.receivers = [receiver for receiver in self.receivers if receiver.get("deviceId") != resource_id]
                if removed_sender_ids:
                    for receiver in self.receivers:
                        for state_name in ("active", "staged"):
                            state = receiver.get(state_name) or {}
                            if state.get("senderId") in removed_sender_ids:
                                state["senderId"] = None
                                state["masterEnable"] = "false"
                                state["transportFile"] = None
            elif normalized_type == "source":
                removed = self._remove_by_id(self.sources, resource_id)
                if not removed:
                    return False
                self.flows = [flow for flow in self.flows if flow.get("sourceId") != resource_id]
            elif normalized_type == "flow":
                removed = self._remove_by_id(self.flows, resource_id)
                if not removed:
                    return False
                for sender in self.senders:
                    if sender.get("flowId") == resource_id:
                        sender["flowId"] = None
                        sender["sourceGroupId"] = sender["id"]
            elif normalized_type == "sender":
                removed = self._remove_by_id(self.senders, resource_id)
                if not removed:
                    return False
                for device in self.devices:
                    device["senderIds"] = [sender_id for sender_id in device.get("senderIds", []) if sender_id != resource_id]
                for receiver in self.receivers:
                    for state_name in ("active", "staged"):
                        state = receiver.get(state_name) or {}
                        if state.get("senderId") == resource_id:
                            state["senderId"] = None
                            state["masterEnable"] = "false"
                            state["transportFile"] = None
            elif normalized_type == "receiver":
                removed = self._remove_by_id(self.receivers, resource_id)
                if not removed:
                    return False
                for device in self.devices:
                    device["receiverIds"] = [receiver_id for receiver_id in device.get("receiverIds", []) if receiver_id != resource_id]
                for sender in self.senders:
                    if sender.get("subscribedReceiverId") == resource_id:
                        sender["subscribedReceiverId"] = None
            else:
                return None

            self.receivers_by_id = {receiver["id"]: receiver for receiver in self.receivers}
            self.senders_by_id = {sender["id"]: sender for sender in self.senders}
            return True

    @staticmethod
    def _upsert(items: list[dict], resource: dict):
        resource_id = resource["id"]
        return [item for item in items if item["id"] != resource_id] + [resource]

    @staticmethod
    def _remove_by_id(items: list[dict], resource_id: str):
        original_count = len(items)
        items[:] = [item for item in items if item["id"] != resource_id]
        return len(items) != original_count

    def _attach_device_sender(self, device_id: str, sender_id: str):
        device = next((item for item in self.devices if item["id"] == device_id), None)
        if device is None:
            return

        sender_ids = [item for item in device.get("senderIds", []) if item != sender_id]
        sender_ids.append(sender_id)
        device["senderIds"] = sender_ids

    def _attach_device_receiver(self, device_id: str, receiver_id: str):
        device = next((item for item in self.devices if item["id"] == device_id), None)
        if device is None:
            return

        receiver_ids = [item for item in device.get("receiverIds", []) if item != receiver_id]
        receiver_ids.append(receiver_id)
        device["receiverIds"] = receiver_ids

    @staticmethod
    def _rate_to_string(value):
        if not value:
            return None

        numerator = value.get("numerator")
        if numerator is None:
            return None

        denominator = value.get("denominator") or 1
        return f"{numerator}/{denominator}"

    @staticmethod
    def _map_registered_node(payload: dict):
        interfaces = payload.get("interfaces") or []
        return {
            "id": payload["id"],
            "label": payload.get("label") or payload["id"],
            "description": payload.get("description"),
            "hostname": payload.get("hostname"),
            "apiVersions": (payload.get("api") or {}).get("versions", []),
            "interfaces": [
                interface.get("name") or interface.get("port_id") or "interface"
                for interface in interfaces
            ],
            "lastSeenAtUtc": payload.get("version_timestamp"),
        }

    @staticmethod
    def _map_registered_device(payload: dict):
        return {
            "id": payload["id"],
            "nodeId": payload.get("node_id", ""),
            "label": payload.get("label") or payload["id"],
            "deviceType": payload.get("type", "urn:x-nmos:device:generic"),
            "senderIds": payload.get("senders", []),
            "receiverIds": payload.get("receivers", []),
            "lastSeenAtUtc": payload.get("version_timestamp"),
        }

    def _map_registered_source(self, payload: dict):
        return {
            "id": payload["id"],
            "deviceId": payload.get("device_id", ""),
            "label": payload.get("label") or payload["id"],
            "format": {
                "format": payload.get("format", ""),
                "mediaType": None,
                "grainRate": self._rate_to_string(payload.get("grain_rate")),
                "frameWidth": None,
                "frameHeight": None,
                "sampleRate": None,
            },
            "lastSeenAtUtc": payload.get("version_timestamp"),
        }

    def _map_registered_flow(self, payload: dict):
        return {
            "id": payload["id"],
            "sourceId": payload.get("source_id", ""),
            "deviceId": payload.get("device_id", ""),
            "label": payload.get("label") or payload["id"],
            "format": {
                "format": payload.get("format", ""),
                "mediaType": payload.get("media_type"),
                "grainRate": self._rate_to_string(payload.get("grain_rate")),
                "frameWidth": payload.get("frame_width"),
                "frameHeight": payload.get("frame_height"),
                "sampleRate": self._rate_to_string(payload.get("sample_rate")),
            },
            "lastSeenAtUtc": payload.get("version_timestamp"),
        }

    @staticmethod
    def _map_registered_sender(payload: dict):
        interface_bindings = payload.get("interface_bindings") or []
        return {
            "id": payload["id"],
            "deviceId": payload.get("device_id", ""),
            "flowId": payload.get("flow_id"),
            "label": payload.get("label") or payload["id"],
            "transport": payload.get("transport", "urn:x-nmos:transport:rtp"),
            "manifestHref": payload.get("manifest_href"),
            "pathType": (interface_bindings[0] if interface_bindings else "A").upper(),
            "subscribedReceiverId": (payload.get("subscription") or {}).get("receiver_id"),
            "sourceGroupId": payload.get("flow_id") or payload["id"],
            "transportFile": None,
            "lastSeenAtUtc": payload.get("version_timestamp"),
        }

    @staticmethod
    def _map_registered_receiver(payload: dict):
        interface_bindings = payload.get("interface_bindings") or []
        return {
            "id": payload["id"],
            "deviceId": payload.get("device_id", ""),
            "label": payload.get("label") or payload["id"],
            "format": {
                "format": payload.get("format", ""),
            },
            "transport": payload.get("transport", "urn:x-nmos:transport:rtp"),
            "signalType": interface_bindings[0] if interface_bindings else "io",
            "constraints": {
                "mediaTypes": ((payload.get("caps") or {}).get("media_types") or []),
                "parameters": [],
            },
            "active": {
                "senderId": None,
                "masterEnable": "false",
                "transportParameters": {},
                "transportFile": None,
                "activation": {
                    "mode": "activate_immediate",
                    "requested_time": None,
                },
            },
            "staged": {
                "senderId": None,
                "masterEnable": "false",
                "transportParameters": {},
                "transportFile": None,
                "activation": {
                    "mode": "activate_immediate",
                    "requested_time": None,
                },
            },
            "lastSeenAtUtc": payload.get("version_timestamp"),
        }

    def list_nodes(self):
        return [
            {
                "id": node["id"],
                "label": node["label"],
                "description": node.get("description"),
                "hostname": node.get("hostname"),
                "api": {"versions": node.get("apiVersions", [])},
                "interfaces": [
                    {"name": interface_name, "port_id": interface_name}
                    for interface_name in node.get("interfaces", [])
                ],
            }
            for node in self.nodes
        ]

    def list_devices(self):
        return [
            {
                "id": device["id"],
                "node_id": device["nodeId"],
                "label": device["label"],
                "type": device["deviceType"],
                "senders": device.get("senderIds", []),
                "receivers": device.get("receiverIds", []),
                "tags": {},
            }
            for device in self.devices
        ]

    def list_sources(self):
        return [
            {
                "id": source["id"],
                "device_id": source["deviceId"],
                "label": source["label"],
                "format": source["format"]["format"],
                "grain_rate": parse_rate(source["format"].get("grainRate")),
                "parents": [],
            }
            for source in self.sources
        ]

    def list_flows(self):
        return [
            {
                "id": flow["id"],
                "source_id": flow["sourceId"],
                "device_id": flow["deviceId"],
                "label": flow["label"],
                "format": flow["format"]["format"],
                "media_type": flow["format"].get("mediaType"),
                "grain_rate": parse_rate(flow["format"].get("grainRate")),
                "frame_width": int(flow["format"]["frameWidth"]) if flow["format"].get("frameWidth") else None,
                "frame_height": int(flow["format"]["frameHeight"]) if flow["format"].get("frameHeight") else None,
                "sample_rate": parse_rate(flow["format"].get("sampleRate")),
                "parents": [],
            }
            for flow in self.flows
        ]

    def list_senders(self, base_url: str):
        return [
            {
                "id": sender["id"],
                "device_id": sender["deviceId"],
                "flow_id": sender.get("flowId"),
                "label": sender["label"],
                "transport": "urn:x-nmos:transport:rtp",
                "manifest_href": self._rewrite_manifest_href(sender.get("manifestHref"), base_url),
                "interface_bindings": [sender.get("pathType", "A").lower()],
                "subscription": {"receiver_id": sender.get("subscribedReceiverId")},
            }
            for sender in self.senders
        ]

    def list_receivers(self):
        return [
            {
                "id": receiver["id"],
                "device_id": receiver["deviceId"],
                "label": receiver["label"],
                "format": receiver["format"]["format"],
                "transport": "urn:x-nmos:transport:rtp",
                "interface_bindings": [receiver.get("signalType", "io").lower()],
                "caps": {"media_types": receiver["constraints"].get("mediaTypes", [])},
            }
            for receiver in self.receivers
        ]

    def get_constraints(self, receiver_id: str):
        receiver = self.receivers_by_id.get(receiver_id)
        if receiver is None:
            return None

        parameters = receiver["constraints"].get("parameters", [])
        if not parameters:
            return []

        leg = {}
        for parameter in parameters:
            value = {}
            if parameter.get("minimum") is not None:
                value["minimum"] = parameter["minimum"]
            if parameter.get("maximum") is not None:
                value["maximum"] = parameter["maximum"]
            if parameter.get("allowedValues"):
                value["enum"] = parameter["allowedValues"]
            leg[parameter["name"]] = value
        return [leg]

    def get_connection_state(self, receiver_id: str, state_name: str):
        receiver = self.receivers_by_id.get(receiver_id)
        if receiver is None:
            return None

        return self._map_connection_state(receiver[state_name])

    def patch_staged(self, receiver_id: str, payload: dict):
        with self._lock:
            receiver = self.receivers_by_id.get(receiver_id)
            if receiver is None:
                return None

            sender_id = payload.get("sender_id", payload.get("senderId"))
            master_enable = bool(payload.get("master_enable", payload.get("masterEnable", False)))
            activation = payload.get("activation") or {}
            mode = activation.get("mode", "activate_immediate")
            requested_time = activation.get("requested_time", activation.get("requestedTime"))

            transport_file = None
            transport_parameters = {}
            if sender_id and master_enable:
                sender = self.senders_by_id.get(sender_id)
                if sender is None:
                    return False

                transport_file = sender.get("transportFile")
                transport_parameters = copy.deepcopy(receiver["active"].get("transportParameters", {}))
                for candidate in self.senders:
                    if candidate.get("subscribedReceiverId") == receiver_id:
                        candidate["subscribedReceiverId"] = None
                source_group_id = sender.get("sourceGroupId")
                for candidate in self.senders:
                    if candidate.get("sourceGroupId") == source_group_id:
                        candidate["subscribedReceiverId"] = receiver_id

            staged_state = {
                "senderId": sender_id if master_enable else None,
                "masterEnable": "true" if master_enable else "false",
                "transportParameters": transport_parameters,
                "transportFile": copy.deepcopy(transport_file),
                "activation": {
                    "mode": mode,
                    "requested_time": requested_time,
                },
            }

            receiver["staged"] = staged_state
            if mode == "activate_immediate":
                receiver["active"] = copy.deepcopy(staged_state)

            return self._map_connection_state(receiver["staged"])

    @staticmethod
    def _rewrite_manifest_href(manifest_href: str | None, base_url: str):
        if not manifest_href:
            return None

        parsed = urlparse(manifest_href)
        if parsed.scheme and parsed.netloc:
            return manifest_href

        return f"{base_url}{parsed.path}"

    @staticmethod
    def _map_connection_state(state: dict):
        activation = state.get("activation") or {}
        return {
            "sender_id": state.get("senderId"),
            "master_enable": str(state.get("masterEnable", "false")).lower() == "true",
            "transport_params": [copy.deepcopy(state.get("transportParameters", {}))],
            "transport_file": None
            if not state.get("transportFile")
            else {
                "type": state["transportFile"].get("contentType", "application/sdp"),
                "data": state["transportFile"].get("content"),
            },
            "activation": {
                "mode": activation.get("mode"),
                "requested_time": activation.get("requested_time"),
                "activation_time": activation.get("requested_time"),
            },
        }


STATE = MockRegistryState(FIXTURE_PATH)


class Handler(BaseHTTPRequestHandler):
    receiver_staged_pattern = re.compile(
        r"^/x-nmos/connection/v1\.1/single/receivers/(?P<receiver_id>[^/]+)/staged/?$"
    )
    receiver_state_pattern = re.compile(
        r"^/x-nmos/connection/v1\.1/single/receivers/(?P<receiver_id>[^/]+)/(?P<state>constraints|active|staged)/?$"
    )
    registration_health_pattern = re.compile(r"^/x-nmos/registration/v1\.3/health/nodes/(?P<node_id>[^/]+)/?$")
    registration_resource_item_pattern = re.compile(
        r"^/x-nmos/registration/v1\.3/resource/(?P<resource_type>[^/]+)/(?P<resource_id>[^/]+)/?$"
    )

    def do_GET(self):
        parsed = urlparse(self.path)
        path = parsed.path

        if path in {"/", ""}:
            self._send_json(
                {
                    "service": "Mock NMOS Registry",
                    "query": "/x-nmos/query/v1.3/",
                    "connection": "/x-nmos/connection/v1.1/",
                    "health": "/health.json",
                }
            )
            return

        if path in {"/x-nmos/query/v1.3", "/x-nmos/query/v1.3/"}:
            self._send_json(
                [
                    "nodes/",
                    "devices/",
                    "sources/",
                    "flows/",
                    "senders/",
                    "receivers/",
                ]
            )
            return

        if path in {"/x-nmos/registration", "/x-nmos/registration/"}:
            self._send_json(["v1.3/"])
            return

        if path in {"/x-nmos/registration/v1.3", "/x-nmos/registration/v1.3/"}:
            self._send_json(["resource/", "health/"])
            return

        if path in {"/x-nmos/registration/v1.3/resource", "/x-nmos/registration/v1.3/resource/"}:
            self._send_json({"description": "POST NMOS resources here."})
            return

        if path in {"/x-nmos/registration/v1.3/health", "/x-nmos/registration/v1.3/health/"}:
            self._send_json(["nodes/"])
            return

        if path in {"/x-nmos/connection/v1.1", "/x-nmos/connection/v1.1/"}:
            self._send_json(["single/"])
            return

        if path in {"/x-nmos/connection/v1.1/single", "/x-nmos/connection/v1.1/single/"}:
            self._send_json(["receivers/"])
            return

        if path in {"/x-nmos/query/v1.3/nodes", "/x-nmos/query/v1.3/nodes/"}:
            self._send_json(STATE.list_nodes())
            return
        if path in {"/x-nmos/query/v1.3/devices", "/x-nmos/query/v1.3/devices/"}:
            self._send_json(STATE.list_devices())
            return
        if path in {"/x-nmos/query/v1.3/sources", "/x-nmos/query/v1.3/sources/"}:
            self._send_json(STATE.list_sources())
            return
        if path in {"/x-nmos/query/v1.3/flows", "/x-nmos/query/v1.3/flows/"}:
            self._send_json(STATE.list_flows())
            return
        if path in {"/x-nmos/query/v1.3/senders", "/x-nmos/query/v1.3/senders/"}:
            self._send_json(STATE.list_senders(self._base_url()))
            return
        if path in {"/x-nmos/query/v1.3/receivers", "/x-nmos/query/v1.3/receivers/"}:
            self._send_json(STATE.list_receivers())
            return

        receiver_match = self.receiver_state_pattern.match(path)
        if receiver_match:
            receiver_id = receiver_match.group("receiver_id")
            state_name = receiver_match.group("state")
            if state_name == "constraints":
                payload = STATE.get_constraints(receiver_id)
            else:
                payload = STATE.get_connection_state(receiver_id, state_name)

            if payload is None:
                self._send_json({"error": "Receiver not found"}, status=HTTPStatus.NOT_FOUND)
                return

            self._send_json(payload)
            return

        self._serve_static(path)

    def do_HEAD(self):
        parsed = urlparse(self.path)
        path = parsed.path

        if path in {
            "/",
            "",
            "/x-nmos/query/v1.3",
            "/x-nmos/query/v1.3/",
            "/x-nmos/registration",
            "/x-nmos/registration/",
            "/x-nmos/registration/v1.3",
            "/x-nmos/registration/v1.3/",
            "/x-nmos/registration/v1.3/resource",
            "/x-nmos/registration/v1.3/resource/",
            "/x-nmos/registration/v1.3/health",
            "/x-nmos/registration/v1.3/health/",
            "/x-nmos/connection/v1.1",
            "/x-nmos/connection/v1.1/",
            "/x-nmos/connection/v1.1/single",
            "/x-nmos/connection/v1.1/single/",
            "/x-nmos/query/v1.3/nodes",
            "/x-nmos/query/v1.3/nodes/",
            "/x-nmos/query/v1.3/devices",
            "/x-nmos/query/v1.3/devices/",
            "/x-nmos/query/v1.3/sources",
            "/x-nmos/query/v1.3/sources/",
            "/x-nmos/query/v1.3/flows",
            "/x-nmos/query/v1.3/flows/",
            "/x-nmos/query/v1.3/senders",
            "/x-nmos/query/v1.3/senders/",
            "/x-nmos/query/v1.3/receivers",
            "/x-nmos/query/v1.3/receivers/",
        } or self.receiver_state_pattern.match(path):
            self.send_response(HTTPStatus.OK)
            self.end_headers()
            return

        self.send_response(HTTPStatus.NOT_FOUND)
        self.end_headers()

    def do_POST(self):
        parsed = urlparse(self.path)
        path = parsed.path

        if path in {"/x-nmos/registration/v1.3/resource", "/x-nmos/registration/v1.3/resource/"}:
            payload = self._read_json()
            if payload is None:
                return

            resource_type = payload.get("type")
            resource_data = payload.get("data")
            if not isinstance(resource_type, str) or not isinstance(resource_data, dict) or "id" not in resource_data:
                self._send_json({"error": "Expected JSON body with type and data.id"}, status=HTTPStatus.BAD_REQUEST)
                return

            resource_id = STATE.register_resource(resource_type, resource_data)
            if resource_id is None:
                self._send_json({"error": f"Unsupported resource type '{resource_type}'"}, status=HTTPStatus.BAD_REQUEST)
                return

            self._send_json(
                {
                    "id": resource_id,
                    "type": resource_type,
                    "message": "Resource registered.",
                },
                status=HTTPStatus.CREATED,
            )
            return

        health_match = self.registration_health_pattern.match(path)
        if health_match:
            STATE.record_node_health(health_match.group("node_id"))
            self._send_json({"health": "ok"})
            return

        self._send_json({"error": "Not found"}, status=HTTPStatus.NOT_FOUND)

    def do_PATCH(self):
        parsed = urlparse(self.path)
        match = self.receiver_staged_pattern.match(parsed.path)
        if not match:
            self._send_json({"error": "Not found"}, status=HTTPStatus.NOT_FOUND)
            return

        raw_body = self._read_body()
        sys.stdout.write(f"PATCH payload raw: {raw_body!r}\n")
        sys.stdout.flush()
        try:
            payload = json.loads(raw_body.decode("utf-8"))
        except json.JSONDecodeError:
            self._send_json({"error": "Invalid JSON"}, status=HTTPStatus.BAD_REQUEST)
            return

        result = STATE.patch_staged(match.group("receiver_id"), payload)
        if result is None:
            self._send_json({"error": "Receiver not found"}, status=HTTPStatus.NOT_FOUND)
            return
        if result is False:
            self._send_json({"error": "Sender not found"}, status=HTTPStatus.BAD_REQUEST)
            return

        self._send_json(result)

    def do_DELETE(self):
        parsed = urlparse(self.path)
        match = self.registration_resource_item_pattern.match(parsed.path)
        if not match:
            self._send_json({"error": "Not found"}, status=HTTPStatus.NOT_FOUND)
            return

        result = STATE.delete_resource(match.group("resource_type"), match.group("resource_id"))
        if result is None:
            self._send_json({"error": "Unsupported resource type"}, status=HTTPStatus.BAD_REQUEST)
            return
        if result is False:
            self._send_json({"error": "Resource not found"}, status=HTTPStatus.NOT_FOUND)
            return

        self.send_response(HTTPStatus.NO_CONTENT)
        self.end_headers()

    def log_message(self, format, *args):
        message = "%s - - [%s] %s\n" % (
            self.address_string(),
            self.log_date_time_string(),
            format % args,
        )
        sys.stdout.write(message)
        sys.stdout.flush()

    def _base_url(self):
        host = self.headers.get("Host")
        if host:
            return f"http://{host}"
        return INTERNAL_BASE_URL.rstrip("/")

    def _read_json(self):
        raw_body = self._read_body()
        try:
            return json.loads(raw_body.decode("utf-8"))
        except json.JSONDecodeError:
            self._send_json({"error": "Invalid JSON"}, status=HTTPStatus.BAD_REQUEST)
            return None

    def _read_body(self):
        transfer_encoding = (self.headers.get("Transfer-Encoding") or "").lower()
        if "chunked" in transfer_encoding:
            chunks = bytearray()
            while True:
                size_line = self.rfile.readline().strip()
                if not size_line:
                    continue

                try:
                    chunk_size = int(size_line.split(b";", 1)[0], 16)
                except ValueError:
                    return b"{}"

                if chunk_size == 0:
                    while True:
                        trailer_line = self.rfile.readline()
                        if trailer_line in (b"\r\n", b"\n", b""):
                            break
                    break

                chunks.extend(self.rfile.read(chunk_size))
                self.rfile.read(2)

            return bytes(chunks) if chunks else b"{}"

        length = int(self.headers.get("Content-Length", "0"))
        return self.rfile.read(length) if length > 0 else b"{}"

    def _serve_static(self, path: str):
        relative_path = path.lstrip("/") or "index.html"
        file_path = (PUBLIC_DIR / relative_path).resolve()
        if PUBLIC_DIR not in file_path.parents and file_path != PUBLIC_DIR:
            self._send_json({"error": "Invalid path"}, status=HTTPStatus.BAD_REQUEST)
            return

        if not file_path.exists() or not file_path.is_file():
            self._send_json({"error": "Not found"}, status=HTTPStatus.NOT_FOUND)
            return

        content_type = "application/octet-stream"
        if file_path.suffix == ".html":
            content_type = "text/html; charset=utf-8"
        elif file_path.suffix == ".json":
            content_type = "application/json; charset=utf-8"
        elif file_path.suffix == ".sdp":
            content_type = "application/sdp"

        payload = file_path.read_bytes()
        self.send_response(HTTPStatus.OK)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def _send_json(self, payload, status=HTTPStatus.OK):
        body = json.dumps(payload).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)


if __name__ == "__main__":
    server = ThreadingHTTPServer((HOST, PORT), Handler)
    print(f"Mock NMOS registry listening on {HOST}:{PORT}")
    server.serve_forever()
