import { useMemo, useState } from "react";
import { useDeletePreset, useExecutePreset, usePresets, useReceivers, useSavePreset, useSenders } from "../api/hooks";
import type { ActivationModeType } from "../api/types";
import { Card } from "../components/Card";
import { EmptyState } from "../components/EmptyState";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

export function PresetsPage() {
  const presetsQuery = usePresets();
  const receiversQuery = useReceivers();
  const sendersQuery = useSenders();
  const savePreset = useSavePreset();
  const executePreset = useExecutePreset();
  const deletePreset = useDeletePreset();

  const [name, setName] = useState("Demo Salvo");
  const [description, setDescription] = useState("Connect the first compatible sender to the first receiver.");
  const [requestedBy, setRequestedBy] = useState("operator");
  const [activationMode, setActivationMode] = useState<ActivationModeType>("Immediate");

  const generatedRoute = useMemo(() => {
    const receiver = receiversQuery.data?.[0];
    const sender = sendersQuery.data?.find((candidate) => candidate.transport === receiver?.transport);
    if (!receiver || !sender) {
      return null;
    }

    return {
      receiverId: receiver.id,
      senderId: sender.id,
      activationMode: "Immediate" as const,
    };
  }, [receiversQuery.data, sendersQuery.data]);

  if (presetsQuery.isLoading || receiversQuery.isLoading || sendersQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (presetsQuery.isError) {
    return <ErrorPanel message={presetsQuery.error.message} />;
  }

  const presets = presetsQuery.data ?? [];

  async function handleCreatePreset() {
    if (!generatedRoute) {
      return;
    }

    await savePreset.mutateAsync({
      name,
      description,
      routes: [generatedRoute],
    });
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Presets / Salvos" subtitle="Create repeatable receiver routing actions and execute them from the controller." />

      <div className="two-column">
        <Card title="Create Demo Preset" subtitle="Uses the first compatible sender and receiver in the current topology.">
          <div className="stack">
            <label className="form-field">
              <span>Name</span>
              <input value={name} onChange={(event) => setName(event.target.value)} />
            </label>
            <label className="form-field">
              <span>Description</span>
              <textarea value={description} onChange={(event) => setDescription(event.target.value)} rows={4} />
            </label>
            <button className="primary-button" type="button" disabled={!generatedRoute} onClick={() => void handleCreatePreset()}>
              Save Preset
            </button>
          </div>
        </Card>

        <Card title="Execute Settings" subtitle="Optional activation override for preset execution.">
          <div className="stack">
            <label className="form-field">
              <span>Requested By</span>
              <input value={requestedBy} onChange={(event) => setRequestedBy(event.target.value)} />
            </label>
            <label className="form-field">
              <span>Activation</span>
              <select value={activationMode} onChange={(event) => setActivationMode(event.target.value as ActivationModeType)}>
                <option value="Immediate">Immediate</option>
                <option value="ScheduledAbsolute">Scheduled Absolute</option>
                <option value="ScheduledRelative">Scheduled Relative</option>
              </select>
            </label>
          </div>
        </Card>
      </div>

      <Card title="Saved Presets" subtitle="Execute or delete stored salvos.">
        {presets.length === 0 ? (
          <EmptyState title="No presets yet" description="Create a demo preset from the current topology to begin." />
        ) : (
          <div className="stack">
            {presets.map((preset) => (
              <article key={preset.id} className="preset-row">
                <div>
                  <strong>{preset.name}</strong>
                  <p>{preset.description ?? "No description"}</p>
                  <small>{preset.routes.length} route(s)</small>
                </div>
                <div className="button-row">
                  <button
                    className="primary-button"
                    type="button"
                    onClick={() =>
                      void executePreset.mutateAsync({
                        presetId: preset.id,
                        payload: {
                          requestedBy,
                          activationMode,
                        },
                      })
                    }
                  >
                    Execute
                  </button>
                  <button className="ghost-button" type="button" onClick={() => void deletePreset.mutateAsync(preset.id)}>
                    Delete
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </Card>
    </div>
  );
}
