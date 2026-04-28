import { useEffect, useMemo, useState } from "react";
import { useConnectReceiver, useDisconnectReceiver, useValidateRoute } from "../../api/hooks";
import type { ActivationModeType, NmosReceiver, NmosSender } from "../../api/types";
import { Drawer } from "../../components/Drawer";
import { StatusBadge } from "../../components/StatusBadge";

interface ConnectionDrawerProps {
  open: boolean;
  receiver: NmosReceiver | null;
  senders: NmosSender[];
  onClose: () => void;
}

const defaultOperator = "operator";

export function ConnectionDrawer({ open, receiver, senders, onClose }: ConnectionDrawerProps) {
  const [selectedSenderId, setSelectedSenderId] = useState<string>("");
  const [operatorName, setOperatorName] = useState(defaultOperator);
  const [validationMessage, setValidationMessage] = useState<string>("");
  const [activationMode, setActivationMode] = useState<ActivationModeType>("Immediate");

  const validateMutation = useValidateRoute();
  const connectMutation = useConnectReceiver();
  const disconnectMutation = useDisconnectReceiver();

  const compatibleSenders = useMemo(() => {
    if (!receiver) {
      return [];
    }

    return senders.filter((sender) => sender.transport === receiver.transport);
  }, [receiver, senders]);

  useEffect(() => {
    if (!receiver) {
      setSelectedSenderId("");
      setValidationMessage("");
      setActivationMode("Immediate");
      return;
    }

    setSelectedSenderId(receiver.active.senderId ?? "");
    setValidationMessage("");
    setActivationMode("Immediate");
  }, [receiver]);

  if (!receiver) {
    return null;
  }

  const activeReceiver = receiver;
  const isBusy = validateMutation.isPending || connectMutation.isPending || disconnectMutation.isPending;

  async function handleValidate() {
    if (!selectedSenderId) {
      setValidationMessage("Select a sender first.");
      return;
    }

    setValidationMessage("Validating route...");

    try {
      const result = await validateMutation.mutateAsync({
        senderId: selectedSenderId,
        receiverId: activeReceiver.id,
        activationMode,
      });

      setValidationMessage(
        result.issues.length > 0
          ? result.issues.map((issue) => issue.message).join(" ")
          : `Validation status: ${result.status}.`,
      );
    } catch (error) {
      setValidationMessage(error instanceof Error ? error.message : "Validation failed.");
    }
  }

  async function handleConnect() {
    if (!selectedSenderId) {
      setValidationMessage("Select a sender first.");
      return;
    }

    setValidationMessage("Submitting connection request...");

    try {
      await connectMutation.mutateAsync({
        receiverId: activeReceiver.id,
        payload: {
          senderId: selectedSenderId,
          requestedBy: operatorName,
          activationMode,
        },
      });
      setValidationMessage("Connection request submitted.");
    } catch (error) {
      setValidationMessage(error instanceof Error ? error.message : "Connection failed.");
    }
  }

  async function handleDisconnect() {
    setValidationMessage("Submitting disconnect request...");

    try {
      await disconnectMutation.mutateAsync({
        receiverId: activeReceiver.id,
        payload: {
          requestedBy: operatorName,
          activationMode,
        },
      });
      setValidationMessage("Disconnect request submitted.");
    } catch (error) {
      setValidationMessage(error instanceof Error ? error.message : "Disconnect failed.");
    }
  }

  return (
    <Drawer open={open} title={activeReceiver.label} onClose={onClose}>
      <div className="route-editor-layout">
        <section className="detail-block stack">
          <div className="inline-status">
            <StatusBadge tone={activeReceiver.active.senderId ? "success" : "muted"}>
              {activeReceiver.active.senderId ? "Connected" : "Disconnected"}
            </StatusBadge>
            <StatusBadge tone={activeReceiver.isConnectable ? "info" : "danger"}>
              {activeReceiver.isConnectable ? "Connectable" : "Locked"}
            </StatusBadge>
          </div>

          <label className="form-field">
            <span>Operator</span>
            <input value={operatorName} onChange={(event) => setOperatorName(event.target.value)} />
          </label>

          <label className="form-field">
            <span>Activation</span>
            <select
              value={activationMode}
              onChange={(event) => setActivationMode(event.target.value as ActivationModeType)}
              disabled={isBusy}
            >
              <option value="Immediate">Immediate</option>
              <option value="ScheduledAbsolute">Scheduled Absolute</option>
              <option value="ScheduledRelative">Scheduled Relative</option>
            </select>
          </label>

          <label className="form-field">
            <span>Sender</span>
            <select value={selectedSenderId} onChange={(event) => setSelectedSenderId(event.target.value)} disabled={isBusy}>
              <option value="">Select sender</option>
              {compatibleSenders.map((sender) => (
                <option key={sender.id} value={sender.id}>
                  {sender.label}
                </option>
              ))}
            </select>
          </label>

          <div className="button-row">
            <button className="ghost-button" type="button" onClick={() => void handleValidate()} disabled={isBusy}>
              {validateMutation.isPending ? "Validating..." : "Validate"}
            </button>
            <button className="primary-button" type="button" onClick={() => void handleConnect()} disabled={isBusy}>
              {connectMutation.isPending ? "Connecting..." : "Connect"}
            </button>
            <button className="danger-button" type="button" onClick={() => void handleDisconnect()} disabled={isBusy}>
              {disconnectMutation.isPending ? "Disconnecting..." : "Disconnect"}
            </button>
          </div>

          {validationMessage ? <div className="info-strip">{validationMessage}</div> : null}
        </section>

        <section className="route-editor-raw stack">
          <section className="detail-block">
            <h4>Active State</h4>
            <pre>{JSON.stringify(activeReceiver.active, null, 2)}</pre>
          </section>

          <section className="detail-block">
            <h4>Staged State</h4>
            <pre>{JSON.stringify(activeReceiver.staged, null, 2)}</pre>
          </section>

          <section className="detail-block">
            <h4>Constraints</h4>
            <pre>{JSON.stringify(activeReceiver.constraints, null, 2)}</pre>
          </section>
        </section>
      </div>
    </Drawer>
  );
}
