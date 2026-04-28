import { useMemo, useState } from "react";
import { useConnectReceiver, useReceivers, useSenders } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";

const signalTypeOrder: Record<string, number> = {
  Video: 0,
  Audio: 1,
  Ancillary: 2,
};

export function RoutingPage() {
  const receiversQuery = useReceivers();
  const sendersQuery = useSenders();
  const connectReceiver = useConnectReceiver();
  const [layoutMode, setLayoutMode] = useState<"side-by-side" | "top-to-bottom">("side-by-side");
  const [selectedReceiverId, setSelectedReceiverId] = useState<string | null>(null);
  const [lastRouteResult, setLastRouteResult] = useState<{
    receiverId: string;
    senderId: string;
    status: "success" | "error";
    message: string;
  } | null>(null);

  const receivers = useMemo(
    () =>
      [...(receiversQuery.data ?? [])].sort((left, right) => {
        const leftRank = signalTypeOrder[left.signalType] ?? 99;
        const rightRank = signalTypeOrder[right.signalType] ?? 99;
        if (leftRank !== rightRank) {
          return leftRank - rightRank;
        }
        return left.label.localeCompare(right.label);
      }),
    [receiversQuery.data],
  );

  const senders = useMemo(
    () =>
      [...(sendersQuery.data ?? [])].sort((left, right) => {
        const leftRank = signalTypeOrder[left.signalType] ?? 99;
        const rightRank = signalTypeOrder[right.signalType] ?? 99;
        if (leftRank !== rightRank) {
          return leftRank - rightRank;
        }
        return left.label.localeCompare(right.label);
      }),
    [sendersQuery.data],
  );
  const receiverById = useMemo(() => new Map(receivers.map((receiver) => [receiver.id, receiver])), [receivers]);
  const selectedReceiver = selectedReceiverId ? receiverById.get(selectedReceiverId) ?? null : null;

  if (receiversQuery.isLoading || sendersQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (receiversQuery.isError) {
    return <ErrorPanel message={receiversQuery.error.message} />;
  }

  if (sendersQuery.isError) {
    return <ErrorPanel message={sendersQuery.error.message} />;
  }

  async function handleSenderSelect(senderId: string) {
    if (!selectedReceiver) {
      return;
    }

    const sender = senders.find((item) => item.id === senderId);
    if (!sender || normalizeSignalType(sender.signalType) !== normalizeSignalType(selectedReceiver.signalType)) {
      return;
    }

    try {
      await connectReceiver.mutateAsync({
        receiverId: selectedReceiver.id,
        payload: {
          senderId: sender.id,
          requestedBy: "soft-panel",
          activationMode: "Immediate",
        },
      });

      setLastRouteResult({
        receiverId: selectedReceiver.id,
        senderId: sender.id,
        status: "success",
        message: `Connected ${selectedReceiver.label} to ${sender.label}.`,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Routing failed.";
      setLastRouteResult({
        receiverId: selectedReceiver.id,
        senderId: sender.id,
        status: "error",
        message,
      });
    } finally {
      setSelectedReceiverId(null);
    }
  }

  return (
    <div className="stack-xl">
      <PageHeader
        title="Soft Panel"
        subtitle="Select a receiver, then select a sender of the same signal type."
        actions={
          <div className="soft-layout-toggle" role="tablist" aria-label="Panel layout">
            <button
              type="button"
              role="tab"
              aria-selected={layoutMode === "side-by-side"}
              className={`ghost-button${layoutMode === "side-by-side" ? " is-selected" : ""}`}
              onClick={() => setLayoutMode("side-by-side")}
            >
              Side by Side
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={layoutMode === "top-to-bottom"}
              className={`ghost-button${layoutMode === "top-to-bottom" ? " is-selected" : ""}`}
              onClick={() => setLayoutMode("top-to-bottom")}
            >
              Top to Bottom
            </button>
          </div>
        }
      />
      {lastRouteResult ? (
        <div className="soft-panel-message" aria-live="polite">{lastRouteResult.message}</div>
      ) : null}

      <section
        className={layoutMode === "side-by-side" ? "two-column soft-layout-side-by-side" : "stack soft-layout-top-to-bottom"}
        aria-label="Routing endpoints"
      >
        <Card title="Receivers" subtitle={`${receivers.length} endpoints`} className="soft-route-panel">
          <div className="soft-route-scroll">
          <div className="resource-grid soft-route-grid">
            {receivers.map((receiver) => (
              <button
                key={receiver.id}
                type="button"
                className={resolveReceiverCardClass(receiver.id, selectedReceiverId, lastRouteResult)}
                onClick={() => {
                  setLastRouteResult(null);
                  setSelectedReceiverId(receiver.id);
                }}
                disabled={connectReceiver.isPending}
              >
                <div className="soft-route-title-row">
                  <strong>{receiver.label}</strong>
                  <StatusBadge tone="info">{toSignalBadgeLabel(receiver.signalType)}</StatusBadge>
                </div>
              </button>
            ))}
            {receivers.length === 0 ? <p className="muted-copy">No receivers found.</p> : null}
          </div>
          </div>
        </Card>

        <Card title="Senders" subtitle={`${senders.length} endpoints`} className="soft-route-panel">
          <div className="soft-route-scroll">
          <div className="resource-grid soft-route-grid">
            {senders.map((sender) => {
              const isSignalMatch =
                !selectedReceiver ||
                normalizeSignalType(sender.signalType) === normalizeSignalType(selectedReceiver.signalType);
              return (
                <button
                  key={sender.id}
                  type="button"
                  className={resolveSenderCardClass(sender.id, isSignalMatch, selectedReceiver, lastRouteResult)}
                  onClick={() => void handleSenderSelect(sender.id)}
                  disabled={!selectedReceiver || !isSignalMatch || connectReceiver.isPending}
                  aria-disabled={!selectedReceiver || !isSignalMatch || connectReceiver.isPending}
                >
                  <div className="soft-route-title-row">
                    <strong>{sender.label}</strong>
                    <StatusBadge tone="info">{toSignalBadgeLabel(sender.signalType)}</StatusBadge>
                  </div>
                </button>
            )})}
            {senders.length === 0 ? <p className="muted-copy">No senders found.</p> : null}
          </div>
          </div>
        </Card>
      </section>
    </div>
  );
}

function normalizeSignalType(value: string): string {
  return value.trim().toLowerCase();
}

function toSignalBadgeLabel(signalType: string): string {
  const normalized = normalizeSignalType(signalType);
  if (normalized === "video") {
    return "V";
  }
  if (normalized === "audio") {
    return "A";
  }
  if (normalized === "ancillary") {
    return "ANC";
  }
  return signalType;
}

function resolveReceiverCardClass(
  receiverId: string,
  selectedReceiverId: string | null,
  result: { receiverId: string; senderId: string; status: "success" | "error"; message: string } | null,
): string {
  const classes = ["resource-card", "soft-route-card"];

  if (selectedReceiverId === receiverId) {
    classes.push("is-selected");
  }

  if (result?.receiverId === receiverId) {
    classes.push(result.status === "success" ? "is-success" : "is-error");
  }

  return classes.join(" ");
}

function resolveSenderCardClass(
  senderId: string,
  isSignalMatch: boolean,
  selectedReceiver: { id: string; signalType: string; active: { senderId: string | null } } | null,
  result: { receiverId: string; senderId: string; status: "success" | "error"; message: string } | null,
): string {
  const classes = ["resource-card", "soft-route-card"];

  if (selectedReceiver && !isSignalMatch) {
    classes.push("is-disabled");
  }
  if (selectedReceiver?.active.senderId === senderId) {
    classes.push("is-linked");
  }

  if (result?.senderId === senderId) {
    classes.push(result.status === "success" ? "is-success" : "is-error");
  }

  return classes.join(" ");
}
