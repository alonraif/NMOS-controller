import { Link, useParams } from "react-router-dom";
import { useResource } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { KeyValueList } from "../components/KeyValueList";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";

export function ResourceDetailPage() {
  const { resourceId = "" } = useParams();
  const resourceQuery = useResource(resourceId);

  if (resourceQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (resourceQuery.isError) {
    return <ErrorPanel message={resourceQuery.error.message} />;
  }

  if (!resourceQuery.data) {
    return <ErrorPanel message="Resource details are unavailable." />;
  }

  const resource = resourceQuery.data;
  const label = resolveResourceLabel(resource.payload) ?? resource.id;
  const parsedItems = buildParsedItems(resource.id, resource.kind, resource.payload);

  return (
    <div className="stack-xl">
      <PageHeader
        title="Resource Detail"
        subtitle="Raw normalized payload from the controller API."
        actions={
          <Link className="ghost-button" to="/inventory">
            Back to Inventory
          </Link>
        }
      />
      <Card title={label} subtitle={`Type: ${resource.kind}`}>
        <div className="two-column">
          <section className="detail-block">
            <h4>Parsed Summary</h4>
            <KeyValueList items={parsedItems} className="kv-list-compact" />
          </section>
          <section className="detail-block">
            <h4>Raw Payload</h4>
            <pre className="json-view">{JSON.stringify(resource.payload, null, 2)}</pre>
          </section>
        </div>
      </Card>
    </div>
  );
}

type KeyValueItem = { label: string; value: string };

function buildParsedItems(resourceId: string, kind: string, payload: unknown): KeyValueItem[] {
  const record = asRecord(payload);
  const items: KeyValueItem[] = [
    { label: "Label", value: getString(record, "label") ?? "-" },
    { label: "Kind", value: kind },
    { label: "ID", value: resourceId },
  ];

  appendIfPresent(items, "Device Type", getString(record, "deviceType"));
  appendIfPresent(items, "Node", getString(record, "nodeId"));
  appendIfPresent(items, "Device", getString(record, "deviceId"));
  appendIfPresent(items, "Source", getString(record, "sourceId"));
  appendIfPresent(items, "Flow", getString(record, "flowId"));
  appendIfPresent(items, "Transport", getString(record, "transport"));
  appendIfPresent(items, "Signal", getString(record, "signalType"));
  appendIfPresent(items, "Hostname", getString(record, "hostname"));
  appendIfPresent(items, "Description", getString(record, "description"));

  const senderIds = getStringArray(record, "senderIds");
  if (senderIds) {
    items.push({ label: "Senders", value: String(senderIds.length) });
  }

  const receiverIds = getStringArray(record, "receiverIds");
  if (receiverIds) {
    items.push({ label: "Receivers", value: String(receiverIds.length) });
  }

  const apiVersions = getStringArray(record, "apiVersions");
  if (apiVersions?.length) {
    items.push({ label: "API Versions", value: apiVersions.join(", ") });
  }

  const interfaces = getStringArray(record, "interfaces");
  if (interfaces) {
    items.push({ label: "Interfaces", value: String(interfaces.length) });
  }

  const format = asRecord(record?.format);
  if (format) {
    appendIfPresent(items, "Media Format", getString(format, "format"));
    appendIfPresent(items, "Media Type", getString(format, "mediaType"));
    const width = getString(format, "frameWidth");
    const height = getString(format, "frameHeight");
    if (width && height) {
      items.push({ label: "Resolution", value: `${width}x${height}` });
    }
    appendIfPresent(items, "Frame Rate", getString(format, "grainRate"));
    appendIfPresent(items, "Sample Rate", getString(format, "sampleRate"));
  }

  const transportFile = asRecord(record?.transportFile);
  if (transportFile) {
    appendIfPresent(items, "Transport File Type", getString(transportFile, "contentType"));

    const sdpContent = getString(transportFile, "content");
    if (sdpContent) {
      const sdpSummary = parseSdpSummary(sdpContent);
      appendIfPresent(items, "Session Name", sdpSummary.sessionName);
      appendIfPresent(items, "Origin Address", sdpSummary.originAddress);
      appendIfPresent(items, "Redundancy Group", sdpSummary.group);
      if (sdpSummary.streamCount > 0) {
        items.push({ label: "Media Streams", value: String(sdpSummary.streamCount) });
      }
      appendIfPresent(items, "PTP Ref Clock", sdpSummary.tsRefClock);
      appendIfPresent(items, "Media Clock", sdpSummary.mediaClock);
      appendIfPresent(items, "Destinations", sdpSummary.destinations);
      appendIfPresent(items, "Source Filters", sdpSummary.sourceFilters);
      appendIfPresent(items, "RTP Mapping", sdpSummary.rtpMaps);
      appendIfPresent(items, "Stream IDs", sdpSummary.mids);
      appendIfPresent(items, "Video Profile", sdpSummary.videoProfile);
      appendIfPresent(items, "Audio Profile", sdpSummary.audioProfile);
      appendIfPresent(items, "ST 2110 Details", sdpSummary.st2110Details);
    }
  }

  const updatedAt = getString(record, "updatedAtUtc");
  appendIfPresent(items, "Updated", updatedAt ? new Date(updatedAt).toLocaleString() : undefined);

  const lastSeenAt = getString(record, "lastSeenAtUtc");
  appendIfPresent(items, "Last Seen", lastSeenAt ? new Date(lastSeenAt).toLocaleString() : undefined);

  return items;
}

function resolveResourceLabel(payload: unknown): string | undefined {
  return getString(asRecord(payload), "label");
}

function appendIfPresent(items: KeyValueItem[], label: string, value?: string): void {
  if (value && value.trim().length > 0) {
    items.push({ label, value });
  }
}

function asRecord(value: unknown): Record<string, unknown> | undefined {
  return value && typeof value === "object" && !Array.isArray(value) ? (value as Record<string, unknown>) : undefined;
}

function getString(record: Record<string, unknown> | undefined, key: string): string | undefined {
  const value = record?.[key];
  return typeof value === "string" ? value : undefined;
}

function getStringArray(record: Record<string, unknown> | undefined, key: string): string[] | undefined {
  const value = record?.[key];
  if (!Array.isArray(value)) {
    return undefined;
  }

  const strings = value.filter((item): item is string => typeof item === "string");
  return strings.length > 0 ? strings : [];
}

interface SdpSummary {
  sessionName?: string;
  originAddress?: string;
  group?: string;
  streamCount: number;
  tsRefClock?: string;
  mediaClock?: string;
  destinations?: string;
  sourceFilters?: string;
  rtpMaps?: string;
  mids?: string;
  videoProfile?: string;
  audioProfile?: string;
  st2110Details?: string;
}

function parseSdpSummary(content: string): SdpSummary {
  const lines = content
    .split(/\r\n|\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  const destinations: string[] = [];
  const sourceFilters: string[] = [];
  const rtpMaps: string[] = [];
  const mids: string[] = [];
  const videoProfiles: string[] = [];
  const audioProfiles: string[] = [];
  const st2110Details = new Set<string>();

  let streamCount = 0;
  let currentMedia: "video" | "audio" | "other" = "other";
  let sessionName: string | undefined;
  let originAddress: string | undefined;
  let group: string | undefined;
  let tsRefClock: string | undefined;
  let mediaClock: string | undefined;

  for (const line of lines) {
    if (line.startsWith("s=")) {
      sessionName = line.slice(2).trim();
      continue;
    }

    if (line.startsWith("o=")) {
      const parts = line.slice(2).trim().split(/\s+/);
      originAddress = parts[parts.length - 1];
      continue;
    }

    if (line.startsWith("a=group:")) {
      group = line.slice("a=group:".length).trim();
      continue;
    }

    if (line.startsWith("m=")) {
      streamCount += 1;
      if (line.startsWith("m=video")) {
        currentMedia = "video";
      } else if (line.startsWith("m=audio")) {
        currentMedia = "audio";
      } else {
        currentMedia = "other";
      }
      continue;
    }

    if (line.startsWith("c=IN IP4 ")) {
      destinations.push(line.slice("c=IN IP4 ".length).trim());
      continue;
    }

    if (line.startsWith("a=source-filter:")) {
      sourceFilters.push(line.slice("a=source-filter:".length).trim());
      continue;
    }

    if (line.startsWith("a=rtpmap:")) {
      rtpMaps.push(line.slice("a=rtpmap:".length).trim());
      continue;
    }

    if (line.startsWith("a=mid:")) {
      mids.push(line.slice("a=mid:".length).trim());
      continue;
    }

    if (line.startsWith("a=ts-refclk:") && !tsRefClock) {
      tsRefClock = line.slice("a=ts-refclk:".length).trim();
      continue;
    }

    if (line.startsWith("a=mediaclk:") && !mediaClock) {
      mediaClock = line.slice("a=mediaclk:".length).trim();
      continue;
    }

    if (line.startsWith("a=fmtp:")) {
      const params = parseFmtpParams(line);
      const stParts = [
        params.SSN ? `SSN=${params.SSN}` : undefined,
        params.PM ? `PM=${params.PM}` : undefined,
        params.TP ? `TP=${params.TP}` : undefined,
        params.TCS ? `TCS=${params.TCS}` : undefined,
      ].filter((part): part is string => Boolean(part));

      for (const part of stParts) {
        st2110Details.add(part);
      }

      if (currentMedia === "video") {
        const width = params.width ?? "Unknown";
        const height = params.height ?? "Unknown";
        const frameRate = params.exactframerate ?? params.framerate ?? "Unknown";
        const depth = params.depth ? `${params.depth}-bit` : "Unknown bit depth";
        const sampling = params.sampling ?? "Unknown sampling";
        const colorimetry = params.colorimetry ?? "Unknown colorimetry";
        const videoProfile = `${width}x${height} @ ${frameRate}, ${depth}, ${sampling}, ${colorimetry}`;
        if (!videoProfiles.includes(videoProfile)) {
          videoProfiles.push(videoProfile);
        }
      } else if (currentMedia === "audio") {
        const channels = params.channels ?? params["channel-order"] ?? "Unknown channels";
        const packetTime = params.ptime ?? params.packettime;
        const audioProfile = packetTime ? `${channels}, ptime=${packetTime}` : channels;
        if (!audioProfiles.includes(audioProfile)) {
          audioProfiles.push(audioProfile);
        }
      }
    }
  }

  return {
    sessionName,
    originAddress,
    group,
    streamCount,
    tsRefClock,
    mediaClock,
    destinations: uniqueJoin(destinations),
    sourceFilters: uniqueJoin(sourceFilters),
    rtpMaps: uniqueJoin(rtpMaps),
    mids: uniqueJoin(mids),
    videoProfile: uniqueJoin(videoProfiles),
    audioProfile: uniqueJoin(audioProfiles),
    st2110Details: uniqueJoin(Array.from(st2110Details)),
  };
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

function uniqueJoin(values: string[]): string | undefined {
  const unique = Array.from(new Set(values.map((value) => value.trim()).filter(Boolean)));
  return unique.length > 0 ? unique.join(" | ") : undefined;
}
