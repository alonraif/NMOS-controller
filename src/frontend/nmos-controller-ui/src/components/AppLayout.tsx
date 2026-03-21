import { NavLink, Outlet } from "react-router-dom";
import { useRegistry, useTopology } from "../api/hooks";
import { StatusBadge } from "./StatusBadge";

const navItems = [
  { to: "/", label: "Dashboard" },
  { to: "/registry", label: "Registry" },
  { to: "/inventory", label: "Inventory" },
  { to: "/senders", label: "Senders" },
  { to: "/receivers", label: "Receivers" },
  { to: "/routing", label: "Routing" },
  { to: "/presets", label: "Presets" },
  { to: "/audit", label: "Audit" },
  { to: "/settings", label: "Settings" },
];

export function AppLayout() {
  const { data: registry } = useRegistry();
  const { data: topology } = useTopology();

  return (
    <div className="shell">
      <aside className="sidebar">
        <div className="brand-block">
          <p className="brand-kicker">Broadcast Control</p>
          <h1>NMOS Controller</h1>
          <p className="brand-subtitle">ST 2110 routing, topology, validation, and presets.</p>
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
        <section className="sidebar-card">
          <div className="sidebar-label">Registry Mode</div>
          <div className="sidebar-row">
            <strong>{registry?.name ?? "Unavailable"}</strong>
            <StatusBadge tone={registry?.mode === "Mock" ? "info" : "success"}>
              {registry?.mode ?? "Unknown"}
            </StatusBadge>
          </div>
          <small>{registry?.baseUrl ?? "No registry configured"}</small>
        </section>
        <section className="sidebar-card">
          <div className="sidebar-label">Live Snapshot</div>
          <div className="sidebar-metrics">
            <span>{topology?.senders.length ?? 0} senders</span>
            <span>{topology?.receivers.length ?? 0} receivers</span>
            <span>{topology?.nodes.length ?? 0} nodes</span>
          </div>
        </section>
      </aside>
      <div className="content-shell">
        <header className="topbar">
          <div>
            <p className="page-kicker">Operations</p>
            <h2>Operator Workspace</h2>
          </div>
          <div className="topbar-status">
            <StatusBadge tone={registry?.isEnabled ? "success" : "danger"}>
              {registry?.isEnabled ? "Enabled" : "Disabled"}
            </StatusBadge>
            <StatusBadge tone={registry?.mode === "Mock" ? "warning" : "info"}>
              {registry?.mode === "Mock" ? "Mock Demo Data" : "Live NMOS"}
            </StatusBadge>
          </div>
        </header>
        <main className="page-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
