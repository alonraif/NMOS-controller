import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { EmptyState } from "../components/EmptyState";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";

type InventoryRow = {
  id: string;
  label: string;
  type: string;
  related: string;
};

export function InventoryPage() {
  const [search, setSearch] = useState("");
  const topologyQuery = useTopology();

  const rows = useMemo<InventoryRow[]>(() => {
    const topology = topologyQuery.data;
    if (!topology) {
      return [];
    }

    return [
      ...topology.nodes.map((item) => ({ id: item.id, label: item.label, type: "Node", related: item.hostname ?? "-" })),
      ...topology.devices.map((item) => ({ id: item.id, label: item.label, type: "Device", related: item.nodeId })),
      ...topology.sources.map((item) => ({ id: item.id, label: item.label, type: "Source", related: item.deviceId })),
      ...topology.flows.map((item) => ({ id: item.id, label: item.label, type: "Flow", related: item.sourceId })),
      ...topology.senders.map((item) => ({ id: item.id, label: item.label, type: "Sender", related: item.deviceId })),
      ...topology.receivers.map((item) => ({ id: item.id, label: item.label, type: "Receiver", related: item.deviceId })),
    ]
      .filter((row) => row.label.toLowerCase().includes(search.toLowerCase()) || row.id.toLowerCase().includes(search.toLowerCase()))
      .sort((left, right) => left.type.localeCompare(right.type) || left.label.localeCompare(right.label));
  }, [search, topologyQuery.data]);

  if (topologyQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (topologyQuery.isError) {
    return <ErrorPanel message={topologyQuery.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Inventory / Topology" subtitle="Normalized graph view across nodes, devices, sources, flows, senders, and receivers." />
      <Card
        title="Inventory"
        subtitle="Search by label or ID, then open a resource detail page."
        actions={<SearchInput value={search} onChange={setSearch} placeholder="Search resources" />}
      >
        {rows.length === 0 ? (
          <EmptyState title="No matching resources" description="Adjust the search term or refresh the topology." />
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Label</th>
                <th>ID</th>
                <th>Related</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={`${row.type}-${row.id}`}>
                  <td>{row.type}</td>
                  <td>
                    <Link className="table-link" to={`/resources/${row.id}`}>
                      {row.label}
                    </Link>
                  </td>
                  <td className="mono">{row.id}</td>
                  <td className="mono">{row.related}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Card>
    </div>
  );
}
