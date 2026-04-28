import { useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useConnectReceiver, useDisconnectReceiver, useReceivers, useSenders, useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";
import { StatusBadge } from "../components/StatusBadge";
import { ConnectionDrawer } from "../features/routing/ConnectionDrawer";

type SenderReceiverTab = "senders" | "receivers";
const receiverSignalOrder: Record<string, number> = {
  Video: 0,
  Audio: 1,
  Ancillary: 2,
};

function resolveTab(value: string | null): SenderReceiverTab {
  return value === "senders" ? "senders" : "receivers";
}

export function SendersReceiversPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [senderSearch, setSenderSearch] = useState("");
  const [receiverSearch, setReceiverSearch] = useState("");
  const [selectedReceiverId, setSelectedReceiverId] = useState<string | null>(null);
  const sendersQuery = useSenders();
  const receiversQuery = useReceivers();
  const topologyQuery = useTopology();
  const connectReceiver = useConnectReceiver();
  const disconnectReceiver = useDisconnectReceiver();
  const activeTab = resolveTab(searchParams.get("tab"));

  const senders = useMemo(() => {
    const source = sendersQuery.data ?? [];
    return [...source]
      .filter((sender) => {
        const term = senderSearch.toLowerCase();
        return sender.label.toLowerCase().includes(term) || sender.id.toLowerCase().includes(term);
      })
      .sort((left, right) => left.label.localeCompare(right.label));
  }, [senderSearch, sendersQuery.data]);

  const receivers = useMemo(() => {
    const source = receiversQuery.data ?? [];
    return [...source]
      .filter((receiver) => {
        const term = receiverSearch.toLowerCase();
        return receiver.label.toLowerCase().includes(term) || receiver.id.toLowerCase().includes(term);
      })
      .sort((left, right) => left.label.localeCompare(right.label));
  }, [receiverSearch, receiversQuery.data]);

  const selectedReceiver = useMemo(
    () => (selectedReceiverId ? (receiversQuery.data ?? []).find((receiver) => receiver.id === selectedReceiverId) ?? null : null),
    [receiversQuery.data, selectedReceiverId],
  );
  const deviceLabelById = useMemo(() => {
    const map = new Map<string, string>();
    for (const device of topologyQuery.data?.devices ?? []) {
      map.set(device.id, device.label);
    }
    return map;
  }, [topologyQuery.data]);

  const receiverGroups = useMemo(() => {
    const groups = new Map<string, { deviceId: string; deviceLabel: string; receivers: typeof receivers }>();

    for (const receiver of receivers) {
      const existing = groups.get(receiver.deviceId);
      if (existing) {
        existing.receivers.push(receiver);
        continue;
      }

      groups.set(receiver.deviceId, {
        deviceId: receiver.deviceId,
        deviceLabel: deviceLabelById.get(receiver.deviceId) ?? receiver.deviceId,
        receivers: [receiver],
      });
    }

    return Array.from(groups.values())
      .map((group) => ({
        ...group,
        receivers: [...group.receivers].sort((left, right) => {
          const leftRank = receiverSignalOrder[left.signalType] ?? 99;
          const rightRank = receiverSignalOrder[right.signalType] ?? 99;
          if (leftRank !== rightRank) {
            return leftRank - rightRank;
          }
          return left.label.localeCompare(right.label);
        }),
      }))
      .sort((left, right) => left.deviceLabel.localeCompare(right.deviceLabel));
  }, [deviceLabelById, receivers]);

  const senderGroups = useMemo(() => {
    const groups = new Map<string, { deviceId: string; deviceLabel: string; senders: typeof senders }>();

    for (const sender of senders) {
      const existing = groups.get(sender.deviceId);
      if (existing) {
        existing.senders.push(sender);
        continue;
      }

      groups.set(sender.deviceId, {
        deviceId: sender.deviceId,
        deviceLabel: deviceLabelById.get(sender.deviceId) ?? sender.deviceId,
        senders: [sender],
      });
    }

    return Array.from(groups.values())
      .map((group) => ({
        ...group,
        senders: [...group.senders].sort((left, right) => {
          const leftRank = receiverSignalOrder[left.signalType] ?? 99;
          const rightRank = receiverSignalOrder[right.signalType] ?? 99;
          if (leftRank !== rightRank) {
            return leftRank - rightRank;
          }
          return left.label.localeCompare(right.label);
        }),
      }))
      .sort((left, right) => left.deviceLabel.localeCompare(right.deviceLabel));
  }, [deviceLabelById, senders]);

  const senderLabelById = useMemo(() => {
    const map = new Map<string, string>();
    for (const sender of sendersQuery.data ?? []) {
      map.set(sender.id, sender.label);
    }
    return map;
  }, [sendersQuery.data]);
  const activeReceiverLabelsBySenderId = useMemo(() => {
    const map = new Map<string, string[]>();
    for (const receiver of receiversQuery.data ?? []) {
      const senderId = receiver.active.senderId;
      if (!senderId) {
        continue;
      }

      const current = map.get(senderId) ?? [];
      current.push(receiver.label);
      map.set(senderId, current);
    }

    for (const [senderId, labels] of map.entries()) {
      map.set(
        senderId,
        Array.from(new Set(labels)).sort((left, right) => left.localeCompare(right)),
      );
    }

    return map;
  }, [receiversQuery.data]);
  const relevantSendersByReceiverId = useMemo(() => {
    const map = new Map<string, Array<{ id: string; label: string }>>();
    const senders = sendersQuery.data ?? [];
    for (const receiver of receivers) {
      const compatible = senders
        .filter((sender) => sender.signalType === receiver.signalType)
        .sort((left, right) => left.label.localeCompare(right.label))
        .map((sender) => ({ id: sender.id, label: sender.label }));
      map.set(receiver.id, compatible);
    }
    return map;
  }, [receivers, sendersQuery.data]);

  if (sendersQuery.isLoading || (activeTab === "receivers" && receiversQuery.isLoading)) {
    return <LoadingPanel />;
  }

  if (sendersQuery.isError) {
    return <ErrorPanel message={sendersQuery.error.message} />;
  }

  if (activeTab === "receivers" && receiversQuery.isError) {
    return <ErrorPanel message={receiversQuery.error.message} />;
  }

  function setTab(tab: SenderReceiverTab) {
    if (tab === "receivers") {
      setSearchParams({});
      setSelectedReceiverId(null);
      return;
    }
    setSearchParams({ tab: "senders" });
  }

  async function handleDisconnect(receiverId: string) {
    await disconnectReceiver.mutateAsync({
      receiverId,
      payload: {
        requestedBy: "operator",
        activationMode: "Immediate",
      },
    });
  }

  async function handleConnectSelection(
    receiver: { id: string; active: { senderId: string | null } },
    targetSenderId: string,
  ) {
    if (!targetSenderId || receiver.active.senderId === targetSenderId) {
      return;
    }

    if (receiver.active.senderId) {
      await disconnectReceiver.mutateAsync({
        receiverId: receiver.id,
        payload: {
          requestedBy: "operator",
          activationMode: "Immediate",
        },
      });
    }

    await connectReceiver.mutateAsync({
      receiverId: receiver.id,
      payload: {
        senderId: targetSenderId,
        requestedBy: "operator",
        activationMode: "Immediate",
      },
    });
  }

  return (
    <div className="stack-xl senders-receivers-page">
      <PageHeader
        title="Senders/Receivers"
        subtitle="Inspect senders and receivers in one workspace, and open route editing directly from receiver cards."
      />
      <Card
        title="Inventory"
        subtitle={activeTab === "senders" ? "Sender inventory and subscription state." : "Receiver inventory and connection state."}
        actions={
          <div className="sender-receiver-actions">
          <div className="sender-receiver-tabs" role="tablist" aria-label="Senders and receivers">
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === "receivers"}
              className={`ghost-button${activeTab === "receivers" ? " is-selected" : ""}`}
              onClick={() => setTab("receivers")}
            >
              Receivers
            </button>
            <button
              type="button"
              role="tab"
              aria-selected={activeTab === "senders"}
              className={`ghost-button${activeTab === "senders" ? " is-selected" : ""}`}
              onClick={() => setTab("senders")}
            >
              Senders
            </button>
          </div>
            {activeTab === "senders" ? (
              <div className="sender-receiver-search">
                <SearchInput value={senderSearch} onChange={setSenderSearch} placeholder="Search senders" />
              </div>
            ) : (
              <div className="sender-receiver-search">
                <SearchInput value={receiverSearch} onChange={setReceiverSearch} placeholder="Search receivers" />
              </div>
            )}
          </div>
        }
      >
        {activeTab === "senders" ? (
          <div className="receiver-groups">
            {senderGroups.map((group) => (
              <section key={group.deviceId} className="receiver-group">
                <div className="receiver-group-header">
                  <span className="sidebar-label">Parent Device</span>
                  <strong>{group.deviceLabel}</strong>
                </div>
                <div className="resource-grid resource-grid-receivers">
                  {group.senders.map((sender) => (
                    <article key={sender.id} className="resource-card">
                      {(() => {
                        const connectedReceiverLabels = activeReceiverLabelsBySenderId.get(sender.id) ?? [];
                        const isConnected = connectedReceiverLabels.length > 0;
                        return (
                          <>
                      <div className="resource-card-header">
                        <div className="receiver-name-row">
                          <span
                            className={`connected-dot ${isConnected ? "is-active" : "is-inactive"}`}
                            aria-hidden="true"
                          />
                          <Link className="text-link" to={`/resources/${sender.id}`}>
                            {sender.label}
                          </Link>
                        </div>
                      </div>
                      <div className="resource-meta">
                        {resolveSenderMediaDetails(sender)}
                      </div>
                      <p className="muted-copy">
                        Connected Receiver:{" "}
                        {isConnected ? connectedReceiverLabels.join(", ") : "None"}
                      </p>
                          </>
                        );
                      })()}
                    </article>
                  ))}
                </div>
              </section>
            ))}
          </div>
        ) : (
          <div className="receiver-groups">
            {receiverGroups.map((group) => (
              <section key={group.deviceId} className="receiver-group">
                <div className="receiver-group-header">
                  <span className="sidebar-label">Parent Device</span>
                  <strong>{group.deviceLabel}</strong>
                </div>
                <div className="resource-grid resource-grid-receivers">
                  {group.receivers.map((receiver) => (
                    <article key={receiver.id} className="resource-card">
                      <div className="resource-card-header">
                        <div>
                          <div className="receiver-name-row">
                            <span
                              className={`connected-dot ${receiver.active.senderId ? "is-active" : "is-inactive"}`}
                              aria-hidden="true"
                            />
                            <button
                              type="button"
                              className="text-link button-link"
                              onClick={() => setSelectedReceiverId(receiver.id)}
                            >
                              {receiver.label}
                            </button>
                          </div>
                        </div>
                        <div>
                          <button
                            className="danger-button disconnect-button"
                            type="button"
                            disabled={
                              !receiver.active.senderId ||
                              (disconnectReceiver.isPending && disconnectReceiver.variables?.receiverId === receiver.id) ||
                              (connectReceiver.isPending && connectReceiver.variables?.receiverId === receiver.id)
                            }
                            onClick={() => void handleDisconnect(receiver.id)}
                          >
                            {disconnectReceiver.isPending && disconnectReceiver.variables?.receiverId === receiver.id
                              ? "Disconnecting..."
                              : "Disconnect"}
                          </button>
                        </div>
                      </div>
                      <div className="resource-meta">
                        {resolveReceiverMediaDetails(receiver)}
                      </div>
                      <p className="muted-copy">
                        Active Sender:{" "}
                        {receiver.active.senderId
                          ? senderLabelById.get(receiver.active.senderId) ?? receiver.active.senderId
                          : "None"}
                      </p>
                      <label className="form-field">
                        <span>Connect Sender</span>
                        <select
                          value={receiver.active.senderId ?? ""}
                          disabled={
                            (disconnectReceiver.isPending && disconnectReceiver.variables?.receiverId === receiver.id) ||
                            (connectReceiver.isPending && connectReceiver.variables?.receiverId === receiver.id) ||
                            (relevantSendersByReceiverId.get(receiver.id)?.length ?? 0) === 0
                          }
                          onChange={(event) => {
                            void handleConnectSelection(receiver, event.target.value);
                          }}
                        >
                          <option value="">Select sender</option>
                          {(relevantSendersByReceiverId.get(receiver.id) ?? []).map((sender) => (
                            <option key={sender.id} value={sender.id}>
                              {sender.label}
                            </option>
                          ))}
                        </select>
                      </label>
                    </article>
                  ))}
                </div>
              </section>
            ))}
          </div>
        )}
      </Card>
      <ConnectionDrawer
        open={Boolean(selectedReceiver)}
        receiver={selectedReceiver}
        senders={sendersQuery.data ?? []}
        onClose={() => setSelectedReceiverId(null)}
      />
    </div>
  );
}

function resolveReceiverMediaDetails(receiver: {
  signalType: string;
  format: { frameWidth: string | null; frameHeight: string | null; grainRate: string | null };
  active: { transportFile: { content: string } | null };
  staged: { transportFile: { content: string } | null };
}): JSX.Element {
  const sdpContent = receiver.active.transportFile?.content ?? receiver.staged.transportFile?.content;

  if (receiver.signalType === "Video") {
    const parsedVideo = parseVideoDetailsFromSdp(sdpContent);
    const resolution =
      parsedVideo.resolution ??
      (receiver.format.frameWidth && receiver.format.frameHeight
        ? `${receiver.format.frameWidth}x${receiver.format.frameHeight}`
        : "Unknown resolution");
    const frameRateValue = parsedVideo.frameRate ?? receiver.format.grainRate;
    const frameRate = frameRateValue ? `${frameRateValue} FPS` : "Unknown FPS";
    const scanType = parsedVideo.scanType ?? "Unknown scan";
    return (
      <>
        <span>{resolution}</span>
        <span>{frameRate}</span>
        <span>{scanType}</span>
      </>
    );
  }

  if (receiver.signalType === "Audio") {
    const channels = parseAudioChannels(sdpContent);
    return <span>{channels ? `Channels: ${channels}` : "Channels: Unknown"}</span>;
  }

  return <span>{receiver.signalType}</span>;
}

function resolveSenderMediaDetails(sender: {
  signalType: string;
  format: { frameWidth: string | null; frameHeight: string | null; grainRate: string | null };
  transportFile: { content: string } | null;
}): JSX.Element {
  const sdpContent = sender.transportFile?.content;

  if (sender.signalType === "Video") {
    const parsedVideo = parseVideoDetailsFromSdp(sdpContent);
    const resolution =
      parsedVideo.resolution ??
      (sender.format.frameWidth && sender.format.frameHeight
        ? `${sender.format.frameWidth}x${sender.format.frameHeight}`
        : "Unknown resolution");
    const frameRateValue = parsedVideo.frameRate ?? sender.format.grainRate;
    const frameRate = frameRateValue ? `${frameRateValue} FPS` : "Unknown FPS";
    const scanType = parsedVideo.scanType ?? "Unknown scan";
    return (
      <>
        <span>{resolution}</span>
        <span>{frameRate}</span>
        <span>{scanType}</span>
      </>
    );
  }

  if (sender.signalType === "Audio") {
    const channels = parseAudioChannels(sdpContent);
    return <span>{channels ? `Channels: ${channels}` : "Channels: Unknown"}</span>;
  }

  return <span>{sender.signalType}</span>;
}

function parseVideoDetailsFromSdp(content?: string): { resolution: string | null; frameRate: string | null; scanType: string | null } {
  if (!content) {
    return { resolution: null, frameRate: null, scanType: null };
  }

  let resolution: string | null = null;
  let frameRate: string | null = null;
  let scanType: string | null = null;
  if (!content) {
    return { resolution, frameRate, scanType };
  }

  const lines = content.split(/\r\n|\n/);
  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line.startsWith("a=fmtp:")) {
      continue;
    }

    const params = parseFmtpParams(line);
    if (!resolution && params.width && params.height) {
      resolution = `${params.width}x${params.height}`;
    }
    if (!frameRate) {
      frameRate = params.exactframerate ?? params.framerate ?? null;
    }
    if (!scanType) {
      scanType = /(?:^|[; ])interlace(?:[; ]|$)/i.test(line) ? "Interlaced" : "Progressive";
    }

    if (resolution && frameRate && scanType) {
      break;
    }
  }

  return { resolution, frameRate, scanType };
}

function parseAudioChannels(content?: string): string | null {
  if (!content) {
    return null;
  }

  const lines = content.split(/\r\n|\n/);
  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line.startsWith("a=rtpmap:")) {
      continue;
    }

    const mediaFormat = line.split(" ")[1];
    if (!mediaFormat) {
      continue;
    }
    const parts = mediaFormat.split("/");
    if (parts.length >= 3 && /^\d+$/.test(parts[2])) {
      return parts[2];
    }
  }

  return null;
}

function parseFmtpParams(line: string): Record<string, string> {
  const firstSpace = line.indexOf(" ");
  if (firstSpace === -1) {
    return {};
  }

  const payload = line.slice(firstSpace + 1);
  const entries = payload
    .split(";")
    .map((part) => part.trim())
    .filter(Boolean);

  const result: Record<string, string> = {};
  for (const entry of entries) {
    const eqIndex = entry.indexOf("=");
    if (eqIndex === -1) {
      continue;
    }

    const key = entry.slice(0, eqIndex).trim();
    const value = entry.slice(eqIndex + 1).trim();
    if (key) {
      result[key] = value;
    }
  }

  return result;
}
