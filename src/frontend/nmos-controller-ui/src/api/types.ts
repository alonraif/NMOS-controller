export type ControllerMode = "Live" | "Mock";
export type CompatibilityStatus = "Compatible" | "Warning" | "Incompatible";
export type ActivationModeType = "Immediate" | "ScheduledRelative" | "ScheduledAbsolute";
export type ResourceKind =
  | "Registry"
  | "Node"
  | "Device"
  | "Source"
  | "Flow"
  | "Sender"
  | "Receiver";

export interface ApiEnvelope<T> {
  data: T;
  utc: string;
}

export interface RegistrySummary {
  id: string;
  name: string;
  baseUrl: string;
  queryApiVersion: string;
  connectionApiVersion: string;
  mode: ControllerMode;
  isEnabled: boolean;
}

export interface RegistrySettings extends RegistrySummary {
  updatedAtUtc: string;
}

export interface MediaFormatSummary {
  format: string;
  mediaType: string | null;
  grainRate: string | null;
  frameWidth: string | null;
  frameHeight: string | null;
  sampleRate: string | null;
}

export interface ConstraintParameter {
  name: string;
  minimum: string | null;
  maximum: string | null;
  allowedValues: string[];
}

export interface ConstraintSet {
  parameters: ConstraintParameter[];
  mediaTypes: string[];
  transportTypes: string[];
  requiresTransportFile: boolean;
}

export interface TransportFileData {
  contentType: string;
  content: string;
}

export interface ConnectionState {
  senderId: string | null;
  masterEnable: string | null;
  transportParameters: Record<string, string>;
  transportFile: TransportFileData | null;
}

export interface NmosNode {
  id: string;
  label: string;
  hostname: string | null;
  description: string | null;
  apiVersions: string[];
  interfaces: string[];
  lastSeenAtUtc: string;
}

export interface NmosDevice {
  id: string;
  nodeId: string;
  label: string;
  deviceType: string;
  senderIds: string[];
  receiverIds: string[];
  lastSeenAtUtc: string;
}

export interface NmosSource {
  id: string;
  deviceId: string;
  label: string;
  format: MediaFormatSummary;
  lastSeenAtUtc: string;
}

export interface NmosFlow {
  id: string;
  sourceId: string;
  deviceId: string;
  label: string;
  format: MediaFormatSummary;
  lastSeenAtUtc: string;
}

export interface NmosSender {
  id: string;
  nodeId: string;
  deviceId: string;
  flowId: string | null;
  label: string;
  transport: string;
  format: MediaFormatSummary;
  manifestHref: string | null;
  subscribedReceiverId: string | null;
  transportFile: TransportFileData | null;
  lastSeenAtUtc: string;
}

export interface NmosReceiver {
  id: string;
  nodeId: string;
  deviceId: string;
  label: string;
  transport: string;
  format: MediaFormatSummary;
  constraints: ConstraintSet;
  active: ConnectionState;
  staged: ConnectionState;
  isConnectable: boolean;
  lastSeenAtUtc: string;
}

export interface TopologyGraph {
  registry: RegistrySummary;
  nodes: NmosNode[];
  devices: NmosDevice[];
  sources: NmosSource[];
  flows: NmosFlow[];
  senders: NmosSender[];
  receivers: NmosReceiver[];
  refreshedAtUtc: string;
}

export interface ResourceDetail {
  id: string;
  kind: ResourceKind;
  payload: unknown;
}

export interface RouteValidationIssue {
  code: string;
  message: string;
  isBlocking: boolean;
}

export interface RouteValidationResult {
  status: CompatibilityStatus;
  issues: RouteValidationIssue[];
}

export interface RouteOperationResponse {
  succeeded: boolean;
  message: string | null;
}

export interface AuditEntry {
  id: string;
  actionType: string;
  actor: string;
  summary: string;
  resourceId: string | null;
  resourceType: string | null;
  correlationId: string | null;
  occurredAtUtc: string;
  metadataJson: string | null;
}

export interface PresetRoute {
  receiverId: string;
  senderId: string | null;
  activationMode: ActivationModeType;
  activationTimeUtc: string | null;
  requestedOffset: string | null;
}

export interface PresetSalvo {
  id: string;
  name: string;
  description: string | null;
  routes: PresetRoute[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ActivationPayload {
  activationMode: ActivationModeType;
  activationTimeUtc?: string;
  requestedOffsetSeconds?: number;
}

export interface ConnectReceiverPayload extends ActivationPayload {
  senderId: string;
  requestedBy: string;
}

export interface DisconnectReceiverPayload extends ActivationPayload {
  requestedBy: string;
}

export interface RouteValidationPayload extends ActivationPayload {
  senderId: string;
  receiverId: string;
}

export interface PresetRouteRequest {
  receiverId: string;
  senderId: string | null;
  activationMode: ActivationModeType;
  activationTimeUtc?: string;
  requestedOffsetSeconds?: number;
}

export interface UpsertPresetPayload {
  id?: string;
  name: string;
  description?: string;
  routes: PresetRouteRequest[];
}

export interface ExecutePresetPayload {
  requestedBy: string;
  activationMode?: ActivationModeType;
  activationTimeUtc?: string;
  requestedOffsetSeconds?: number;
}

export interface UpdateRegistryPayload {
  name: string;
  baseUrl: string;
  queryApiVersion: string;
  connectionApiVersion: string;
  mode: ControllerMode;
  isEnabled: boolean;
}
