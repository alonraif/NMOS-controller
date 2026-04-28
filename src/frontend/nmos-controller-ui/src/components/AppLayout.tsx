import type { CSSProperties } from "react";
import { useEffect } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useHostResources, useRegistry, useTopology } from "../api/hooks";
import { StatusBadge } from "./StatusBadge";

const navItems = [
  { to: "/", label: "Dashboard" },
  { to: "/inventory", label: "Inventory" },
  { to: "/senders-receivers", label: "Senders/Receivers" },
  { to: "/routing", label: "Soft Panel" },
  { to: "/audit", label: "History" },
  { to: "/settings", label: "Settings" },
];

export function AppLayout() {
  const location = useLocation();
  const isDashboardRoute = location.pathname === "/";
  const registryQuery = useRegistry();
  const registry = registryQuery.data;
  const hostResourcesQuery = useHostResources();
  const hostResources = hostResourcesQuery.data;
  const topologyQuery = useTopology(true);
  const topology = topologyQuery.data;
  const refreshedAtLabel = topology?.refreshedAtUtc
    ? new Date(topology.refreshedAtUtc).toLocaleString()
    : "Unknown";

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      void topologyQuery.refetch();
    }, 10_000);
    return () => window.clearInterval(intervalId);
  }, [topologyQuery.refetch]);

  const registryServerHealth = topologyQuery.isLoading && !topology
    ? { label: "Checking", tone: "warning" as const, detail: "Checking live registry server connectivity..." }
    : topologyQuery.isError || topologyQuery.isRefetchError
      ? { label: "Offline", tone: "danger" as const, detail: "Registry server is not reachable right now." }
      : {
          label: "Online",
          tone: "success" as const,
          detail: `Last live check ${refreshedAtLabel}`,
        };

  const databaseHealth = registryQuery.isError
    ? { label: "Unreachable", tone: "danger" as const, detail: "Unable to read controller DB-backed settings." }
    : registryQuery.isLoading && !registry
      ? { label: "Checking", tone: "warning" as const, detail: "Checking controller database connectivity..." }
      : { label: "Healthy", tone: "success" as const, detail: "Controller database is reachable via API." };

  const snapshotHealth = topologyQuery.isLoading && !topology
    ? { label: "Checking", tone: "warning" as const, detail: "Loading live topology snapshot..." }
    : topologyQuery.isError || topologyQuery.isRefetchError
      ? { label: "Unavailable", tone: "danger" as const, detail: "Live topology data is currently unavailable." }
      : {
          label: "Healthy",
          tone: "success" as const,
          detail: `Refreshed ${refreshedAtLabel}`,
        };

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand-block">
          <p className="brand-kicker">LiveU Customer Success</p>
          <h1>NMOS Controller</h1>
          <p className="brand-subtitle">ST 2110 routing, topology, and validation.</p>
        </div>
        <nav className="nav">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
              className={({ isActive }) => `nav-link${isActive ? " is-active" : ""}`}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="content-shell">
        <div className={`content-top-right${isDashboardRoute ? " is-dashboard" : ""}`}>
          {isDashboardRoute ? (
            <section className="sidebar-card registry-panel host-resources-card">
              <div className="sidebar-label">Host Resources</div>
              {hostResources ? (
                <div className="host-resources-panel">
                  <div className="host-gauges">
                    <Gauge
                      label="CPU Total in use"
                      percent={hostResources.cpuTotalPercent}
                      value={formatPercent(hostResources.cpuTotalPercent)}
                    />
                    <Gauge
                      label="Memory Total in use"
                      percent={resolveMemoryInUsePercent(hostResources.memoryAvailableBytes, hostResources.memoryTotalBytes)}
                      value={formatPercent(
                        resolveMemoryInUsePercent(hostResources.memoryAvailableBytes, hostResources.memoryTotalBytes),
                      )}
                    />
                  </div>
                  <div className="host-resource-lines">
                    <p className="muted-copy">CPU Used by NMOS Controller: {formatPercent(hostResources.cpuUsedByControllerPercent)}</p>
                    <p className="muted-copy">Memory Used by NMOS Controller: {formatBytes(hostResources.memoryUsedByControllerBytes)}</p>
                  </div>
                </div>
              ) : hostResourcesQuery.isError ? (
                <p className="muted-copy">Host resource telemetry unavailable: {hostResourcesQuery.error.message}</p>
              ) : (
                <p className="muted-copy">Loading host resource telemetry...</p>
              )}
            </section>
          ) : null}
          {isDashboardRoute ? (
            <div className="content-top-right-group">
              <section className="sidebar-card registry-panel">
                <div className="sidebar-label">Registry Server</div>
                <div className="sidebar-row">
                  <strong>{registry?.name ?? "Unavailable"}</strong>
                  <StatusBadge tone={registryServerHealth.tone}>{registryServerHealth.label}</StatusBadge>
                </div>
                <small>{registry?.baseUrl ?? "No registry configured"}</small>
                <small>{registryServerHealth.detail}</small>
              </section>
              <section className="sidebar-card registry-panel">
                <div className="sidebar-label">Database</div>
                <div className="sidebar-row">
                  <strong>PostgreSQL</strong>
                  <StatusBadge tone={databaseHealth.tone}>{databaseHealth.label}</StatusBadge>
                </div>
                <small>{databaseHealth.detail}</small>
              </section>
              <section className="sidebar-card registry-panel">
                <div className="sidebar-label">Live Snapshot</div>
                <strong>Topology</strong>
                <div className="sidebar-metrics">
                  <span>{topology?.senders.length ?? "-"} senders</span>
                  <span>{topology?.receivers.length ?? "-"} receivers</span>
                  <span>{topology?.nodes.length ?? "-"} nodes</span>
                </div>
                <small>{snapshotHealth.detail}</small>
              </section>
            </div>
          ) : null}
        </div>
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function formatPercent(value: number): string {
  return `${value.toFixed(2)}%`;
}

function formatBytes(value: number): string {
  if (value <= 0) {
    return "0 B";
  }

  const units = ["B", "KB", "MB", "GB", "TB"];
  let unitIndex = 0;
  let scaled = value;

  while (scaled >= 1024 && unitIndex < units.length - 1) {
    scaled /= 1024;
    unitIndex += 1;
  }

  const decimals = scaled >= 10 || unitIndex === 0 ? 0 : 1;
  return `${scaled.toFixed(decimals)} ${units[unitIndex]}`;
}

function resolveMemoryInUsePercent(available: number, total: number): number {
  if (total <= 0) {
    return 0;
  }

  const availablePercent = Math.max(0, Math.min(100, (available / total) * 100));
  return Math.max(0, Math.min(100, 100 - availablePercent));
}

interface GaugeProps {
  label: string;
  percent: number;
  value: string;
}

function Gauge({ label, percent, value }: GaugeProps) {
  const normalizedPercent = Math.max(0, Math.min(100, percent));
  const style = { "--gauge-value": `${normalizedPercent}%` } as CSSProperties;

  return (
    <div className="resource-gauge">
      <div className="resource-gauge-ring" style={style}>
        <div className="resource-gauge-core">{value}</div>
      </div>
      <div className="resource-gauge-label">{label}</div>
    </div>
  );
}
