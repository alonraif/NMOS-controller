import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { setApiRoot } from "../api/runtimeConfig";
import { useRegistry, useTopology, useUpdateRegistry } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

const UI_URL_STORAGE_KEY = "nmos_controller_ui_url";

function normalizeUrl(value: string): string {
  return value.trim().replace(/\/+$/, "");
}

export function SetupWizardPage() {
  const navigate = useNavigate();
  const registryQuery = useRegistry();
  const topologyQuery = useTopology(true);
  const updateRegistry = useUpdateRegistry();

  const [uiUrl, setUiUrl] = useState("");
  const [apiUrl, setApiUrl] = useState("");
  const [registryName, setRegistryName] = useState("");
  const [baseUrl, setBaseUrl] = useState("");
  const [submitError, setSubmitError] = useState<string | null>(null);

  const hasInitialData = Boolean(registryQuery.data);
  const isInitialSetup = hasInitialData && !registryQuery.data.initialSetupCompleted;

  useEffect(() => {
    if (!registryQuery.data) {
      return;
    }

    setRegistryName(registryQuery.data.name);
    setBaseUrl(registryQuery.data.baseUrl);
    setApiUrl("");

    const storedUiUrl = window.localStorage.getItem(UI_URL_STORAGE_KEY);
    if (storedUiUrl) {
      setUiUrl(storedUiUrl);
      return;
    }

    setUiUrl(window.location.origin);
  }, [registryQuery.data]);

  const isSubmitting = updateRegistry.isPending;
  const isBusy = registryQuery.isLoading || isSubmitting;
  const isReady = useMemo(() => {
    if (!topologyQuery.data) {
      return false;
    }

    return Boolean(topologyQuery.data.registry?.id);
  }, [topologyQuery.data]);

  if (registryQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (registryQuery.isError) {
    return <ErrorPanel message={registryQuery.error.message} />;
  }

  async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitError(null);

    const normalizedUiUrl = normalizeUrl(uiUrl);
    const normalizedApiUrl = normalizeUrl(apiUrl);
    const normalizedName = registryName.trim();
    const normalizedBaseUrl = normalizeUrl(baseUrl);

    if (!normalizedUiUrl || !normalizedName || !normalizedBaseUrl) {
      setSubmitError("UI URL, Registry Name, and NMOS IS-04 Base URL are required.");
      return;
    }

    try {
      new URL(normalizedUiUrl);
      if (normalizedApiUrl) {
        new URL(normalizedApiUrl);
      }
      new URL(normalizedBaseUrl);
    } catch {
      setSubmitError("Please provide valid absolute URLs (http:// or https://).");
      return;
    }

    try {
      await updateRegistry.mutateAsync({
        name: normalizedName,
        baseUrl: normalizedBaseUrl,
        connectionBaseUrl: registryQuery.data?.connectionBaseUrl ?? null,
        connectionBaseUrls: registryQuery.data?.connectionBaseUrls ?? null,
        queryApiVersion: registryQuery.data?.queryApiVersion ?? "v1.3",
        connectionApiVersion: registryQuery.data?.connectionApiVersion ?? "v1.1",
        isEnabled: true,
        initialSetupCompleted: true,
      });

      if (normalizedApiUrl) {
        setApiRoot(normalizedApiUrl);
      } else {
        setApiRoot("/api/v1");
      }

      window.localStorage.setItem(UI_URL_STORAGE_KEY, normalizedUiUrl);
      await topologyQuery.refetch();
      navigate("/settings", { replace: true });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Failed to save wizard settings.";
      setSubmitError(message);
    }
  }

  async function handleUseExistingSettings() {
    if (!registryQuery.data) {
      return;
    }

    setSubmitError(null);
    try {
      await updateRegistry.mutateAsync({
        name: registryQuery.data.name,
        baseUrl: registryQuery.data.baseUrl,
        connectionBaseUrl: registryQuery.data.connectionBaseUrl,
        connectionBaseUrls: registryQuery.data.connectionBaseUrls,
        queryApiVersion: registryQuery.data.queryApiVersion,
        connectionApiVersion: registryQuery.data.connectionApiVersion,
        isEnabled: true,
        initialSetupCompleted: true,
      });

      navigate("/settings", { replace: true });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Failed to keep existing settings.";
      setSubmitError(message);
    }
  }

  function handleBypassWizard() {
    window.localStorage.setItem("nmos_controller_wizard_bypass", "true");
    navigate("/settings", { replace: true });
  }

  return (
    <div className="stack-xl">
      <PageHeader
        title="Setup Wizard"
        subtitle={
          isInitialSetup
            ? "Complete initial controller setup before using the system."
            : "Review or rerun setup using the same guided flow."
        }
      />
      <Card title="Deployment Basics" subtitle="Only required values to get the controller online and ready.">
        <form className="settings-form" onSubmit={(event) => void handleSubmit(event)}>
          <label className="form-field">
            <span>Controller UI URL</span>
            <input value={uiUrl} onChange={(event) => setUiUrl(event.target.value)} placeholder="http://controller-ui:8088" />
          </label>
          <label className="form-field">
            <span>Controller API URL (optional when same-origin)</span>
            <input
              value={apiUrl}
              onChange={(event) => setApiUrl(event.target.value)}
              placeholder="http://controller-api:8080/api/v1"
            />
          </label>
          <label className="form-field">
            <span>Registry Name</span>
            <input value={registryName} onChange={(event) => setRegistryName(event.target.value)} />
          </label>
          <label className="form-field">
            <span>NMOS IS-04 Base URL</span>
            <input value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} placeholder="http://registry-host:8081" />
          </label>
          {submitError ? <ErrorPanel message={submitError} /> : null}
          {!isInitialSetup && isReady ? <p className="muted-copy">Wizard complete. Topology endpoint is responding.</p> : null}
          <div className="button-row">
            <button className="primary-button" type="submit" disabled={isBusy}>
              {isSubmitting ? "Saving..." : "Save and Finish"}
            </button>
            <button className="ghost-button" type="button" disabled={isBusy} onClick={() => void handleUseExistingSettings()}>
              Use Existing Settings
            </button>
            <button className="ghost-button" type="button" onClick={handleBypassWizard}>
              Bypass Wizard
            </button>
          </div>
        </form>
      </Card>
    </div>
  );
}
