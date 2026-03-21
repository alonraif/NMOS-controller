import { StatusBadge } from "../StatusBadge";

interface RouteInspectorPanelProps {
  selectedDestinationLabel: string | null;
  inspectorRoutes: Array<{
    layer: string;
    isSupported: boolean;
    activeSourceLabel: string | null;
    previewSourceLabel: string | null;
    redundancyStatus: string;
    isBreakaway: boolean;
  }>;
  collapsed?: boolean;
}

export function RouteInspectorPanel({
  selectedDestinationLabel,
  inspectorRoutes,
  collapsed = false,
}: RouteInspectorPanelProps) {
  if (!selectedDestinationLabel) {
    return <p className="muted-copy">Select a destination from the router, XY panel, or topology graph.</p>;
  }

  if (collapsed) {
    return (
      <div className="inspector-summary">
        <strong>{selectedDestinationLabel}</strong>
        <span className="table-subtext">{inspectorRoutes.filter((route) => route.activeSourceLabel).length} active layers</span>
      </div>
    );
  }

  return (
    <div className="route-inspector">
      <div className="inspector-title-row">
        <strong>{selectedDestinationLabel}</strong>
        <span className="table-subtext">Shared state across all routing tabs</span>
      </div>
      {inspectorRoutes.map((route) => (
        <div key={route.layer} className="inspector-row">
          <div>
            <strong>{route.layer}</strong>
            <div className="table-subtext">{route.isSupported ? "Supported" : "Unavailable"}</div>
          </div>
          <div className="stack-sm">
            <span>Active: {route.activeSourceLabel ?? "None"}</span>
            <span className="preview-copy">Preview: {route.previewSourceLabel ?? "None"}</span>
          </div>
          <div className="xy-item-row">
            <StatusBadge tone={route.redundancyStatus === "A/B OK" ? "success" : route.redundancyStatus === "No signal" ? "danger" : "warning"}>
              {route.redundancyStatus}
            </StatusBadge>
            {route.isBreakaway ? <StatusBadge tone="info">Breakaway</StatusBadge> : null}
          </div>
        </div>
      ))}
    </div>
  );
}
