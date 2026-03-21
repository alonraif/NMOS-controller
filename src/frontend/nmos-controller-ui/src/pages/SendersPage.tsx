import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useSenders } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";
import { StatusBadge } from "../components/StatusBadge";

export function SendersPage() {
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<"label" | "transport">("label");
  const sendersQuery = useSenders();

  const senders = useMemo(() => {
    const source = sendersQuery.data ?? [];
    return [...source]
      .filter((sender) => {
        const term = search.toLowerCase();
        return sender.label.toLowerCase().includes(term) || sender.id.toLowerCase().includes(term);
      })
      .sort((left, right) => {
        if (sortBy === "transport") {
          return left.transport.localeCompare(right.transport) || left.label.localeCompare(right.label);
        }
        return left.label.localeCompare(right.label);
      });
  }, [search, sendersQuery.data, sortBy]);

  if (sendersQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (sendersQuery.isError) {
    return <ErrorPanel message={sendersQuery.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Senders" subtitle="Inspect available senders, transport file exposure, and current receiver subscriptions." />
      <Card
        title="Sender Inventory"
        subtitle="Search and sort for routing operations."
        actions={
          <div className="toolbar">
            <SearchInput value={search} onChange={setSearch} placeholder="Search senders" />
            <label className="compact-select">
              <span>Sort</span>
              <select value={sortBy} onChange={(event) => setSortBy(event.target.value as "label" | "transport")}>
                <option value="label">Label</option>
                <option value="transport">Transport</option>
              </select>
            </label>
          </div>
        }
      >
        <div className="resource-grid">
          {senders.map((sender) => (
            <article key={sender.id} className="resource-card">
              <div className="resource-card-header">
                <div>
                  <Link className="text-link" to={`/resources/${sender.id}`}>
                    {sender.label}
                  </Link>
                  <p className="mono">{sender.id}</p>
                </div>
                <StatusBadge tone={sender.subscribedReceiverId ? "success" : "muted"}>
                  {sender.subscribedReceiverId ? "Subscribed" : "Idle"}
                </StatusBadge>
              </div>
              <div className="resource-meta">
                <span>{sender.transport}</span>
                <span>{sender.format.mediaType ?? sender.format.format}</span>
              </div>
              <p className="muted-copy">Receiver: {sender.subscribedReceiverId ?? "None"}</p>
              <p className="muted-copy">Transport File: {sender.transportFile ? sender.transportFile.contentType : "Unavailable"}</p>
            </article>
          ))}
        </div>
      </Card>
    </div>
  );
}
