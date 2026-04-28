import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { EmptyState } from "../components/EmptyState";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";

const inventoryTypes = [
  { value: "Node", label: "Nodes" },
  { value: "Device", label: "Devices" },
  { value: "Sender", label: "Senders" },
  { value: "Receiver", label: "Receivers" },
  { value: "Flow", label: "Flows" },
] as const;
type InventoryType = (typeof inventoryTypes)[number];

type InventoryRow = {
  id: string;
  label: string;
  type: InventoryType["value"];
  parentLabel: string;
  parentId: string | null;
  isConnected: boolean | null;
};

type ResourceSummary = {
  title: string;
  lines: Array<{ label: string; value: string }>;
};

export function InventoryPage() {
  const [search, setSearch] = useState("");
  const [selectedTypes, setSelectedTypes] = useState<InventoryType["value"][]>([]);
  const topologyQuery = useTopology();

  const rows = useMemo<InventoryRow[]>(() => {
    const topology = topologyQuery.data;
    if (!topology) {
      return [];
    }

    const labelsById = new Map<string, string>([
      ...topology.nodes.map((item) => [item.id, item.label] as const),
      ...topology.devices.map((item) => [item.id, item.label] as const),
      ...topology.sources.map((item) => [item.id, item.label] as const),
      ...topology.flows.map((item) => [item.id, item.label] as const),
      ...topology.senders.map((item) => [item.id, item.label] as const),
      ...topology.receivers.map((item) => [item.id, item.label] as const),
    ]);
    const activeSenderIds = new Set(
      topology.receivers
        .map((receiver) => receiver.active.senderId)
        .filter((senderId): senderId is string => Boolean(senderId)),
    );

    return [
      ...topology.nodes.map((item) => ({
        id: item.id,
        label: item.label,
        type: "Node" as const,
        parentLabel: "-",
        parentId: null,
        isConnected: null,
      })),
      ...topology.devices.map((item) => ({
        id: item.id,
        label: item.label,
        type: "Device" as const,
        parentLabel: labelsById.get(item.nodeId) ?? item.nodeId,
        parentId: item.nodeId,
        isConnected: null,
      })),
      ...topology.flows.map((item) => ({
        id: item.id,
        label: item.label,
        type: "Flow" as const,
        parentLabel: labelsById.get(item.sourceId) ?? item.sourceId,
        parentId: item.sourceId,
        isConnected: null,
      })),
      ...topology.senders.map((item) => ({
        id: item.id,
        label: item.label,
        type: "Sender" as const,
        parentLabel: labelsById.get(item.deviceId) ?? item.deviceId,
        parentId: item.deviceId,
        isConnected: activeSenderIds.has(item.id),
      })),
      ...topology.receivers.map((item) => ({
        id: item.id,
        label: item.label,
        type: "Receiver" as const,
        parentLabel: labelsById.get(item.deviceId) ?? item.deviceId,
        parentId: item.deviceId,
        isConnected: Boolean(item.active.senderId),
      })),
    ]
      .filter((row) => selectedTypes.length === 0 || selectedTypes.includes(row.type as InventoryType["value"]))
      .filter((row) => row.label.toLowerCase().includes(search.toLowerCase()) || row.id.toLowerCase().includes(search.toLowerCase()))
      .sort((left, right) => left.type.localeCompare(right.type) || left.label.localeCompare(right.label));
  }, [search, selectedTypes, topologyQuery.data]);

  const resourceSummaries = useMemo(() => {
    const topology = topologyQuery.data;
    if (!topology) {
      return new Map<string, ResourceSummary>();
    }

    const labelsById = new Map<string, string>([
      ...topology.nodes.map((item) => [item.id, item.label] as const),
      ...topology.devices.map((item) => [item.id, item.label] as const),
      ...topology.flows.map((item) => [item.id, item.label] as const),
      ...topology.senders.map((item) => [item.id, item.label] as const),
      ...topology.receivers.map((item) => [item.id, item.label] as const),
      ...topology.sources.map((item) => [item.id, item.label] as const),
    ]);

    const summaries = new Map<string, ResourceSummary>();

    const sendersByNode = new Map<string, typeof topology.senders>();
    const receiversByNode = new Map<string, typeof topology.receivers>();
    const sendersByDevice = new Map<string, typeof topology.senders>();
    const receiversByDevice = new Map<string, typeof topology.receivers>();

    for (const sender of topology.senders) {
      const nodeSenders = sendersByNode.get(sender.nodeId) ?? [];
      nodeSenders.push(sender);
      sendersByNode.set(sender.nodeId, nodeSenders);

      const deviceSenders = sendersByDevice.get(sender.deviceId) ?? [];
      deviceSenders.push(sender);
      sendersByDevice.set(sender.deviceId, deviceSenders);
    }

    for (const receiver of topology.receivers) {
      const nodeReceivers = receiversByNode.get(receiver.nodeId) ?? [];
      nodeReceivers.push(receiver);
      receiversByNode.set(receiver.nodeId, nodeReceivers);

      const deviceReceivers = receiversByDevice.get(receiver.deviceId) ?? [];
      deviceReceivers.push(receiver);
      receiversByDevice.set(receiver.deviceId, deviceReceivers);
    }

    topology.nodes.forEach((node) => {
      const nodeSenders = sendersByNode.get(node.id) ?? [];
      const nodeReceivers = receiversByNode.get(node.id) ?? [];

      const videoSenders = nodeSenders.filter((sender) => sender.signalType === "Video").length;
      const audioSenders = nodeSenders.filter((sender) => sender.signalType === "Audio").length;
      const videoReceivers = nodeReceivers.filter((receiver) => receiver.signalType === "Video").length;
      const audioReceivers = nodeReceivers.filter((receiver) => receiver.signalType === "Audio").length;

      summaries.set(node.id, {
        title: node.label,
        lines: [
          { label: "Hostname", value: node.hostname ?? "-" },
          { label: "API Versions", value: node.apiVersions.join(", ") || "-" },
          { label: "Senders (V/A)", value: `${videoSenders}/${audioSenders}` },
          { label: "Receivers (V/A)", value: `${videoReceivers}/${audioReceivers}` },
          { label: "Interfaces", value: String(node.interfaces.length) },
          { label: "Last Seen", value: new Date(node.lastSeenAtUtc).toLocaleString() },
        ],
      });
    });

    topology.devices.forEach((device) => {
      const deviceSenders = sendersByDevice.get(device.id) ?? [];
      const deviceReceivers = receiversByDevice.get(device.id) ?? [];

      const videoSenders = deviceSenders.filter((sender) => sender.signalType === "Video").length;
      const audioSenders = deviceSenders.filter((sender) => sender.signalType === "Audio").length;
      const videoReceivers = deviceReceivers.filter((receiver) => receiver.signalType === "Video").length;
      const audioReceivers = deviceReceivers.filter((receiver) => receiver.signalType === "Audio").length;

      summaries.set(device.id, {
        title: device.label,
        lines: [
          { label: "Device Type", value: device.deviceType },
          { label: "Node", value: labelsById.get(device.nodeId) ?? device.nodeId },
          { label: "Senders (V/A)", value: `${videoSenders}/${audioSenders}` },
          { label: "Receivers (V/A)", value: `${videoReceivers}/${audioReceivers}` },
          { label: "Last Seen", value: new Date(device.lastSeenAtUtc).toLocaleString() },
        ],
      });
    });

    topology.senders.forEach((sender) => {
      const parsedSdp = parseSdpTechnicalDetails(sender.transportFile?.content);
      summaries.set(sender.id, {
        title: sender.label,
        lines: [
          { label: "Signal", value: sender.signalType },
          { label: "Video", value: formatVideoDetails(sender.format, parsedSdp.video) },
          { label: "Audio", value: formatAudioDetails(sender.format, parsedSdp.audio) },
          { label: "Transport", value: sender.transport },
          { label: "Flow", value: sender.flowId ? labelsById.get(sender.flowId) ?? sender.flowId : "-" },
          {
            label: "Subscribed Receiver",
            value: sender.subscribedReceiverId ? labelsById.get(sender.subscribedReceiverId) ?? sender.subscribedReceiverId : "-",
          },
          { label: "Last Seen", value: new Date(sender.lastSeenAtUtc).toLocaleString() },
        ],
      });
    });

    topology.receivers.forEach((receiver) => {
      const receiverTransportSdp = receiver.active.transportFile?.content ?? receiver.staged.transportFile?.content;
      const parsedSdp = parseSdpTechnicalDetails(receiverTransportSdp);
      summaries.set(receiver.id, {
        title: receiver.label,
        lines: [
          { label: "Signal", value: receiver.signalType },
          { label: "Video", value: formatVideoDetails(receiver.format, parsedSdp.video) },
          { label: "Audio", value: formatAudioDetails(receiver.format, parsedSdp.audio) },
          { label: "Transport", value: receiver.transport },
          { label: "Connectable", value: receiver.isConnectable ? "Yes" : "No" },
          {
            label: "Active Sender",
            value: receiver.active.senderId ? labelsById.get(receiver.active.senderId) ?? receiver.active.senderId : "-",
          },
          {
            label: "Staged Sender",
            value: receiver.staged.senderId ? labelsById.get(receiver.staged.senderId) ?? receiver.staged.senderId : "-",
          },
          { label: "Last Seen", value: new Date(receiver.lastSeenAtUtc).toLocaleString() },
        ],
      });
    });

    topology.flows.forEach((flow) => {
      summaries.set(flow.id, {
        title: flow.label,
        lines: [
          { label: "Signal", value: signalFromFormat(flow.format.format) },
          { label: "Video", value: formatVideoDetails(flow.format) },
          { label: "Audio", value: formatAudioDetails(flow.format) },
          { label: "Source", value: labelsById.get(flow.sourceId) ?? flow.sourceId },
          { label: "Device", value: labelsById.get(flow.deviceId) ?? flow.deviceId },
          { label: "Format", value: flow.format.format },
          { label: "Media Type", value: flow.format.mediaType ?? "-" },
          { label: "Last Seen", value: new Date(flow.lastSeenAtUtc).toLocaleString() },
        ],
      });
    });

    return summaries;
  }, [topologyQuery.data]);

  if (topologyQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (topologyQuery.isError) {
    return <ErrorPanel message={topologyQuery.error.message} />;
  }

  function toggleType(type: InventoryType["value"]) {
    setSelectedTypes((current) =>
      current.includes(type) ? current.filter((item) => item !== type) : [...current, type],
    );
  }

  return (
    <div className="stack-xl">
      <PageHeader title="Inventory / Topology" subtitle="Normalized graph view across nodes, devices, flows, senders, and receivers." />
      <Card title="Inventory">
        <div className="inventory-card-body">
          <div className="inventory-toolbar">
            <SearchInput value={search} onChange={setSearch} placeholder="Search resources" />
            <div className="inventory-type-filters">
              <span>Show only</span>
              <div className="inventory-type-options">
                {inventoryTypes.map((type) => (
                  <label key={type.value} className="inventory-type-option">
                    <input
                      type="checkbox"
                      checked={selectedTypes.includes(type.value)}
                      onChange={() => toggleType(type.value)}
                    />
                    {type.label}
                  </label>
                ))}
              </div>
            </div>
          </div>
          {rows.length === 0 ? (
            <EmptyState title="No matching resources" description="Adjust the search term or refresh the topology." />
          ) : (
            <table className="data-table">
              <thead>
                <tr>
                  <th>Type</th>
                  <th>Label</th>
                  <th>Connected</th>
                  <th>ID</th>
                  <th>Parent</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={`${row.type}-${row.id}`}>
                    <td>{row.type}</td>
                    <td>
                      <ResourceHoverLink row={row} summary={resourceSummaries.get(row.id)} />
                    </td>
                    <td>
                      <ConnectedIndicator isConnected={row.isConnected} />
                    </td>
                    <td className="mono">{row.id}</td>
                    <td>
                      <ParentHoverValue label={row.parentLabel} parentId={row.parentId} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </div>
  );
}

interface ConnectedIndicatorProps {
  isConnected: boolean | null;
}

function ConnectedIndicator({ isConnected }: ConnectedIndicatorProps) {
  if (isConnected === null) {
    return null;
  }

  return (
    <span className="connected-dot-wrap" title={isConnected ? "Connected" : "Not connected"}>
      <span className={`connected-dot ${isConnected ? "is-active" : "is-inactive"}`} />
    </span>
  );
}

interface ParentHoverValueProps {
  label: string;
  parentId: string | null;
}

function ParentHoverValue({ label, parentId }: ParentHoverValueProps) {
  return (
    <span className="parent-hover-value">
      {label}
      {parentId ? <span className="parent-id-popup">{parentId}</span> : null}
    </span>
  );
}

function signalFromFormat(format: string): string {
  if (format.includes("video")) {
    return "Video";
  }

  if (format.includes("audio")) {
    return "Audio";
  }

  if (format.includes("data")) {
    return "Ancillary";
  }

  return "Unknown";
}

function formatVideoDetails(
  format: { frameWidth: string | null; frameHeight: string | null; grainRate: string | null; format: string },
  parsedFromSdp?: string,
): string {
  if (parsedFromSdp) {
    return parsedFromSdp;
  }

  if (!format.format.includes("video")) {
    return "-";
  }

  const resolution = format.frameWidth && format.frameHeight ? `${format.frameWidth}x${format.frameHeight}` : "Unknown";
  const frameRate = format.grainRate ?? "Unknown";
  return `${resolution} @ ${frameRate}`;
}

function formatAudioDetails(
  format: { sampleRate: string | null; mediaType: string | null; format: string },
  parsedFromSdp?: string,
): string {
  if (parsedFromSdp) {
    return parsedFromSdp;
  }

  if (!format.format.includes("audio")) {
    return "-";
  }

  const sampleRate = format.sampleRate ?? "Unknown";
  const mediaType = format.mediaType ?? "Unknown";
  return `${sampleRate}, ${mediaType}`;
}

function parseSdpTechnicalDetails(content?: string): { video?: string; audio?: string } {
  if (!content) {
    return {};
  }

  const lines = content.split(/\r\n|\n/);
  let currentMedia: "video" | "audio" | null = null;
  let video: string | undefined;
  let audio: string | undefined;

  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (!line) {
      continue;
    }

    if (line.startsWith("m=")) {
      if (line.startsWith("m=video")) {
        currentMedia = "video";
      } else if (line.startsWith("m=audio")) {
        currentMedia = "audio";
      } else {
        currentMedia = null;
      }
      continue;
    }

    if (currentMedia === "video" && line.startsWith("a=fmtp:") && !video) {
      const fmtp = parseFmtpParams(line);
      const width = fmtp.width ?? "Unknown";
      const height = fmtp.height ?? "Unknown";
      const frameRate = fmtp.exactframerate ?? fmtp.framerate ?? "Unknown";
      const depth = fmtp.depth ? `${fmtp.depth}-bit` : "Unknown bit depth";
      const sampling = fmtp.sampling ?? "Unknown sampling";
      const colorimetry = fmtp.colorimetry ?? "Unknown colorimetry";
      video = `${width}x${height} @ ${frameRate}, ${depth}, ${sampling}, ${colorimetry}`;
      continue;
    }

    if (currentMedia === "audio") {
      if (line.startsWith("a=rtpmap:") && !audio) {
        const rtpmap = line.slice("a=rtpmap:".length);
        const slashIndex = rtpmap.indexOf(" ");
        if (slashIndex > -1) {
          const mediaFormat = rtpmap.slice(slashIndex + 1).trim(); // e.g. L24/48000/2
          const parts = mediaFormat.split("/");
          const codec = parts[0] ?? "Unknown";
          const sampleRate = parts[1] ?? "Unknown";
          const channels = parts[2] ? `${parts[2]}ch` : "Unknown channels";
          audio = `${codec}, ${sampleRate} Hz, ${channels}`;
        }
      }

      if (line.startsWith("a=fmtp:") && audio) {
        const fmtp = parseFmtpParams(line);
        if (fmtp["channel-order"]) {
          audio = `${audio}, ${fmtp["channel-order"]}`;
        }
      }
    }
  }

  return { video, audio };
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

interface ResourceHoverLinkProps {
  row: InventoryRow;
  summary?: ResourceSummary;
}

function ResourceHoverLink({ row, summary }: ResourceHoverLinkProps) {
  return (
    <div className="inventory-resource-hover">
      <Link className="table-link" to={`/resources/${row.id}`}>
        {row.label}
      </Link>
      {summary ? (
        <div className="inventory-resource-popup">
          <p className="inventory-resource-popup-title">{summary.title}</p>
          <dl className="inventory-resource-popup-list">
            {summary.lines.map((line) => (
              <div key={`${row.id}-${line.label}`}>
                <dt>{line.label}</dt>
                <dd>{line.value}</dd>
              </div>
            ))}
          </dl>
        </div>
      ) : null}
    </div>
  );
}
