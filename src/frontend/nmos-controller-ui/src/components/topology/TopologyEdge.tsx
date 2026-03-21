import type { CSSProperties } from "react";
import { BaseEdge, EdgeLabelRenderer, getBezierPath, type EdgeProps } from "reactflow";

export function TopologyEdge({ id, sourceX, sourceY, targetX, targetY, sourcePosition, targetPosition, data, selected }: EdgeProps) {
  const [edgePath, labelX, labelY] = getBezierPath({
    sourceX,
    sourceY,
    targetX,
    targetY,
    sourcePosition,
    targetPosition,
  });

  const classes = [
    "topology-edge-path",
    data?.state ? `is-${String(data.state).toLowerCase()}` : "",
    data?.path === "B" ? "is-secondary" : "",
    data?.isHealthy === false ? "is-unhealthy" : "",
    selected ? "is-selected" : "",
  ]
    .filter(Boolean)
    .join(" ");

  const style: CSSProperties = {
    strokeWidth: data?.state === "active" ? 2.8 : data?.state === "preview" ? 2.6 : 2.2,
    stroke:
      data?.state === "preview"
        ? "#ffc75c"
        : data?.state === "staged"
          ? "#8dc9ff"
          : data?.state === "active"
            ? "#57cb9b"
            : "rgba(106, 136, 173, 0.38)",
    strokeDasharray: data?.path === "B" ? "6 6" : undefined,
    opacity: data?.isHealthy === false ? 0.42 : 1,
  };

  return (
    <>
      <BaseEdge id={id} path={edgePath} style={style} />
      <EdgeLabelRenderer>
        <div
          className={classes ? `topology-edge-label ${classes}` : "topology-edge-label"}
          style={{
            transform: `translate(-50%, -50%) translate(${labelX}px, ${labelY}px)`,
          }}
        >
          {String(data?.layer ?? "")} {String(data?.path ?? "")}
        </div>
      </EdgeLabelRenderer>
    </>
  );
}
