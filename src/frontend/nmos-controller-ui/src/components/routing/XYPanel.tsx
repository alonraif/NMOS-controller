import { StatusBadge } from "../StatusBadge";
import type { RoutingDestination, RoutingSource } from "../../api/types";

interface XYPanelProps {
  destinations: RoutingDestination[];
  sources: RoutingSource[];
  selectedDestinationId: string | null;
  selectedSourceId: string | null;
  onDestinationSelect: (destinationId: string) => void;
  onSourceSelect: (sourceId: string) => void;
}

export function XYPanel({
  destinations,
  sources,
  selectedDestinationId,
  selectedSourceId,
  onDestinationSelect,
  onSourceSelect,
}: XYPanelProps) {
  return (
    <div className="xy-panel">
      <div className="xy-column">
        <div className="xy-column-header">
          <h4>Destinations</h4>
          <span className="muted-copy">Select destination first</span>
        </div>
        <div className="xy-list">
          {destinations.map((destination) => (
            <button
              key={destination.id}
              className={selectedDestinationId === destination.id ? "xy-item is-selected" : "xy-item"}
              type="button"
              onClick={() => onDestinationSelect(destination.id)}
            >
              <strong>{destination.label}</strong>
              <div className="xy-item-meta">
                {destination.tags.map((tag) => (
                  <StatusBadge key={tag} tone="info">
                    {tag}
                  </StatusBadge>
                ))}
              </div>
            </button>
          ))}
        </div>
      </div>
      <div className="xy-column">
        <div className="xy-column-header">
          <h4>Sources</h4>
          <span className="muted-copy">Filtered by destination and breakaway selection</span>
        </div>
        <div className="xy-list">
          {sources.map((source) => (
            <button
              key={source.id}
              className={selectedSourceId === source.id ? "xy-item is-preview" : "xy-item"}
              type="button"
              onClick={() => onSourceSelect(source.id)}
              disabled={!selectedDestinationId}
            >
              <strong>{source.label}</strong>
              <div className="xy-item-row">
                <StatusBadge tone={source.redundancyStatus === "A/B OK" ? "success" : source.redundancyStatus === "No signal" ? "danger" : "warning"}>
                  {source.redundancyStatus}
                </StatusBadge>
                <StatusBadge tone="muted">{source.layer}</StatusBadge>
              </div>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}
