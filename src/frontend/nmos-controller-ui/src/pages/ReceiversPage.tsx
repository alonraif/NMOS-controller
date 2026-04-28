import { useMemo, useState } from "react";
import { useReceivers, useSenders } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";
import { StatusBadge } from "../components/StatusBadge";
import { ConnectionDrawer } from "../features/routing/ConnectionDrawer";

export function ReceiversPage() {
  const [search, setSearch] = useState("");
  const [selectedReceiverId, setSelectedReceiverId] = useState<string | null>(null);
  const receiversQuery = useReceivers();
  const sendersQuery = useSenders();

  const receivers = useMemo(() => {
    const source = receiversQuery.data ?? [];
    return [...source]
      .filter((receiver) => {
        const term = search.toLowerCase();
        return receiver.label.toLowerCase().includes(term) || receiver.id.toLowerCase().includes(term);
      })
      .sort((left, right) => left.label.localeCompare(right.label));
  }, [receiversQuery.data, search]);

  const selectedReceiver = useMemo(
    () => (selectedReceiverId ? (receiversQuery.data ?? []).find((receiver) => receiver.id === selectedReceiverId) ?? null : null),
    [receiversQuery.data, selectedReceiverId],
  );

  if (receiversQuery.isLoading || sendersQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (receiversQuery.isError) {
    return <ErrorPanel message={receiversQuery.error.message} />;
  }

  if (sendersQuery.isError) {
    return <ErrorPanel message={sendersQuery.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Receivers" subtitle="Inspect connection state and open the route editor for connect or disconnect actions." />
      <Card
        title="Receiver Inventory"
        subtitle="Active, staged, and constraints are available directly from each receiver card."
        actions={<SearchInput value={search} onChange={setSearch} placeholder="Search receivers" />}
      >
        <div className="resource-grid">
          {receivers.map((receiver) => (
            <article key={receiver.id} className="resource-card">
              <div className="resource-card-header">
                <div>
                  <strong>{receiver.label}</strong>
                  <p className="mono">{receiver.id}</p>
                </div>
                <div className="stack-sm">
                  <StatusBadge tone={receiver.active.senderId ? "success" : "muted"}>
                    {receiver.active.senderId ? "Connected" : "Disconnected"}
                  </StatusBadge>
                  <StatusBadge tone={receiver.isConnectable ? "info" : "danger"}>
                    {receiver.isConnectable ? "Connectable" : "Locked"}
                  </StatusBadge>
                </div>
              </div>
              <div className="resource-meta">
                <span>{receiver.transport}</span>
                <span>{receiver.format.mediaType ?? receiver.format.format}</span>
              </div>
              <p className="muted-copy">Active Sender: {receiver.active.senderId ?? "None"}</p>
              <p className="muted-copy">Staged Sender: {receiver.staged.senderId ?? "None"}</p>
              <button className="primary-button" type="button" onClick={() => setSelectedReceiverId(receiver.id)}>
                Open Route Editor
              </button>
            </article>
          ))}
        </div>
      </Card>
      <ConnectionDrawer
        open={Boolean(selectedReceiver)}
        receiver={selectedReceiver}
        senders={sendersQuery.data ?? []}
        onClose={() => setSelectedReceiverId(null)}
      />
    </div>
  );
}
