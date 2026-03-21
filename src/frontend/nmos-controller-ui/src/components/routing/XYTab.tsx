import { Card } from "../Card";
import { BreakawayControls } from "./BreakawayControls";
import { TakeControls } from "./TakeControls";
import { XYPanel } from "./XYPanel";
import type { RoutingDestination, RoutingSource } from "../../api/types";
import type { RoutingLayer } from "../../hooks/usePreviewState";

interface XYTabProps {
  destinations: RoutingDestination[];
  sources: RoutingSource[];
  selectedDestinationId: string | null;
  selectedSourceId: string | null;
  enabledLayers: Record<RoutingLayer, boolean>;
  autoTake: boolean;
  hasPreview: boolean;
  isBusy: boolean;
  onDestinationSelect: (destinationId: string) => void;
  onSourceSelect: (sourceId: string) => void;
  onToggleLayer: (layer: RoutingLayer) => void;
  onToggleAutoTake: (value: boolean) => void;
  onTake: () => void;
  onClear: () => void;
  onDisconnect: () => void;
}

export function XYTab({
  destinations,
  sources,
  selectedDestinationId,
  selectedSourceId,
  enabledLayers,
  autoTake,
  hasPreview,
  isBusy,
  onDestinationSelect,
  onSourceSelect,
  onToggleLayer,
  onToggleAutoTake,
  onTake,
  onClear,
  onDisconnect,
}: XYTabProps) {
  return (
    <div className="routing-tab-layout is-xy">
      <Card
        title="XY Switching"
        subtitle="Destination-first routing with preview and TAKE separated from the matrix view."
        actions={<BreakawayControls enabledLayers={enabledLayers} onToggle={onToggleLayer} />}
      >
        <XYPanel
          destinations={destinations}
          sources={sources}
          selectedDestinationId={selectedDestinationId}
          selectedSourceId={selectedSourceId}
          onDestinationSelect={onDestinationSelect}
          onSourceSelect={onSourceSelect}
        />
      </Card>

      <Card title="Take Controls" subtitle="Keyboard-friendly TAKE workflow with preview persistence across tabs.">
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
  );
}
