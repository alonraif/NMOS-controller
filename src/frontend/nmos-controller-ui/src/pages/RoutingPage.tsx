import { useMemo, useState } from "react";
import { useReceivers, useSenders } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";
import { StatusBadge } from "../components/StatusBadge";
import { ConnectionDrawer } from "../features/routing/ConnectionDrawer";
import type { NmosReceiver } from "../api/types";

export function RoutingPage() {
  const [search, setSearch] = useState("");
  const [selectedReceiver, setSelectedReceiver] = useState<NmosReceiver | null>(null);
  const sendersQuery = useSenders();
  const receiversQuery = useReceivers();

  const matrixRows = useMemo(() => {
    const senders = sendersQuery.data ?? [];
    return (receiversQuery.data ?? [])
      .filter((receiver) => receiver.label.toLowerCase().includes(search.toLowerCase()) || receiver.id.toLowerCase().includes(search.toLowerCase()))
      .map((receiver) => ({
        receiver,
        currentSender: senders.find((sender) => sender.id === receiver.active.senderId) ?? null,
      }));
  }, [receiversQuery.data, search, sendersQuery.data]);

  if (sendersQuery.isLoading || receiversQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (sendersQuery.isError) {
    return <ErrorPanel message={sendersQuery.error.message} />;
  }

  if (receiversQuery.isError) {
    return <ErrorPanel message={receiversQuery.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Routing Matrix" subtitle="Receiver-oriented routing workflow with validation and connection editor entry points." />
      <Card
        title="Receiver Routing"
        subtitle="The UI exposes immediate activation first, while scheduled activation remains present in the model."
        actions={<SearchInput value={search} onChange={setSearch} placeholder="Search routing rows" />}
      >
        <table className="data-table">
          <thead>
            <tr>
              <th>Receiver</th>
              <th>Current Sender</th>
              <th>Transport</th>
              <th>Status</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {matrixRows.map(({ receiver, currentSender }) => (
              <tr key={receiver.id}>
                <td>
                  <strong>{receiver.label}</strong>
                  <div className="table-subtext mono">{receiver.id}</div>
                </td>
                <td>{currentSender?.label ?? "Disconnected"}</td>
                <td>{receiver.transport}</td>
                <td>
                  <StatusBadge tone={receiver.active.senderId ? "success" : "muted"}>
                    {receiver.active.senderId ? "Connected" : "Disconnected"}
                  </StatusBadge>
                </td>
                <td>
                  <button className="ghost-button" type="button" onClick={() => setSelectedReceiver(receiver)}>
                    Route
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>

      <ConnectionDrawer
        open={Boolean(selectedReceiver)}
        receiver={selectedReceiver}
        senders={sendersQuery.data ?? []}
        onClose={() => setSelectedReceiver(null)}
      />
    </div>
  );
}
