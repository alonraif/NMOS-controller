import { Link } from "react-router-dom";
import { useAudit, useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";

export function DashboardPage() {
  const topologyQuery = useTopology();
  const auditQuery = useAudit(6);

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

  return (
    <div className="stack-xl">
      <PageHeader
        title="Dashboard"
        subtitle="Current registry status, controller topology density, and recent operator actions."
      />

      <div className="stats-grid">
        <Card>
          <div className="stat-label">Nodes</div>
          <div className="stat-value">{topology.nodes.length}</div>
        </Card>
        <Card>
          <div className="stat-label">Devices</div>
          <div className="stat-value">{topology.devices.length}</div>
        </Card>
        <Card>
          <div className="stat-label">Senders</div>
          <div className="stat-value">{topology.senders.length}</div>
        </Card>
        <Card>
          <div className="stat-label">Connected Receivers</div>
          <div className="stat-value">{connectedReceivers}</div>
        </Card>
      </div>

      <div className="two-column">
        <Card title="Registry Snapshot" subtitle="Current controller view of the external registry.">
          <div className="stack">
            <div className="inline-status">
              <StatusBadge tone="success">Live</StatusBadge>
              <StatusBadge tone={topology.registry.isEnabled ? "success" : "danger"}>
                {topology.registry.isEnabled ? "Enabled" : "Disabled"}
              </StatusBadge>
            </div>
            <p className="mono">{topology.registry.baseUrl}</p>
            <p className="muted-copy">
              Query {topology.registry.queryApiVersion} / Connection {topology.registry.connectionApiVersion}
            </p>
            <Link className="text-link" to="/registry">
              Open registry status
            </Link>
          </div>
        </Card>

        <Card title="Recent Audit" subtitle="Latest controller actions and validations.">
          <div className="stack">
            {auditQuery.data?.map((entry) => (
              <div key={entry.id} className="timeline-row">
                <div>
                  <strong>{entry.summary}</strong>
                  <p>{entry.actor}</p>
                </div>
                <time>{new Date(entry.occurredAtUtc).toLocaleString()}</time>
              </div>
            ))}
            <Link className="text-link" to="/audit">
              Open full audit log
            </Link>
          </div>
        </Card>
      </div>

      <Card title="Route Readiness" subtitle="Fast access to the operator workflows used most often.">
        <div className="action-grid">
          <Link to="/routing" className="feature-tile">
            <strong>Routing Matrix</strong>
            <p>Validate and execute sender to receiver changes.</p>
          </Link>
          <Link to="/receivers" className="feature-tile">
            <strong>Receiver Inspection</strong>
            <p>Inspect active, staged, constraints, and disconnect state.</p>
          </Link>
          <Link to="/presets" className="feature-tile">
            <strong>Presets / Salvos</strong>
            <p>Capture repeatable routing actions for demo or operations.</p>
          </Link>
        </div>
      </Card>
    </div>
  );
}
