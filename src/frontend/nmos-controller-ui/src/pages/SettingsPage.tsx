import { Link } from "react-router-dom";
import { getApiRoot } from "../api/runtimeConfig";
import { useRegistry } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { KeyValueList } from "../components/KeyValueList";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

export function SettingsPage() {
  const registryQuery = useRegistry();

  if (registryQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (registryQuery.isError) {
    return <ErrorPanel message={registryQuery.error.message} />;
  }

  const registry = registryQuery.data;
  const storedUiUrl = window.localStorage.getItem("nmos_controller_ui_url");
  const controllerUiUrl = storedUiUrl ?? window.location.origin;
  const controllerApiUrl = getApiRoot();

  const items = [
    { label: "Controller UI URL", value: controllerUiUrl },
    { label: "Controller API URL", value: controllerApiUrl },
    { label: "Registry Name", value: registry.name },
    { label: "NMOS IS-04 Base URL", value: registry.baseUrl },
    { label: "Query API Version", value: registry.queryApiVersion },
    { label: "Connection API Version", value: registry.connectionApiVersion },
    { label: "Registry Enabled", value: registry.isEnabled ? "Yes" : "No" },
    { label: "IS-05 Connection Base URL", value: registry.connectionBaseUrl ?? "Not set" },
    { label: "IS-05 Connection Base URLs", value: registry.connectionBaseUrls ?? "Not set" },
    { label: "Initial Setup Completed", value: registry.initialSetupCompleted ? "Yes" : "No" },
    { label: "Last Updated (UTC)", value: new Date(registry.updatedAtUtc).toISOString() },
  ];

  return (
    <div className="stack-xl">
      <PageHeader title="Settings" subtitle="System configuration summary." />
      <Card title="System Settings" subtitle="Read-only values currently active in this deployment.">
        <div className="stack">
          <KeyValueList items={items} />
          <div className="button-row">
            <Link className="ghost-button" to="/setup-wizard">
              Run Setup Wizard
            </Link>
          </div>
        </div>
      </Card>
    </div>
  );
}
