import { useEffect, useState } from "react";
import { useRegistry, useUpdateRegistry } from "../api/hooks";
import type { ControllerMode } from "../api/types";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

export function SettingsPage() {
  const registryQuery = useRegistry();
  const updateRegistry = useUpdateRegistry();

  const [name, setName] = useState("");
  const [baseUrl, setBaseUrl] = useState("");
  const [queryApiVersion, setQueryApiVersion] = useState("v1.3");
  const [connectionApiVersion, setConnectionApiVersion] = useState("v1.1");
  const [mode, setMode] = useState<ControllerMode>("Mock");
  const [isEnabled, setIsEnabled] = useState(true);

  useEffect(() => {
    if (!registryQuery.data) {
      return;
    }

    setName(registryQuery.data.name);
    setBaseUrl(registryQuery.data.baseUrl);
    setQueryApiVersion(registryQuery.data.queryApiVersion);
    setConnectionApiVersion(registryQuery.data.connectionApiVersion);
    setMode(registryQuery.data.mode);
    setIsEnabled(registryQuery.data.isEnabled);
  }, [registryQuery.data]);

  if (registryQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (registryQuery.isError) {
    return <ErrorPanel message={registryQuery.error.message} />;
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await updateRegistry.mutateAsync({
      name,
      baseUrl,
      queryApiVersion,
      connectionApiVersion,
      mode,
      isEnabled,
    });
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Settings" subtitle="Edit controller-owned registry settings and switch between live and mock mode." />
      <Card title="Registry Configuration" subtitle="The frontend never talks directly to NMOS endpoints.">
        <form className="settings-form" onSubmit={(event) => void handleSubmit(event)}>
          <label className="form-field">
            <span>Name</span>
            <input value={name} onChange={(event) => setName(event.target.value)} />
          </label>
          <label className="form-field">
            <span>Base URL</span>
            <input value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} />
          </label>
          <div className="form-grid">
            <label className="form-field">
              <span>Query API</span>
              <input value={queryApiVersion} onChange={(event) => setQueryApiVersion(event.target.value)} />
            </label>
            <label className="form-field">
              <span>Connection API</span>
              <input value={connectionApiVersion} onChange={(event) => setConnectionApiVersion(event.target.value)} />
            </label>
          </div>
          <div className="form-grid">
            <label className="form-field">
              <span>Mode</span>
              <select value={mode} onChange={(event) => setMode(event.target.value as ControllerMode)}>
                <option value="Mock">Mock</option>
                <option value="Live">Live</option>
              </select>
            </label>
            <label className="checkbox-field">
              <input type="checkbox" checked={isEnabled} onChange={(event) => setIsEnabled(event.target.checked)} />
              <span>Registry Enabled</span>
            </label>
          </div>
          <button className="primary-button" type="submit">
            Save Settings
          </button>
        </form>
      </Card>
    </div>
  );
}
