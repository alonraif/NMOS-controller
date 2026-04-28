import { useMemo, useState } from "react";
import { useConnectReceiver, useReceivers, useSenders } from "../api/hooks";
import type { NmosReceiver, NmosSender } from "../api/types";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { StatusBadge } from "../components/StatusBadge";

type SoftPanelTab = "audio-follow-video" | "per-signal";
type LayoutMode = "side-by-side" | "top-to-bottom";
type LayerKey = "video" | "audio" | "ancillary";

interface ReceiverBundle {
  id: string;
  label: string;
  videoReceiverId: string | null;
  audioReceiverId: string | null;
  ancillaryReceiverId: string | null;
}

interface SenderBundle {
  id: string;
  label: string;
  videoSenderId: string | null;
  audioSenderId: string | null;
  ancillarySenderId: string | null;
}

const signalTypeOrder: Record<string, number> = {
  Video: 0,
  Audio: 1,
  Ancillary: 2,
};

export function RoutingPage() {
  const receiversQuery = useReceivers();
  const sendersQuery = useSenders();
  const connectReceiver = useConnectReceiver();
  const [activeTab, setActiveTab] = useState<SoftPanelTab>("audio-follow-video");
  const [layoutMode, setLayoutMode] = useState<LayoutMode>("side-by-side");

  const [selectedReceiverId, setSelectedReceiverId] = useState<string | null>(null);
  const [selectedReceiverBundleId, setSelectedReceiverBundleId] = useState<string | null>(null);

  const [perSignalResult, setPerSignalResult] = useState<{
    receiverId: string;
    senderId: string;
    status: "success" | "error";
    message: string;
  } | null>(null);

  const [afvResult, setAfvResult] = useState<{
    receiverBundleId: string;
    senderBundleId: string;
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

  const receiverBundles = useMemo(() => buildReceiverBundles(receivers), [receivers]);
  const senderBundles = useMemo(() => buildSenderBundles(senders), [senders]);
  const receiverBundleById = useMemo(() => new Map(receiverBundles.map((bundle) => [bundle.id, bundle])), [receiverBundles]);
  const selectedReceiverBundle = selectedReceiverBundleId ? receiverBundleById.get(selectedReceiverBundleId) ?? null : null;

  if (receiversQuery.isLoading || sendersQuery.isLoading) {
    return <LoadingPanel />;
  }
  if (receiversQuery.isError) {
    return <ErrorPanel message={receiversQuery.error.message} />;
  }
  if (sendersQuery.isError) {
    return <ErrorPanel message={sendersQuery.error.message} />;
  }

  async function handlePerSignalSenderSelect(senderId: string) {
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

      setPerSignalResult({
        receiverId: selectedReceiver.id,
        senderId: sender.id,
        status: "success",
        message: `Connected ${selectedReceiver.label} to ${sender.label}.`,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : "Routing failed.";
      setPerSignalResult({
        receiverId: selectedReceiver.id,
        senderId: sender.id,
        status: "error",
        message,
      });
    } finally {
      setSelectedReceiverId(null);
    }
  }

  async function handleAfvSenderBundleSelect(senderBundleId: string) {
    if (!selectedReceiverBundle) {
      return;
    }

    const senderBundle = senderBundles.find((bundle) => bundle.id === senderBundleId);
    if (!senderBundle) {
      return;
    }

    const routes: Array<{ receiverId: string; senderId: string }> = [];
    if (selectedReceiverBundle.videoReceiverId && senderBundle.videoSenderId) {
      routes.push({ receiverId: selectedReceiverBundle.videoReceiverId, senderId: senderBundle.videoSenderId });
    }
    if (selectedReceiverBundle.audioReceiverId && senderBundle.audioSenderId) {
      routes.push({ receiverId: selectedReceiverBundle.audioReceiverId, senderId: senderBundle.audioSenderId });
    }
    if (selectedReceiverBundle.ancillaryReceiverId && senderBundle.ancillarySenderId) {
      routes.push({ receiverId: selectedReceiverBundle.ancillaryReceiverId, senderId: senderBundle.ancillarySenderId });
    }

    if (routes.length === 0) {
      setAfvResult({
        receiverBundleId: selectedReceiverBundle.id,
        senderBundleId: senderBundle.id,
        status: "error",
        message: "No compatible V/A/ANC routes found between selected bundles.",
      });
      return;
    }

    try {
      for (const route of routes) {
        await connectReceiver.mutateAsync({
          receiverId: route.receiverId,
          payload: {
            senderId: route.senderId,
            requestedBy: "soft-panel-afv",
            activationMode: "Immediate",
          },
        });
      }

      setAfvResult({
        receiverBundleId: selectedReceiverBundle.id,
        senderBundleId: senderBundle.id,
        status: "success",
        message: `Connected ${selectedReceiverBundle.label} to ${senderBundle.label} (AFV).`,
      });
    } catch (error) {
      const message = error instanceof Error ? error.message : "AFV routing failed.";
      setAfvResult({
        receiverBundleId: selectedReceiverBundle.id,
        senderBundleId: senderBundle.id,
        status: "error",
        message,
      });
    } finally {
      setSelectedReceiverBundleId(null);
    }
  }

  return (
    <div className="stack-xl">
      <PageHeader
        title="Soft Panel"
        subtitle={
          activeTab === "audio-follow-video"
            ? "Grouped endpoint routing with Audio/Ancillary follow Video."
            : "Per-signal routing (Video/Audio/Ancillary shown as separate entries)."
        }
        actions={
          <div className="soft-layout-toggle" role="tablist" aria-label="Soft panel mode">
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === "audio-follow-video"}
              className={`ghost-button${activeTab === "audio-follow-video" ? " is-selected" : ""}`}
              onClick={() => setActiveTab("audio-follow-video")}
            >
              Audio Follow Video
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === "per-signal"}
              className={`ghost-button${activeTab === "per-signal" ? " is-selected" : ""}`}
              onClick={() => setActiveTab("per-signal")}
            >
              Per Signal
            </button>
          </div>
        }
      />

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

      {activeTab === "audio-follow-video" && afvResult ? (
        <div className="soft-panel-message" aria-live="polite">{afvResult.message}</div>
      ) : null}
      {activeTab === "per-signal" && perSignalResult ? (
        <div className="soft-panel-message" aria-live="polite">{perSignalResult.message}</div>
      ) : null}

      {activeTab === "audio-follow-video" ? (
        <section
          className={layoutMode === "side-by-side" ? "two-column soft-layout-side-by-side" : "stack soft-layout-top-to-bottom"}
          aria-label="Grouped routing endpoints"
        >
          <Card title="Receiver Endpoints" subtitle={`${receiverBundles.length} grouped endpoints`} className="soft-route-panel">
            <div className="soft-route-scroll">
              <div className="resource-grid soft-route-grid">
                {receiverBundles.map((bundle) => (
                  <button
                    key={bundle.id}
                    type="button"
                    className={resolveAfvReceiverCardClass(bundle.id, selectedReceiverBundleId, afvResult)}
                    onClick={() => {
                      setAfvResult(null);
                      setSelectedReceiverBundleId(bundle.id);
                    }}
                    disabled={connectReceiver.isPending}
                  >
                    <div className="soft-route-title-row">
                      <strong>{bundle.label}</strong>
                    </div>
                    <div className="soft-route-badge-row">
                      <StatusBadge tone={bundle.videoReceiverId ? "success" : "muted"}>V</StatusBadge>
                      <StatusBadge tone={bundle.audioReceiverId ? "success" : "muted"}>A</StatusBadge>
                      <StatusBadge tone={bundle.ancillaryReceiverId ? "success" : "muted"}>ANC</StatusBadge>
                    </div>
                  </button>
                ))}
                {receiverBundles.length === 0 ? <p className="muted-copy">No grouped receivers found.</p> : null}
              </div>
            </div>
          </Card>

          <Card title="Sender Endpoints" subtitle={`${senderBundles.length} grouped endpoints`} className="soft-route-panel">
            <div className="soft-route-scroll">
              <div className="resource-grid soft-route-grid">
                {senderBundles.map((bundle) => {
                  const enabled = Boolean(selectedReceiverBundle);
                  const linked = isAfvBundleLinked(selectedReceiverBundle, bundle, receiverById);
                  return (
                    <button
                      key={bundle.id}
                      type="button"
                      className={resolveAfvSenderCardClass(bundle.id, linked, afvResult)}
                      onClick={() => void handleAfvSenderBundleSelect(bundle.id)}
                      disabled={!enabled || connectReceiver.isPending}
                      aria-disabled={!enabled || connectReceiver.isPending}
                    >
                      <div className="soft-route-title-row">
                        <strong>{bundle.label}</strong>
                      </div>
                      <div className="soft-route-badge-row">
                        <StatusBadge tone={bundle.videoSenderId ? "success" : "muted"}>V</StatusBadge>
                        <StatusBadge tone={bundle.audioSenderId ? "success" : "muted"}>A</StatusBadge>
                        <StatusBadge tone={bundle.ancillarySenderId ? "success" : "muted"}>ANC</StatusBadge>
                      </div>
                    </button>
                  );
                })}
                {senderBundles.length === 0 ? <p className="muted-copy">No grouped senders found.</p> : null}
              </div>
            </div>
          </Card>
        </section>
      ) : (
        <section
          className={layoutMode === "side-by-side" ? "two-column soft-layout-side-by-side" : "stack soft-layout-top-to-bottom"}
          aria-label="Per-signal routing endpoints"
        >
          <Card title="Receivers" subtitle={`${receivers.length} endpoints`} className="soft-route-panel">
            <div className="soft-route-scroll">
              <div className="resource-grid soft-route-grid">
                {receivers.map((receiver) => (
                  <button
                    key={receiver.id}
                    type="button"
                    className={resolveReceiverCardClass(receiver.id, selectedReceiverId, perSignalResult)}
                    onClick={() => {
                      setPerSignalResult(null);
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
                      className={resolveSenderCardClass(sender.id, isSignalMatch, selectedReceiver, perSignalResult)}
                      onClick={() => void handlePerSignalSenderSelect(sender.id)}
                      disabled={!selectedReceiver || !isSignalMatch || connectReceiver.isPending}
                      aria-disabled={!selectedReceiver || !isSignalMatch || connectReceiver.isPending}
                    >
                      <div className="soft-route-title-row">
                        <strong>{sender.label}</strong>
                        <StatusBadge tone="info">{toSignalBadgeLabel(sender.signalType)}</StatusBadge>
                      </div>
                    </button>
                  );
                })}
                {senders.length === 0 ? <p className="muted-copy">No senders found.</p> : null}
              </div>
            </div>
          </Card>
        </section>
      )}
    </div>
  );
}

function buildReceiverBundles(receivers: NmosReceiver[]): ReceiverBundle[] {
  const bundles = new Map<string, ReceiverBundle>();

  for (const receiver of receivers) {
    const key = receiver.routingDestinationId || receiver.deviceId || receiver.id;
    const receiverBaseLabel = stripSignalSuffix(receiver.label);
    const existing = bundles.get(key) ?? {
      id: key,
      label: receiverBaseLabel || receiver.routingDestinationLabel || receiver.label,
      videoReceiverId: null,
      audioReceiverId: null,
      ancillaryReceiverId: null,
    };

    if (isSignalType(receiver.signalType, "video")) {
      existing.videoReceiverId = receiver.id;
    }
    if (isSignalType(receiver.signalType, "audio")) {
      existing.audioReceiverId = receiver.id;
    }
    if (isSignalType(receiver.signalType, "ancillary")) {
      existing.ancillaryReceiverId = receiver.id;
    }
    if (isSignalType(receiver.signalType, "video") && receiverBaseLabel) {
      existing.label = receiverBaseLabel;
    }
    bundles.set(key, existing);
  }

  return Array.from(bundles.values()).sort((left, right) => left.label.localeCompare(right.label));
}

function buildSenderBundles(senders: NmosSender[]): SenderBundle[] {
  const bundles = new Map<string, SenderBundle>();

  for (const sender of senders) {
    const key = sender.sourceGroupId || sender.id;
    const label = stripSignalSuffix(sender.sourceGroupLabel || sender.label);
    const existing = bundles.get(key) ?? {
      id: key,
      label,
      videoSenderId: null,
      audioSenderId: null,
      ancillarySenderId: null,
    };

    if (isSignalType(sender.signalType, "video")) {
      existing.videoSenderId = sender.id;
    }
    if (isSignalType(sender.signalType, "audio")) {
      existing.audioSenderId = sender.id;
    }
    if (isSignalType(sender.signalType, "ancillary")) {
      existing.ancillarySenderId = sender.id;
    }
    bundles.set(key, existing);
  }

  return Array.from(bundles.values()).sort((left, right) => left.label.localeCompare(right.label));
}

function stripSignalSuffix(value: string): string {
  return value.replace(/[:\s_-]+(video|audio|ancillary)$/i, "").trim();
}

function isSignalType(value: string, expected: "video" | "audio" | "ancillary"): boolean {
  const normalized = normalizeSignalType(value);
  if (expected === "ancillary") {
    return normalized === "ancillary" || normalized === "anc";
  }
  return normalized === expected;
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
  if (normalized === "ancillary" || normalized === "anc") {
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
  selectedReceiver: { active: { senderId: string | null }; signalType: string } | null,
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

function resolveAfvReceiverCardClass(
  bundleId: string,
  selectedReceiverBundleId: string | null,
  result: { receiverBundleId: string; senderBundleId: string; status: "success" | "error"; message: string } | null,
): string {
  const classes = ["resource-card", "soft-route-card"];
  if (bundleId === selectedReceiverBundleId) {
    classes.push("is-selected");
  }
  if (result?.receiverBundleId === bundleId) {
    classes.push(result.status === "success" ? "is-success" : "is-error");
  }
  return classes.join(" ");
}

function resolveAfvSenderCardClass(
  bundleId: string,
  linked: boolean,
  result: { receiverBundleId: string; senderBundleId: string; status: "success" | "error"; message: string } | null,
): string {
  const classes = ["resource-card", "soft-route-card"];
  if (linked) {
    classes.push("is-linked");
  }
  if (result?.senderBundleId === bundleId) {
    classes.push(result.status === "success" ? "is-success" : "is-error");
  }
  return classes.join(" ");
}

function isAfvBundleLinked(
  receiverBundle: ReceiverBundle | null,
  senderBundle: SenderBundle,
  receiverById: Map<string, NmosReceiver>,
): boolean {
  if (!receiverBundle) {
    return false;
  }

  const activeSenderIds = new Set<string>();
  const receiverIds = [receiverBundle.videoReceiverId, receiverBundle.audioReceiverId, receiverBundle.ancillaryReceiverId];
  for (const receiverId of receiverIds) {
    if (!receiverId) {
      continue;
    }
    const activeSenderId = receiverById.get(receiverId)?.active.senderId;
    if (activeSenderId) {
      activeSenderIds.add(activeSenderId);
    }
  }

  return [senderBundle.videoSenderId, senderBundle.audioSenderId, senderBundle.ancillarySenderId].some(
    (senderId) => senderId !== null && activeSenderIds.has(senderId),
  );
}
