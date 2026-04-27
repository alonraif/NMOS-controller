export const queryKeys = {
  topology: (refresh = false) => ["topology", refresh] as const,
  routingMatrix: (refresh = false) => ["routing-matrix", refresh] as const,
  senders: (refresh = false) => ["senders", refresh] as const,
  receivers: (refresh = false) => ["receivers", refresh] as const,
  resource: (resourceId: string) => ["resource", resourceId] as const,
  registry: () => ["registry"] as const,
  hostResources: () => ["host-resources"] as const,
  presets: () => ["presets"] as const,
  preset: (presetId: string) => ["preset", presetId] as const,
  audit: (limit: number) => ["audit", limit] as const,
};
