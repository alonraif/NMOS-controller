import type {
  ApiEnvelope,
  AuditEntry,
  ConnectReceiverPayload,
  DisconnectReceiverPayload,
  ExecutePresetPayload,
  NmosReceiver,
  NmosSender,
  PresetSalvo,
  RegistrySettings,
  ResourceDetail,
  RoutingConnectPayload,
  RoutingDisconnectPayload,
  RoutingMatrix,
  RouteOperationResponse,
  RouteValidationPayload,
  RouteValidationResult,
  TopologyGraph,
  UpdateRegistryPayload,
  UpsertPresetPayload,
} from "./types";

const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.replace(/\/$/, "") ?? "http://localhost:8080";
const API_ROOT = `${API_BASE_URL}/api/v1`;

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_ROOT}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const problem = (await response.json()) as { detail?: string; title?: string };
      message = problem.detail ?? problem.title ?? message;
    } catch {
      const text = await response.text();
      if (text) {
        message = text;
      }
    }

    throw new Error(message);
  }

  return (await response.json()) as T;
}

function unwrap<T>(envelope: ApiEnvelope<T>): T {
  return envelope.data;
}

export const api = {
  async getTopology(refresh = false) {
    return unwrap(await request<ApiEnvelope<TopologyGraph>>(`/topology?refresh=${refresh}`));
  },
  async getRoutingMatrix(refresh = false) {
    return unwrap(await request<ApiEnvelope<RoutingMatrix>>(`/routing/matrix?refresh=${refresh}`));
  },
  async getSenders(refresh = false) {
    return unwrap(await request<ApiEnvelope<NmosSender[]>>(`/senders?refresh=${refresh}`));
  },
  async getReceivers(refresh = false) {
    return unwrap(await request<ApiEnvelope<NmosReceiver[]>>(`/receivers?refresh=${refresh}`));
  },
  async getResource(resourceId: string) {
    return unwrap(await request<ApiEnvelope<ResourceDetail>>(`/resources/${resourceId}`));
  },
  async getRegistry() {
    return unwrap(await request<ApiEnvelope<RegistrySettings>>(`/registry`));
  },
  async updateRegistry(payload: UpdateRegistryPayload) {
    return unwrap(
      await request<ApiEnvelope<RegistrySettings>>(`/registry`, {
        method: "PUT",
        body: JSON.stringify(payload),
      }),
    );
  },
  async validateRoute(payload: RouteValidationPayload) {
    return unwrap(
      await request<ApiEnvelope<RouteValidationResult>>(`/routing/validate`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async connectReceiver(receiverId: string, payload: ConnectReceiverPayload) {
    return unwrap(
      await request<ApiEnvelope<RouteOperationResponse>>(`/routing/receivers/${receiverId}/connect`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async disconnectReceiver(receiverId: string, payload: DisconnectReceiverPayload) {
    return unwrap(
      await request<ApiEnvelope<RouteOperationResponse>>(`/routing/receivers/${receiverId}/disconnect`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async connectRouting(payload: RoutingConnectPayload) {
    return unwrap(
      await request<ApiEnvelope<RouteOperationResponse>>(`/routing/connect`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async disconnectRouting(payload: RoutingDisconnectPayload) {
    return unwrap(
      await request<ApiEnvelope<RouteOperationResponse>>(`/routing/disconnect`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async getPresets() {
    return unwrap(await request<ApiEnvelope<PresetSalvo[]>>(`/presets`));
  },
  async getPreset(id: string) {
    return unwrap(await request<ApiEnvelope<PresetSalvo>>(`/presets/${id}`));
  },
  async savePreset(payload: UpsertPresetPayload) {
    return unwrap(
      await request<ApiEnvelope<string>>(`/presets`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async deletePreset(id: string) {
    return unwrap(
      await request<ApiEnvelope<RouteOperationResponse>>(`/presets/${id}`, {
        method: "DELETE",
      }),
    );
  },
  async executePreset(id: string, payload: ExecutePresetPayload) {
    return unwrap(
      await request<ApiEnvelope<RouteOperationResponse>>(`/presets/${id}/execute`, {
        method: "POST",
        body: JSON.stringify(payload),
      }),
    );
  },
  async getAudit(limit = 100) {
    return unwrap(await request<ApiEnvelope<AuditEntry[]>>(`/audit?limit=${limit}`));
  },
};
