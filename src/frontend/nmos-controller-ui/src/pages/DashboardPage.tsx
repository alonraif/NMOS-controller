import { useMemo } from "react";
import { Link } from "react-router-dom";
import { useAudit, useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

export function DashboardPage() {
  const topologyQuery = useTopology();
  const auditQuery = useAudit(6);
  const resourceLabelsById = useMemo(() => {
    const topology = topologyQuery.data;
    if (!topology) {
      return new Map<string, string>();
    }

    return new Map<string, string>([
      ...topology.nodes.map((item) => [item.id, item.label] as const),
      ...topology.devices.map((item) => [item.id, item.label] as const),
      ...topology.sources.map((item) => [item.id, item.label] as const),
      ...topology.flows.map((item) => [item.id, item.label] as const),
      ...topology.senders.map((item) => [item.id, item.label] as const),
      ...topology.receivers.map((item) => [item.id, item.label] as const),
      ...topology.routingDestinations.map((item) => [item.id, item.label] as const),
    ]);
  }, [topologyQuery.data]);

  if (topologyQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (topologyQuery.isError) {
    return <ErrorPanel message={topologyQuery.error.message} />;
  }

  if (!topologyQuery.data) {
    return <ErrorPanel message="Topology data is unavailable." />;
  }

  const topology = topologyQuery.data;
  const connectedReceivers = topology.receivers.filter((receiver) => receiver.active.senderId).length;
  const nodeItems = topology.nodes.map((node) => (node.hostname ? `${node.label} (${node.hostname})` : node.label));
  const deviceItems = topology.devices.map((device) => device.label);
  const senderItems = topology.senders.map((sender) => sender.label);
  const connectedReceiverItems = topology.receivers
    .filter((receiver) => receiver.active.senderId)
    .map((receiver) => {
      const activeSenderId = receiver.active.senderId;
      const activeSenderLabel = activeSenderId ? resourceLabelsById.get(activeSenderId) ?? activeSenderId : "Unknown sender";
      return `${receiver.label} <- ${activeSenderLabel}`;
    });

  return (
    <div className="stack-xl">
      <PageHeader
        title="Dashboard"
        subtitle="Current registry status, controller topology density, and recent operator actions."
      />

      <div className="stats-grid">
        <HoverStatCard label="Nodes" value={topology.nodes.length} items={nodeItems} emptyLabel="No nodes available." />
        <HoverStatCard label="Devices" value={topology.devices.length} items={deviceItems} emptyLabel="No devices available." />
        <HoverStatCard label="Senders" value={topology.senders.length} items={senderItems} emptyLabel="No senders available." />
        <HoverStatCard
          label="Connected Receivers"
          value={connectedReceivers}
          items={connectedReceiverItems}
          emptyLabel="No connected receivers."
        />
      </div>

      <Card title="Recent Audit" subtitle="Latest controller actions and validations.">
        <div className="stack audit-terminal">
          {auditQuery.data?.map((entry) => (
            <div key={entry.id} className="audit-line">
              <time className="audit-ts">{new Date(entry.occurredAtUtc).toLocaleString()}</time>
              <span className="audit-prompt">$</span>
              <strong className="audit-summary">{formatAuditSummary(entry.summary, resourceLabelsById)}</strong>
              <span className="audit-actor">{resolveAuditLabel(entry.resourceId, entry.actor, resourceLabelsById)}</span>
            </div>
          ))}
          <Link className="text-link" to="/audit">
            Open full audit log
          </Link>
        </div>
      </Card>
    </div>
  );
}

function resolveAuditLabel(resourceId: string | null, fallbackActor: string, labelsById: Map<string, string>): string {
  if (!resourceId) {
    return fallbackActor;
  }

  return labelsById.get(resourceId) ?? resourceId;
}

function formatAuditSummary(summary: string, labelsById: Map<string, string>): string {
  return summary.replace(/\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi, (id) => {
    return labelsById.get(id) ?? id;
  });
}

interface HoverStatCardProps {
  label: string;
  value: number;
  items: string[];
  emptyLabel: string;
}

function HoverStatCard({ label, value, items, emptyLabel }: HoverStatCardProps) {
  return (
    <Card>
      <div className="stat-hover-card" tabIndex={0}>
        <div className="stat-label">{label}</div>
        <div className="stat-value">{value}</div>
        <div className="stat-hover-popup">
          <p className="stat-hover-title">{label}</p>
          {items.length > 0 ? (
            <ul className="stat-hover-list">
              {items.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          ) : (
            <p className="muted-copy">{emptyLabel}</p>
          )}
        </div>
      </div>
    </Card>
  );
}
