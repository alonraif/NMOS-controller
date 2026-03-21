import { useMemo, useState } from "react";
import { useAudit } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";

export function AuditLogPage() {
  const [search, setSearch] = useState("");
  const auditQuery = useAudit(250);

  const entries = useMemo(() => {
    const source = auditQuery.data ?? [];
    return source.filter((entry) => {
      const term = search.toLowerCase();
      return (
        entry.summary.toLowerCase().includes(term) ||
        entry.actor.toLowerCase().includes(term) ||
        (entry.resourceId ?? "").toLowerCase().includes(term)
      );
    });
  }, [auditQuery.data, search]);

  if (auditQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (auditQuery.isError) {
    return <ErrorPanel message={auditQuery.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Audit Log" subtitle="Recent controller actions, route validations, preset execution, and operator events." />
      <Card
        title="Audit Entries"
        subtitle="Search by actor, summary, or resource ID."
        actions={<SearchInput value={search} onChange={setSearch} placeholder="Search audit log" />}
      >
        <table className="data-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Actor</th>
              <th>Action</th>
              <th>Resource</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.id}>
                <td>{new Date(entry.occurredAtUtc).toLocaleString()}</td>
                <td>{entry.actor}</td>
                <td>{entry.summary}</td>
                <td className="mono">{entry.resourceId ?? "-"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </Card>
    </div>
  );
}
