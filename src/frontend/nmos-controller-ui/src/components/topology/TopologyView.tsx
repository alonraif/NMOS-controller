import { useMemo } from "react";
import ReactFlow, { Background, Controls, MarkerType } from "reactflow";
import "reactflow/dist/style.css";
import type { TopologyGraph, TopologyRouteEdge } from "../../api/types";
import { TopologyEdge } from "./TopologyEdge";
import { TopologyNode } from "./TopologyNode";

interface TopologyViewProps {
  topology: TopologyGraph;
  previewEdges: TopologyRouteEdge[];
  selectedDestinationId: string | null;
  selectedSourceId: string | null;
  visibleLayers: string[];
  showInfrastructure: boolean;
  showOnlySelectedRoute: boolean;
  onRouteSelect: (destinationId: string, sourceId?: string) => void;
}

export function TopologyView({
  topology,
  previewEdges,
  selectedDestinationId,
  selectedSourceId,
  visibleLayers,
  showInfrastructure,
  showOnlySelectedRoute,
  onRouteSelect,
}: TopologyViewProps) {
  const graph = useMemo(
    () =>
      buildGraph(topology, previewEdges, selectedDestinationId, selectedSourceId, visibleLayers, showInfrastructure, showOnlySelectedRoute),
    [previewEdges, selectedDestinationId, selectedSourceId, showInfrastructure, showOnlySelectedRoute, topology, visibleLayers],
  );

  return (
    <div className="topology-canvas">
      <ReactFlow
        fitView
        nodes={graph.nodes}
        edges={graph.edges}
        nodeTypes={{ topology: TopologyNode }}
        edgeTypes={{ topology: TopologyEdge }}
        onNodeClick={(_, node) => {
          const destinationId = String(node.data.destinationId ?? "");
          if (destinationId) {
            onRouteSelect(destinationId);
          }
        }}
        onEdgeClick={(_, edge) => {
          const destinationId = String(edge.data?.destinationId ?? "");
          const sourceId = String(edge.data?.senderGroupId ?? "");
          if (destinationId) {
            onRouteSelect(destinationId, sourceId || undefined);
          }
        }}
        defaultEdgeOptions={{ markerEnd: { type: MarkerType.ArrowClosed } }}
      >
        <Background gap={24} color="rgba(139, 180, 227, 0.12)" />
        <Controls />
      </ReactFlow>
    </div>
  );
}

function buildGraph(
  topology: TopologyGraph,
  previewEdges: TopologyRouteEdge[],
  selectedDestinationId: string | null,
  selectedSourceId: string | null,
  visibleLayers: string[],
  showInfrastructure: boolean,
  showOnlySelectedRoute: boolean,
) {
  const nodes = [
    ...topology.nodes.map((node, index) => ({
      id: node.id,
      type: "topology",
      position: { x: 0, y: index * 140 },
      data: { label: node.label, kind: "Node", meta: node.hostname ?? undefined },
      selectable: false,
    })),
    ...topology.devices.map((device, index) => ({
      id: device.id,
      type: "topology",
      position: { x: 240, y: index * 110 },
      data: { label: device.label, kind: "Device" },
      selectable: false,
    })),
    ...topology.senders.map((sender, index) => ({
      id: sender.id,
      type: "topology",
      position: { x: 560, y: index * 92 },
      data: {
        label: sender.label,
        kind: `${sender.signalType} ${sender.pathType}`,
        meta: sender.isHealthy ? sender.sourceGroupLabel : `${sender.sourceGroupLabel} degraded`,
      },
      selected: sender.sourceGroupId === selectedSourceId,
    })),
    ...topology.routingDestinations.map((destination, index) => ({
      id: destination.id,
      type: "topology",
      position: { x: 920, y: index * 180 },
      data: {
        label: destination.label,
        kind: "Destination",
        meta: destination.tags.join(" • "),
        destinationId: destination.id,
      },
      selected: destination.id === selectedDestinationId,
    })),
  ];

  const infraEdges = [
    ...topology.devices.map((device) => ({
      id: `infra:${device.nodeId}:${device.id}`,
      source: device.nodeId,
      target: device.id,
      type: "topology",
      data: { layer: "Infra", path: "", state: "active", destinationId: "" },
      selectable: false,
    })),
    ...topology.senders.map((sender) => ({
      id: `infra:${sender.deviceId}:${sender.id}`,
      source: sender.deviceId,
      target: sender.id,
      type: "topology",
      data: { layer: sender.signalType, path: sender.pathType, state: "active", destinationId: "" },
      selectable: false,
    })),
  ];

  const routeEdges = [...topology.routeEdges, ...previewEdges]
    .filter((edge) => visibleLayers.includes(edge.layer))
    .filter((edge) => !showOnlySelectedRoute || edge.target === selectedDestinationId || edge.metadata.senderGroupId === selectedSourceId)
    .map((edge) => ({
    id: edge.id,
    source: edge.source,
    target: edge.target,
    type: "topology",
    animated: edge.state === "preview" || edge.metadata.senderGroupId === selectedSourceId,
    data: {
      layer: edge.layer,
      path: edge.path,
      state: edge.state,
      isHealthy: edge.isHealthy,
      destinationId: edge.target,
      senderGroupId: edge.metadata.senderGroupId,
    },
    selected: edge.target === selectedDestinationId || edge.metadata.senderGroupId === selectedSourceId,
    }));

  return {
    nodes,
    edges: [...(showInfrastructure ? infraEdges : []), ...routeEdges],
  };
}
