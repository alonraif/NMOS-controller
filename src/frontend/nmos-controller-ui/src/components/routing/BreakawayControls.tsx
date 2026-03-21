import type { RoutingLayer } from "../../hooks/usePreviewState";

interface BreakawayControlsProps {
  enabledLayers: Record<RoutingLayer, boolean>;
  onToggle: (layer: RoutingLayer) => void;
}

export function BreakawayControls({ enabledLayers, onToggle }: BreakawayControlsProps) {
  const labels: Array<{ layer: RoutingLayer; short: string }> = [
    { layer: "Video", short: "V" },
    { layer: "Audio", short: "A" },
    { layer: "Ancillary", short: "ANC" },
  ];

  return (
    <div className="breakaway-controls">
      {labels.map(({ layer, short }) => (
        <button
          key={layer}
          className={enabledLayers[layer] ? "routing-toggle is-active" : "routing-toggle"}
          type="button"
          onClick={() => onToggle(layer)}
        >
          <span>{short}</span>
          <small>{layer}</small>
        </button>
      ))}
    </div>
  );
}
