import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useAudit, useReceivers, useSenders, useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";

export function DashboardPage() {
  const [historySearch, setHistorySearch] = useState("");
  const [historyFrom, setHistoryFrom] = useState("");
  const [historyTo, setHistoryTo] = useState("");
  const topologyQuery = useTopology();
  const sendersQuery = useSenders();
  const receiversQuery = useReceivers();
  const auditQuery = useAudit(50);
  const resourceLabelsById = useMemo(() => {
    const topology = topologyQuery.data;
    const senders = sendersQuery.data ?? [];
    const receivers = receiversQuery.data ?? [];
    if (!topology && senders.length === 0 && receivers.length === 0) {
      return new Map<string, string>();
    }

    return new Map<string, string>([
      ...(topology?.nodes ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.devices ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.sources ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.flows ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.senders ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.receivers ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.routingDestinations ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...senders.map((item) => [item.id.toLowerCase(), item.label] as const),
      ...receivers.map((item) => [item.id.toLowerCase(), item.label] as const),
    ]);
  }, [receiversQuery.data, sendersQuery.data, topologyQuery.data]);
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
      const activeSenderLabel = activeSenderId
        ? resourceLabelsById.get(activeSenderId.toLowerCase()) ?? activeSenderId
        : "Unknown sender";
      return `${receiver.label} <- ${activeSenderLabel}`;
    });
  const filteredHistoryEntries = (auditQuery.data ?? []).filter((entry) => {
    const fromUtc = historyFrom ? new Date(historyFrom) : null;
    const toUtc = historyTo ? new Date(historyTo) : null;
    const term = historySearch.trim().toLowerCase();
    const occurredAt = new Date(entry.occurredAtUtc);

    if (fromUtc && occurredAt < fromUtc) {
      return false;
    }
    if (toUtc && occurredAt > toUtc) {
      return false;
    }

    if (!term) {
      return true;
    }

    const summary = formatAuditSummary(entry.summary, resourceLabelsById).toLowerCase();
    return summary.includes(term);
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

      <Card className="dashboard-history-card" title="History" subtitle="Latest controller actions and validations.">
        <div className="history-panel">
          <div className="history-toolbar">
            <SearchInput value={historySearch} onChange={setHistorySearch} placeholder="Search history" />
            <label className="form-field">
              <span>From</span>
              <input type="datetime-local" value={historyFrom} onChange={(event) => setHistoryFrom(event.target.value)} />
            </label>
            <label className="form-field">
              <span>To</span>
              <input type="datetime-local" value={historyTo} onChange={(event) => setHistoryTo(event.target.value)} />
            </label>
          </div>
          <div className="stack audit-terminal history-list">
            {filteredHistoryEntries.map((entry) => (
              <div key={entry.id} className="audit-line">
                <time className="audit-ts">{new Date(entry.occurredAtUtc).toLocaleString()}</time>
                <span className="audit-prompt">$</span>
                <strong className="audit-summary">{formatAuditSummary(entry.summary, resourceLabelsById)}</strong>
              </div>
            ))}
            <Link className="text-link" to="/audit">
              Open full audit log
            </Link>
          </div>
        </div>
      </Card>
    </div>
  );
}

function formatAuditSummary(summary: string, labelsById: Map<string, string>): string {
  return summary.replace(/\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi, (id) => {
    return labelsById.get(id.toLowerCase()) ?? id;
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
