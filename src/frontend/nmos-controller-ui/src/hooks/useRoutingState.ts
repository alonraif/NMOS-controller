import { useEffect, useMemo, useState } from "react";
import { useConnectRouting, useDisconnectRouting, useRoutingMatrix, useTopology } from "../api/hooks";
import type {
  RoutingDestination,
  RoutingDestinationRoute,
  RoutingMatrix,
  RoutingSource,
  TopologyGraph,
} from "../api/types";
import { usePreviewState, type RoutingLayer } from "./usePreviewState";

export type RoutingMode = "matrix" | "xy";

const layerOrder: RoutingLayer[] = ["Video", "Audio", "Ancillary"];

export function useRoutingState() {
  const matrixQuery = useRoutingMatrix();
  const topologyQuery = useTopology();
  const connectMutation = useConnectRouting();
  const disconnectMutation = useDisconnectRouting();
  const { preview, hasPreview, setLayerPreview, clearPreview } = usePreviewState();

  const [mode, setMode] = useState<RoutingMode>("matrix");
  const [selectedDestinationId, setSelectedDestinationId] = useState<string | null>(null);
  const [selectedSourceId, setSelectedSourceId] = useState<string | null>(null);
  const [autoTake, setAutoTake] = useState(false);
  const [xyFocus, setXyFocus] = useState<"destination" | "source">("destination");
  const [sourceSearch, setSourceSearch] = useState("");
  const [destinationSearch, setDestinationSearch] = useState("");
  const [visibleTopologyLayers, setVisibleTopologyLayers] = useState<string[]>(["Video", "Audio", "Ancillary"]);
  const [showInfrastructure, setShowInfrastructure] = useState(true);
  const [showOnlySelectedRoute, setShowOnlySelectedRoute] = useState(false);
  const [enabledLayers, setEnabledLayers] = useState<Record<RoutingLayer, boolean>>({
    Video: true,
    Audio: true,
    Ancillary: false,
  });

  const matrix = matrixQuery.data;
  const topology = topologyQuery.data;

  const destinations = matrix?.destinations ?? [];
  const sources = matrix?.sources ?? [];

  const destinationMap = useMemo(() => new Map(destinations.map((item) => [item.id, item])), [destinations]);
  const sourceMap = useMemo(() => new Map(sources.map((item) => [item.id, item])), [sources]);

  const selectedDestination = selectedDestinationId ? destinationMap.get(selectedDestinationId) ?? null : null;
  const previewDestination = preview.destinationId ? destinationMap.get(preview.destinationId) ?? null : null;

  const filteredSources = useMemo(() => {
    if (!selectedDestination) {
      return sources.filter((source) => enabledLayers[source.layer as RoutingLayer]);
    }

    return sources.filter((source) => {
      if (!enabledLayers[source.layer as RoutingLayer]) {
        return false;
      }

      const route = selectedDestination.routes.find((item) => item.layer === source.layer);
      return Boolean(route?.isSupported);
    });
  }, [enabledLayers, selectedDestination, sources]);

  const previewEdges = useMemo(() => buildPreviewEdges(previewDestination, preview.layers, topology, sourceMap), [preview.layers, previewDestination, sourceMap, topology]);

  const inspectorRoutes = useMemo(() => {
    if (!selectedDestination) {
      return [];
    }

    return selectedDestination.routes.map((route) => ({
      ...route,
      previewSourceId: preview.destinationId === selectedDestination.id ? preview.layers[route.layer as RoutingLayer] : null,
      previewSourceLabel:
        preview.destinationId === selectedDestination.id
          ? sourceMap.get(preview.layers[route.layer as RoutingLayer] ?? "")?.label ?? null
          : null,
    }));
  }, [preview.destinationId, preview.layers, selectedDestination, sourceMap]);

  useEffect(() => {
    if (!selectedDestinationId && destinations.length > 0) {
      setSelectedDestinationId(destinations[0].id);
    }
  }, [destinations, selectedDestinationId]);

  useEffect(() => {
    if (!autoTake || !hasPreview) {
      return;
    }

    void takePreview();
  }, [autoTake, hasPreview]);

  useEffect(() => {
    if (mode !== "xy") {
      return;
    }

    function onKeyDown(event: KeyboardEvent) {
      if (!destinations.length) {
        return;
      }

      if (event.key === "Escape") {
        clearPreview();
        return;
      }

      if (event.key === "Enter" && hasPreview) {
        event.preventDefault();
        void takePreview();
        return;
      }

      if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
        event.preventDefault();
        setXyFocus((current) => (current === "destination" ? "source" : "destination"));
        return;
      }

      if (event.key !== "ArrowDown" && event.key !== "ArrowUp") {
        return;
      }

      event.preventDefault();
      const direction = event.key === "ArrowDown" ? 1 : -1;
      if (xyFocus === "destination") {
        const currentIndex = Math.max(
          destinations.findIndex((item) => item.id === selectedDestinationId),
          0,
        );
        const next = destinations[(currentIndex + direction + destinations.length) % destinations.length];
        setSelectedDestinationId(next.id);
        return;
      }

      if (!filteredSources.length) {
        return;
      }

      const currentIndex = Math.max(
        filteredSources.findIndex((item) => item.id === selectedSourceId),
        0,
      );
      const next = filteredSources[(currentIndex + direction + filteredSources.length) % filteredSources.length];
      selectSource(next.id);
    }

    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [
    clearPreview,
    destinations,
    filteredSources,
    hasPreview,
    mode,
    selectedDestinationId,
    selectedSourceId,
    xyFocus,
  ]);

  function toggleLayer(layer: RoutingLayer) {
    setEnabledLayers((current) => ({
      ...current,
      [layer]: !current[layer],
    }));
  }

  function selectDestination(destinationId: string) {
    setSelectedDestinationId(destinationId);
    setXyFocus("source");
  }

  function previewRoute(destinationId: string, updates: Partial<Record<RoutingLayer, string | null>>) {
    setSelectedDestinationId(destinationId);
    setLayerPreview(destinationId, updates);
  }

  function selectSource(sourceId: string) {
    if (!selectedDestination) {
      return;
    }

    const source = sourceMap.get(sourceId);
    if (!source) {
      return;
    }

    const matchedSources = sources.filter((item) => item.label === source.label);
    const updates: Partial<Record<RoutingLayer, string | null>> = {};
    for (const layer of layerOrder) {
      if (!enabledLayers[layer]) {
        continue;
      }

      const layerMatch = matchedSources.find((item) => item.layer === layer);
      const route = selectedDestination.routes.find((item) => item.layer === layer);
      if (route?.isSupported && layerMatch) {
        updates[layer] = layerMatch.id;
      }
    }

    if (!Object.keys(updates).length && selectedDestination.routes.find((item) => item.layer === source.layer)?.isSupported) {
      updates[source.layer as RoutingLayer] = source.id;
    }

    setSelectedSourceId(sourceId);
    previewRoute(selectedDestination.id, updates);
  }

  function selectMatrixCrosspoint(destinationId: string, sourceId: string, layer: RoutingLayer) {
    setSelectedSourceId(sourceId);
    previewRoute(destinationId, { [layer]: sourceId });
  }

  async function takePreview() {
    if (!preview.destinationId || !hasPreview) {
      return;
    }

    const destination = destinationMap.get(preview.destinationId);
    if (!destination) {
      return;
    }

    const payload = {
      destinationId: destination.id,
      requestedBy: "broadcast-operator",
      activationMode: "Immediate" as const,
      videoSourceId: resolvePreviewForLayer(destination, preview.layers.Video, "Video"),
      audioSourceId: resolvePreviewForLayer(destination, preview.layers.Audio, "Audio"),
      ancillarySourceId: resolvePreviewForLayer(destination, preview.layers.Ancillary, "Ancillary"),
    };

    await connectMutation.mutateAsync(payload);
    clearPreview();
  }

  async function disconnectSelected() {
    if (!selectedDestination) {
      return;
    }

    await disconnectMutation.mutateAsync({
      destinationId: selectedDestination.id,
      requestedBy: "broadcast-operator",
      activationMode: "Immediate",
      disconnectVideo: enabledLayers.Video,
      disconnectAudio: enabledLayers.Audio,
      disconnectAncillary: enabledLayers.Ancillary,
    });
    clearPreview();
  }

  function syncFromGraphEdge(destinationId: string, sourceId?: string) {
    setSelectedDestinationId(destinationId);
    if (sourceId) {
      setSelectedSourceId(sourceId);
    }
  }

  function toggleTopologyLayer(layer: string) {
    setVisibleTopologyLayers((current) =>
      current.includes(layer) ? current.filter((item) => item !== layer) : [...current, layer],
    );
  }

  return {
    mode,
    setMode,
    matrix,
    topology,
    destinations,
    sources,
    filteredSources,
    selectedDestinationId,
    selectedSourceId,
    selectedDestination,
    inspectorRoutes,
    preview,
    hasPreview,
    previewEdges,
    enabledLayers,
    autoTake,
    xyFocus,
    sourceSearch,
    destinationSearch,
    visibleTopologyLayers,
    showInfrastructure,
    showOnlySelectedRoute,
    isLoading: matrixQuery.isLoading || topologyQuery.isLoading,
    error: matrixQuery.error ?? topologyQuery.error ?? connectMutation.error ?? disconnectMutation.error ?? null,
    isMutating: connectMutation.isPending || disconnectMutation.isPending,
    setAutoTake,
    setXyFocus,
    setSourceSearch,
    setDestinationSearch,
    setShowInfrastructure,
    setShowOnlySelectedRoute,
    toggleLayer,
    toggleTopologyLayer,
    selectDestination,
    selectSource,
    selectMatrixCrosspoint,
    clearPreview,
    takePreview,
    disconnectSelected,
    syncFromGraphEdge,
  };
}

function resolvePreviewForLayer(
  destination: RoutingDestination,
  previewSourceId: string | null,
  layer: RoutingLayer,
) {
  const route = destination.routes.find((item) => item.layer === layer);
  if (!route?.isSupported) {
    return undefined;
  }

  return previewSourceId ?? route.activeSourceId ?? undefined;
}

function buildPreviewEdges(
  destination: RoutingDestination | null,
  previewLayers: Record<RoutingLayer, string | null>,
  topology: TopologyGraph | undefined,
  sourceMap: Map<string, RoutingSource>,
) {
  if (!destination || !topology) {
    return [];
  }

  return layerOrder.flatMap((layer) => {
    const sourceId = previewLayers[layer];
    if (!sourceId) {
      return [];
    }

    const source = sourceMap.get(sourceId);
    if (!source) {
      return [];
    }

    return topology.senders
      .filter((sender) => sender.sourceGroupId === source.id)
      .map((sender) => ({
        id: `preview:${destination.id}:${layer}:${sender.id}`,
        source: sender.id,
        target: destination.id,
        state: "preview",
        path: sender.pathType,
        layer,
        redundancyGroup: sender.redundancyGroupId,
        isHealthy: sender.isHealthy,
        metadata: {
          senderGroupId: source.id,
        },
      }));
  });
}
