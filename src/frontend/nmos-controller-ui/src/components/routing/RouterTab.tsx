import { Card } from "../Card";
import { StatusBadge } from "../StatusBadge";
import { BreakawayControls } from "./BreakawayControls";
import { RoutingMatrix } from "./RoutingMatrix";
import { TakeControls } from "./TakeControls";
import type { RoutingLayer } from "../../hooks/usePreviewState";
import type { RoutingCrosspoint, RoutingDestination, RoutingSource } from "../../api/types";

interface RouterTabProps {
  sources: RoutingSource[];
  destinations: RoutingDestination[];
  filteredSources: RoutingSource[];
  crosspoints: RoutingCrosspoint[];
  selectedDestinationId: string | null;
  selectedSourceId: string | null;
  previewDestinationId: string | null;
  previewLayers: Record<RoutingLayer, string | null>;
  enabledLayers: Record<RoutingLayer, boolean>;
  autoTake: boolean;
  hasPreview: boolean;
  isBusy: boolean;
  sourceSearch: string;
  destinationSearch: string;
  onSourceSearchChange: (value: string) => void;
  onDestinationSearchChange: (value: string) => void;
  onToggleLayer: (layer: RoutingLayer) => void;
  onSourceSelect: (sourceId: string) => void;
  onDestinationSelect: (destinationId: string) => void;
  onCrosspointSelect: (destinationId: string, sourceId: string, layer: RoutingLayer) => void;
  onToggleAutoTake: (value: boolean) => void;
  onTake: () => void;
  onClear: () => void;
  onDisconnect: () => void;
}

export function RouterTab({
  sources,
  destinations,
  filteredSources,
  crosspoints,
  selectedDestinationId,
  selectedSourceId,
  previewDestinationId,
  previewLayers,
  enabledLayers,
  autoTake,
  hasPreview,
  isBusy,
  sourceSearch,
  destinationSearch,
  onSourceSearchChange,
  onDestinationSearchChange,
  onToggleLayer,
  onSourceSelect,
  onDestinationSelect,
  onCrosspointSelect,
  onToggleAutoTake,
  onTake,
  onClear,
  onDisconnect,
}: RouterTabProps) {
  const visibleSources = filteredSources.filter((source) => source.label.toLowerCase().includes(sourceSearch.toLowerCase()));
  const visibleDestinations = destinations.filter((destination) => destination.label.toLowerCase().includes(destinationSearch.toLowerCase()));

  return (
    <div className="routing-tab-layout is-router">
      <div className="stack">
        <Card
          title="Source Bank"
          subtitle="Grouped sources with quick filtering and redundancy badges."
          actions={
            <input
              value={sourceSearch}
              onChange={(event) => onSourceSearchChange(event.target.value)}
              placeholder="Filter sources"
            />
          }
        >
          <div className="source-panel">
            {visibleSources.map((source) => (
              <button
                key={source.id}
                className={selectedSourceId === source.id ? "source-tile is-selected" : "source-tile"}
                type="button"
                onClick={() => onSourceSelect(source.id)}
              >
                <strong>{source.label}</strong>
                <div className="xy-item-row">
                  <StatusBadge tone="muted">{source.layer}</StatusBadge>
                  <StatusBadge tone={source.redundancyStatus === "A/B OK" ? "success" : source.redundancyStatus === "No signal" ? "danger" : "warning"}>
                    {source.redundancyStatus}
                  </StatusBadge>
                </div>
              </button>
            ))}
          </div>
        </Card>

        <Card
          title="Destination List"
          subtitle="Current destination focus shared with XY and topology."
          actions={
            <input
              value={destinationSearch}
              onChange={(event) => onDestinationSearchChange(event.target.value)}
              placeholder="Filter destinations"
            />
          }
        >
          <div className="destination-list">
            {visibleDestinations.map((destination) => (
              <button
                key={destination.id}
                type="button"
                className={selectedDestinationId === destination.id ? "destination-tile is-selected" : "destination-tile"}
                onClick={() => onDestinationSelect(destination.id)}
              >
                <strong>{destination.label}</strong>
                <span className="table-subtext">
                  {destination.routes.filter((route) => route.isBreakaway).length ? "Breakaway active" : "Unified route"}
                </span>
              </button>
            ))}
          </div>
        </Card>
      </div>

      <div className="stack">
        <Card
          title="Router Panel"
          subtitle="Focused matrix view for rapid operational routing."
          actions={<BreakawayControls enabledLayers={enabledLayers} onToggle={onToggleLayer} />}
        >
          <RoutingMatrix
            destinations={visibleDestinations}
            sources={visibleSources}
            crosspoints={crosspoints}
            selectedDestinationId={selectedDestinationId}
            previewDestinationId={previewDestinationId}
            previewLayers={previewLayers}
            onCrosspointSelect={onCrosspointSelect}
            onDestinationSelect={onDestinationSelect}
          />
        </Card>

        <Card title="Route Actions" subtitle="Preview, take, disconnect, and auto-take controls.">
          <TakeControls
            autoTake={autoTake}
            hasPreview={hasPreview}
            isBusy={isBusy}
            onToggleAutoTake={onToggleAutoTake}
            onTake={onTake}
            onClear={onClear}
            onDisconnect={onDisconnect}
          />
        </Card>
      </div>
    </div>
  );
}
