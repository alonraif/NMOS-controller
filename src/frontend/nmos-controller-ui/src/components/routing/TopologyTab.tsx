import { Card } from "../Card";
import { StatusBadge } from "../StatusBadge";
import type { TopologyGraph, TopologyRouteEdge } from "../../api/types";
import { TopologyView } from "../topology/TopologyView";
import { RouteInspectorPanel } from "./RouteInspectorPanel";

interface TopologyTabProps {
  topology: TopologyGraph;
  previewEdges: TopologyRouteEdge[];
  selectedDestinationId: string | null;
  selectedSourceId: string | null;
  selectedDestinationLabel: string | null;
  inspectorRoutes: Array<{
    layer: string;
    isSupported: boolean;
    activeSourceLabel: string | null;
    previewSourceLabel: string | null;
    redundancyStatus: string;
    isBreakaway: boolean;
  }>;
  visibleLayers: string[];
  showInfrastructure: boolean;
  showOnlySelectedRoute: boolean;
  inspectorExpanded: boolean;
  onToggleLayer: (layer: string) => void;
  onToggleInfrastructure: (value: boolean) => void;
  onToggleOnlySelectedRoute: (value: boolean) => void;
  onToggleInspector: () => void;
  onRouteSelect: (destinationId: string, sourceId?: string) => void;
}

export function TopologyTab({
  topology,
  previewEdges,
  selectedDestinationId,
  selectedSourceId,
  selectedDestinationLabel,
  inspectorRoutes,
  visibleLayers,
  showInfrastructure,
  showOnlySelectedRoute,
  inspectorExpanded,
  onToggleLayer,
  onToggleInfrastructure,
  onToggleOnlySelectedRoute,
  onToggleInspector,
  onRouteSelect,
}: TopologyTabProps) {
  const layerOptions = ["Video", "Audio", "Ancillary"];

  return (
    <div className="routing-tab-layout is-topology">
      <Card
        title="Topology View"
        subtitle="Engineering view of active, preview, and staged routes with 2022-7 path awareness."
        actions={
          <div className="topology-toolbar">
            {layerOptions.map((layer) => (
              <button
                key={layer}
                type="button"
                className={visibleLayers.includes(layer) ? "routing-toggle is-active" : "routing-toggle"}
                onClick={() => onToggleLayer(layer)}
              >
                {layer}
              </button>
            ))}
            <label className="take-checkbox">
              <input
                type="checkbox"
                checked={showInfrastructure}
                onChange={(event) => onToggleInfrastructure(event.target.checked)}
              />
              <span>Infra</span>
            </label>
            <label className="take-checkbox">
              <input
                type="checkbox"
                checked={showOnlySelectedRoute}
                onChange={(event) => onToggleOnlySelectedRoute(event.target.checked)}
              />
              <span>Selected route only</span>
            </label>
          </div>
        }
      >
        <div className="topology-tab-grid">
          <div className="stack">
            <div className="topology-legend">
              <StatusBadge tone="success">A path active</StatusBadge>
              <StatusBadge tone="info">B path dashed</StatusBadge>
              <StatusBadge tone="warning">Preview</StatusBadge>
              <StatusBadge tone="danger">Degraded</StatusBadge>
            </div>
            <TopologyView
              topology={topology}
              previewEdges={previewEdges}
              selectedDestinationId={selectedDestinationId}
              selectedSourceId={selectedSourceId}
              onRouteSelect={onRouteSelect}
              visibleLayers={visibleLayers}
              showInfrastructure={showInfrastructure}
              showOnlySelectedRoute={showOnlySelectedRoute}
            />
          </div>

          <aside className={inspectorExpanded ? "topology-inspector" : "topology-inspector is-collapsed"}>
            <div className="inspector-title-row">
              <strong>Route Inspector</strong>
              <button className="ghost-button" type="button" onClick={onToggleInspector}>
                {inspectorExpanded ? "Collapse" : "Expand"}
              </button>
            </div>
            <RouteInspectorPanel
              selectedDestinationLabel={selectedDestinationLabel}
              inspectorRoutes={inspectorRoutes}
              collapsed={!inspectorExpanded}
            />
          </aside>
        </div>
      </Card>
    </div>
  );
}
