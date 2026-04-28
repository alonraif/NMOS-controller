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
  isEnabled: boolean;
}

export interface RegistrySettings extends RegistrySummary {
  connectionBaseUrl: string | null;
  connectionBaseUrls: string | null;
  updatedAtUtc: string;
}

export interface HostResourceSnapshot {
  cpuTotalPercent: number;
  cpuAvailablePercent: number;
  cpuUsedByControllerPercent: number;
  memoryTotalBytes: number;
  memoryAvailableBytes: number;
  memoryUsedByControllerBytes: number;
  sampledAtUtc: string;
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
  signalType: string;
  sourceGroupId: string;
  sourceGroupLabel: string;
  redundancyGroupId: string | null;
  pathType: string;
  isHealthy: boolean;
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
  signalType: string;
  routingDestinationId: string;
  routingDestinationLabel: string;
  lastSeenAtUtc: string;
}

export interface RoutingDestinationSnapshot {
  id: string;
  label: string;
  nodeId: string;
  deviceId: string;
  videoReceiverId: string | null;
  audioReceiverId: string | null;
  ancillaryReceiverId: string | null;
  tags: string[];
}

export interface TopologyRouteEdge {
  id: string;
  source: string;
  target: string;
  state: string;
  path: string;
  layer: string;
  redundancyGroup: string | null;
  isHealthy: boolean;
  metadata: Record<string, string>;
}

export interface TopologyGraph {
  registry: RegistrySummary;
  nodes: NmosNode[];
  devices: NmosDevice[];
  sources: NmosSource[];
  flows: NmosFlow[];
  senders: NmosSender[];
  receivers: NmosReceiver[];
  routingDestinations: RoutingDestinationSnapshot[];
  routeEdges: TopologyRouteEdge[];
  refreshedAtUtc: string;
}

export interface RoutingSource {
  id: string;
  label: string;
  groupHint: string;
  layer: string;
  primarySenderId: string | null;
  secondarySenderId: string | null;
  redundancyStatus: string;
  isAvailable: boolean;
  transport: string;
  format: string;
  nodeId: string;
  deviceId: string;
}

export interface RoutingDestinationRoute {
  layer: string;
  isSupported: boolean;
  receiverId: string | null;
  activeSourceId: string | null;
  activeSourceLabel: string | null;
  activeSenderId: string | null;
  stagedSourceId: string | null;
  stagedSourceLabel: string | null;
  stagedSenderId: string | null;
  redundancyStatus: string;
  isBreakaway: boolean;
}

export interface RoutingDestination {
  id: string;
  label: string;
  nodeId: string;
  deviceId: string;
  routes: RoutingDestinationRoute[];
  tags: string[];
}

export interface RoutingCrosspoint {
  destinationId: string;
  sourceId: string;
  layer: string;
  isCompatible: boolean;
  isActive: boolean;
  isBreakaway: boolean;
}

export interface RoutingMatrix {
  sources: RoutingSource[];
  destinations: RoutingDestination[];
  crosspoints: RoutingCrosspoint[];
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

export interface RoutingConnectPayload extends ActivationPayload {
  destinationId: string;
  requestedBy: string;
  videoSourceId?: string | null;
  audioSourceId?: string | null;
  ancillarySourceId?: string | null;
}

export interface RoutingDisconnectPayload extends ActivationPayload {
  destinationId: string;
  requestedBy: string;
  disconnectVideo: boolean;
  disconnectAudio: boolean;
  disconnectAncillary: boolean;
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
  connectionBaseUrl?: string | null;
  connectionBaseUrls?: string | null;
  queryApiVersion: string;
  connectionApiVersion: string;
  isEnabled: boolean;
}
