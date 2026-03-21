import { useRegistry } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { KeyValueList } from "../components/KeyValueList";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";

export function RegistryStatusPage() {
  const registryQuery = useRegistry();

  if (registryQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (registryQuery.isError) {
    return <ErrorPanel message={registryQuery.error.message} />;
  }

  if (!registryQuery.data) {
    return <ErrorPanel message="Registry settings are unavailable." />;
  }

  const registry = registryQuery.data;

  return (
    <div className="stack-xl">
      <PageHeader title="Registry Status" subtitle="Resolved controller registry configuration and operating mode." />
      <Card title={registry.name} subtitle="Controller-owned registry settings.">
        <div className="inline-status">
          <StatusBadge tone={registry.mode === "Mock" ? "warning" : "success"}>{registry.mode}</StatusBadge>
          <StatusBadge tone={registry.isEnabled ? "success" : "danger"}>
            {registry.isEnabled ? "Enabled" : "Disabled"}
          </StatusBadge>
        </div>
        <KeyValueList
          items={[
            { label: "Base URL", value: registry.baseUrl },
            { label: "Query API", value: registry.queryApiVersion },
            { label: "Connection API", value: registry.connectionApiVersion },
            { label: "Updated", value: new Date(registry.updatedAtUtc).toLocaleString() },
          ]}
        />
      </Card>
    </div>
  );
}
