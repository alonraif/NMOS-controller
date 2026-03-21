import type { CSSProperties } from "react";
import { StatusBadge } from "../StatusBadge";
import type { RoutingCrosspoint, RoutingDestination, RoutingSource } from "../../api/types";
import type { RoutingLayer } from "../../hooks/usePreviewState";

interface RoutingMatrixProps {
  destinations: RoutingDestination[];
  sources: RoutingSource[];
  crosspoints: RoutingCrosspoint[];
  selectedDestinationId: string | null;
  previewDestinationId: string | null;
  previewLayers: Record<RoutingLayer, string | null>;
  onCrosspointSelect: (destinationId: string, sourceId: string, layer: RoutingLayer) => void;
  onDestinationSelect: (destinationId: string) => void;
}

export function RoutingMatrix({
  destinations,
  sources,
  crosspoints,
  selectedDestinationId,
  previewDestinationId,
  previewLayers,
  onCrosspointSelect,
  onDestinationSelect,
}: RoutingMatrixProps) {
  const crosspointMap = new Map(crosspoints.map((item) => [`${item.destinationId}:${item.sourceId}`, item]));
  const columnStyle = { ["--matrix-columns" as const]: String(sources.length) } as CSSProperties;

  return (
    <div className="routing-matrix-shell">
      <div className="routing-matrix" style={columnStyle}>
        <div className="matrix-row matrix-header">
          <div className="matrix-sticky-cell">Destination</div>
          {sources.map((source) => (
            <div key={source.id} className="matrix-header-cell">
              <strong>{source.label}</strong>
              <div className="matrix-header-meta">
                <StatusBadge tone="muted">{source.layer}</StatusBadge>
                <StatusBadge tone={source.redundancyStatus === "A/B OK" ? "success" : source.redundancyStatus === "No signal" ? "danger" : "warning"}>
                  {source.redundancyStatus}
                </StatusBadge>
              </div>
            </div>
          ))}
        </div>

        {destinations.map((destination) => (
          <div key={destination.id} className={selectedDestinationId === destination.id ? "matrix-row is-selected" : "matrix-row"}>
            <button className="matrix-sticky-cell matrix-destination" type="button" onClick={() => onDestinationSelect(destination.id)}>
              <strong>{destination.label}</strong>
              <div className="table-subtext">{destination.routes.filter((route) => route.isBreakaway).length ? "Breakaway active" : "Unified route"}</div>
            </button>
            {sources.map((source) => {
              const crosspoint = crosspointMap.get(`${destination.id}:${source.id}`);
              const route = destination.routes.find((item) => item.layer === source.layer);
              const isPreview =
                previewDestinationId === destination.id && previewLayers[source.layer as RoutingLayer] === source.id;

              return (
                <button
                  key={`${destination.id}:${source.id}`}
                  type="button"
                  className={[
                    "matrix-cell",
                    crosspoint?.isCompatible ? "" : "is-disabled",
                    crosspoint?.isActive ? "is-active" : "",
                    isPreview ? "is-preview" : "",
                    route?.isBreakaway ? "is-breakaway" : "",
                  ]
                    .filter(Boolean)
                    .join(" ")}
                  onClick={() => onCrosspointSelect(destination.id, source.id, source.layer as RoutingLayer)}
                  disabled={!crosspoint?.isCompatible}
                >
                  <span>{crosspoint?.isActive ? "Active" : isPreview ? "Preview" : "Route"}</span>
                </button>
              );
            })}
          </div>
        ))}
      </div>
    </div>
  );
}
