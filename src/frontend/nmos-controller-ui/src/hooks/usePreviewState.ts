import { useMemo, useState } from "react";

export type RoutingLayer = "Video" | "Audio" | "Ancillary";

export interface PreviewRouteState {
  destinationId: string | null;
  layers: Record<RoutingLayer, string | null>;
}

const emptyLayers = (): Record<RoutingLayer, string | null> => ({
  Video: null,
  Audio: null,
  Ancillary: null,
});

export function usePreviewState() {
  const [preview, setPreview] = useState<PreviewRouteState>({
    destinationId: null,
    layers: emptyLayers(),
  });

  const hasPreview = useMemo(
    () => Boolean(preview.destinationId && Object.values(preview.layers).some((value) => value)),
    [preview],
  );

  function setLayerPreview(destinationId: string, updates: Partial<Record<RoutingLayer, string | null>>) {
    setPreview((current) => ({
      destinationId,
      layers: {
        ...(current.destinationId === destinationId ? current.layers : emptyLayers()),
        ...updates,
      },
    }));
  }

  function clearPreview() {
    setPreview({
      destinationId: null,
      layers: emptyLayers(),
    });
  }

  return {
    preview,
    hasPreview,
    setLayerPreview,
    clearPreview,
  };
}
