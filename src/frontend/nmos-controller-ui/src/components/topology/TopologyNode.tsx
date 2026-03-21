import type { NodeProps } from "reactflow";
import { Handle, Position } from "reactflow";

export function TopologyNode({ data, selected }: NodeProps<{ label: string; kind: string; meta?: string }>) {
  return (
    <div className={selected ? "topology-node is-selected" : "topology-node"}>
      <Handle type="target" position={Position.Left} />
      <div className="topology-node-kind">{data.kind}</div>
      <strong>{data.label}</strong>
      {data.meta ? <small>{data.meta}</small> : null}
      <Handle type="source" position={Position.Right} />
    </div>
  );
}
