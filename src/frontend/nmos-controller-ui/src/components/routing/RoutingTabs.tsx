export type RoutingTabId = "router" | "topology" | "xy" | "inspector";

interface RoutingTabsProps {
  activeTab: RoutingTabId;
  onChange: (tab: RoutingTabId) => void;
}

const tabs: Array<{ id: RoutingTabId; label: string; description: string }> = [
  { id: "router", label: "Router", description: "Fast operational matrix routing." },
  { id: "topology", label: "Topology", description: "Engineering view of nodes, paths, and redundancy." },
  { id: "xy", label: "XY Panel", description: "Destination-first switching workflow." },
  { id: "inspector", label: "Inspector", description: "Selected route state and redundancy summary." },
];

export function RoutingTabs({ activeTab, onChange }: RoutingTabsProps) {
  return (
    <div className="routing-tabs" role="tablist" aria-label="Routing workspaces">
      {tabs.map((tab) => (
        <button
          key={tab.id}
          role="tab"
          aria-selected={activeTab === tab.id}
          className={activeTab === tab.id ? "routing-tab is-active" : "routing-tab"}
          type="button"
          onClick={() => onChange(tab.id)}
        >
          <strong>{tab.label}</strong>
          <small>{tab.description}</small>
        </button>
      ))}
    </div>
  );
}
