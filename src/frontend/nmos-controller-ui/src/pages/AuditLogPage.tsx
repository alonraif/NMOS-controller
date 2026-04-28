import { useMemo, useState } from "react";
import { useAudit, useReceivers, useSenders, useTopology } from "../api/hooks";
import { Card } from "../components/Card";
import { ErrorPanel } from "../components/ErrorPanel";
import { LoadingPanel } from "../components/LoadingPanel";
import { PageHeader } from "../components/PageHeader";
import { SearchInput } from "../components/SearchInput";

export function AuditLogPage() {
  const [search, setSearch] = useState("");
  const [expandedEntryIds, setExpandedEntryIds] = useState<Set<string>>(new Set());
  const [expandedGroupIds, setExpandedGroupIds] = useState<Set<string>>(new Set());
  const auditQuery = useAudit(250);
  const topologyQuery = useTopology();
  const sendersQuery = useSenders();
  const receiversQuery = useReceivers();
  const resourceLabelsById = useMemo(() => {
    const topology = topologyQuery.data;
    const senders = sendersQuery.data ?? [];
    const receivers = receiversQuery.data ?? [];
    if (!topology && senders.length === 0 && receivers.length === 0) {
      return new Map<string, string>();
    }

    return new Map<string, string>([
      ...(topology?.nodes ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.devices ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.sources ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.flows ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.senders ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.receivers ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...(topology?.routingDestinations ?? []).map((item) => [item.id.toLowerCase(), item.label] as const),
      ...senders.map((item) => [item.id.toLowerCase(), item.label] as const),
      ...receivers.map((item) => [item.id.toLowerCase(), item.label] as const),
    ]);
  }, [receiversQuery.data, sendersQuery.data, topologyQuery.data]);

  const entries = useMemo(() => {
    const source = auditQuery.data ?? [];
    return source.filter((entry) => {
      const term = search.toLowerCase();
      const metadataText = formatMetadataSearchText(entry.metadataJson, resourceLabelsById);
      return (
        formatAuditSummary(entry.summary, resourceLabelsById).toLowerCase().includes(term) ||
        entry.actor.toLowerCase().includes(term) ||
        (entry.resourceId ?? "").toLowerCase().includes(term) ||
        metadataText.includes(term)
      );
    });
  }, [auditQuery.data, resourceLabelsById, search]);

  const groupedEntries = useMemo(() => {
    const groups = new Map<string, { id: string; correlationId: string | null; entries: typeof entries }>();

    for (const entry of entries) {
      const key = entry.correlationId ?? entry.id;
      const existing = groups.get(key);
      if (existing) {
        existing.entries.push(entry);
        continue;
      }

      groups.set(key, { id: key, correlationId: entry.correlationId, entries: [entry] });
    }

    return Array.from(groups.values());
  }, [entries]);

  if (auditQuery.isLoading) {
    return <LoadingPanel />;
  }

  if (auditQuery.isError) {
    return <ErrorPanel message={auditQuery.error.message} />;
  }

  return (
    <div className="stack-xl">
      <PageHeader title="History" subtitle="Recent controller actions, route validations, preset execution, and operator events." />
      <Card
        className="dashboard-history-card"
        title="History"
        subtitle="Search by actor, summary, or resource ID."
        actions={<SearchInput value={search} onChange={setSearch} placeholder="Search history" />}
      >
        <div className="history-panel">
          <div className="stack audit-terminal history-list">
            {groupedEntries.map((group) => {
              const leadEntry = group.entries[0];
              const isExpanded = group.correlationId ? expandedGroupIds.has(group.id) : true;
              const leadTone = resolveEntryTone(leadEntry.actionType);
              return (
              <div key={group.id} className={`stack-sm history-group tone-${leadTone}`}>
                <div className={`audit-line tone-${leadTone}`}>
                  <time className="audit-ts">{new Date(leadEntry.occurredAtUtc).toLocaleString()}</time>
                  <span className="audit-prompt">$</span>
                  <strong className="audit-summary">{formatAuditSummary(leadEntry.summary, resourceLabelsById)}</strong>
                  {group.entries.length > 1 ? <span className="muted-copy">({group.entries.length} events)</span> : null}
                  {group.correlationId ? (
                    <button
                      type="button"
                      className="ghost-button history-group-toggle"
                      onClick={() =>
                        setExpandedGroupIds((current) => {
                          const next = new Set(current);
                          if (next.has(group.id)) {
                            next.delete(group.id);
                          } else {
                            next.add(group.id);
                          }
                          return next;
                        })
                      }
                    >
                      {isExpanded ? "Hide Events" : "Show Events"}
                    </button>
                  ) : null}
                </div>

                {isExpanded ? (
                  <div className="stack-sm">
                    {group.entries.map((entry) => (
                      <div key={entry.id} className="stack-sm">
                        {group.entries.length > 1 ? (
                          <div className={`audit-line tone-${resolveEntryTone(entry.actionType)}`}>
                            <time className="audit-ts">{new Date(entry.occurredAtUtc).toLocaleString()}</time>
                            <span className="audit-prompt">$</span>
                            <strong className="audit-summary">{formatAuditSummary(entry.summary, resourceLabelsById)}</strong>
                          </div>
                        ) : null}
                        {entry.metadataJson ? (
                          <div className="history-metadata">
                            <div className="history-metadata-toggle-row">
                              <button
                                type="button"
                                className="ghost-button"
                                onClick={() =>
                                  setExpandedEntryIds((current) => {
                                    const next = new Set(current);
                                    if (next.has(entry.id)) {
                                      next.delete(entry.id);
                                    } else {
                                      next.add(entry.id);
                                    }
                                    return next;
                                  })
                                }
                              >
                                {expandedEntryIds.has(entry.id) ? "Hide Details" : "Show Details"}
                              </button>
                            </div>
                            {expandedEntryIds.has(entry.id) ? (
                              <dl className="kv-list kv-list-compact">
                                {formatMeaningfulDetails(entry, resourceLabelsById).map(([key, value]) => (
                                  <div key={`${entry.id}-${key}`} className="kv-row">
                                    <dt className={resolveDetailKeyClass(key)}>{key}</dt>
                                    <dd className="mono">{value}</dd>
                                  </div>
                                ))}
                              </dl>
                            ) : null}
                          </div>
                        ) : null}
                      </div>
                    ))}
                  </div>
                ) : null}
              </div>
            )})}
          </div>
        </div>
      </Card>
    </div>
  );
}

function formatAuditSummary(summary: string, labelsById: Map<string, string>): string {
  return summary.replace(/\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi, (id) => {
    return labelsById.get(id.toLowerCase()) ?? id;
  });
}

function formatMetadataEntries(metadataJson: string, labelsById: Map<string, string>): Array<[string, string]> {
  const parsed = parseMetadataObject(metadataJson);
  if (!parsed) {
    return [["metadata", resolveIdsInText(metadataJson, labelsById)]];
  }

  return Object.entries(parsed).map(([key, value]) => [toFriendlyKeyLabel(key), resolveMetadataValue(value, labelsById)]);
}

function resolveMetadataValue(value: unknown, labelsById: Map<string, string>): string {
  if (typeof value === "string") {
    return resolveIdsInText(value, labelsById);
  }

  if (value === null || value === undefined) {
    return String(value);
  }

  try {
    const serialized = JSON.stringify(value);
    return resolveIdsInText(serialized ?? String(value), labelsById);
  } catch {
    return resolveIdsInText(String(value), labelsById);
  }
}

function resolveIdsInText(value: string, labelsById: Map<string, string>): string {
  return value.replace(/\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi, (id) => {
    return labelsById.get(id.toLowerCase()) ?? id;
  });
}

function formatMetadataSearchText(metadataJson: string | null, labelsById: Map<string, string>): string {
  if (!metadataJson) {
    return "";
  }

  return formatMetadataEntries(metadataJson, labelsById)
    .map(([key, value]) => `${key} ${value}`)
    .join(" ")
    .toLowerCase();
}

function parseMetadataObject(metadataJson: string): Record<string, unknown> | null {
  try {
    const parsed = JSON.parse(metadataJson) as Record<string, unknown>;
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      return null;
    }
    return parsed;
  } catch {
    return null;
  }
}

function formatMeaningfulDetails(
  entry: { actionType: string; actor: string; resourceId: string | null; resourceType: string | null; correlationId: string | null; metadataJson: string | null },
  labelsById: Map<string, string>,
): Array<[string, string]> {
  const details: Array<[string, string]> = [];
  const metadata = entry.metadataJson ? parseMetadataObject(entry.metadataJson) : null;

  if (entry.actor) {
    details.push(["Actor", entry.actor]);
  }
  if (entry.resourceType) {
    details.push(["Resource Type", entry.resourceType]);
  }
  if (entry.resourceId) {
    details.push(["Resource", resolveIdsInText(entry.resourceId, labelsById)]);
  }
  if (entry.correlationId) {
    details.push(["Correlation", entry.correlationId]);
  }

  if (metadata) {
    const receiverId = getString(metadata, "ReceiverId") ?? getString(metadata, "receiverId");
    const senderId = getString(metadata, "SenderId") ?? getString(metadata, "senderId");
    const previousSenderId = getString(metadata, "PreviousSenderId");
    const newSenderId = getString(metadata, "NewSenderId");
    const destinationId = getString(metadata, "DestinationId") ?? getString(metadata, "destinationId");
    const layer = getString(metadata, "Layer") ?? getString(metadata, "layer");
    const mode = getString(metadata, "Mode") ?? getString(metadata, "ActivationMode");
    const reason = getString(metadata, "Reason");
    const status = getString(metadata, "Status") ?? getString(metadata, "assessment.Status");

    if (receiverId) {
      details.push(["Receiver", resolveIdsInText(receiverId, labelsById)]);
    }
    if (senderId) {
      details.push(["Sender", resolveIdsInText(senderId, labelsById)]);
    }
    if (destinationId) {
      details.push(["Destination", resolveIdsInText(destinationId, labelsById)]);
    }
    if (layer) {
      details.push(["Layer", layer]);
    }
    if (mode) {
      details.push(["Activation", mode]);
    }
    if (status) {
      details.push(["Validation", status]);
    }
    if (reason) {
      details.push(["Failure Reason", reason]);
    }
    if (previousSenderId !== undefined || newSenderId !== undefined) {
      details.push([
        "Sender Change",
        `${resolveIdsInText(previousSenderId ?? "None", labelsById)} -> ${resolveIdsInText(newSenderId ?? "None", labelsById)}`,
      ]);
    }

    // Add remaining metadata fields, excluding ones already surfaced.
    const handledKeys = new Set([
      "ReceiverId", "receiverId",
      "SenderId", "senderId",
      "PreviousSenderId", "NewSenderId",
      "DestinationId", "destinationId",
      "Layer", "layer",
      "Mode", "ActivationMode",
      "Reason",
      "Status",
    ]);

    for (const [key, value] of Object.entries(metadata)) {
      if (handledKeys.has(key)) {
        continue;
      }
      details.push([toFriendlyKeyLabel(key), resolveMetadataValue(value, labelsById)]);
    }
  } else if (entry.metadataJson) {
    details.push(["Metadata", resolveIdsInText(entry.metadataJson, labelsById)]);
  }

  return dedupeDetails(details);
}

function getString(source: Record<string, unknown>, key: string): string | undefined {
  const value = source[key];
  return typeof value === "string" ? value : undefined;
}

function toFriendlyKeyLabel(key: string): string {
  return key
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .replace(/\s+/g, " ")
    .trim()
    .replace(/^./, (char) => char.toUpperCase());
}

function dedupeDetails(details: Array<[string, string]>): Array<[string, string]> {
  const seen = new Set<string>();
  const result: Array<[string, string]> = [];
  for (const [key, value] of details) {
    const signature = `${key}:${value}`;
    if (seen.has(signature)) {
      continue;
    }
    seen.add(signature);
    result.push([key, value]);
  }
  return result;
}

function resolveEntryTone(actionType: string): "success" | "warning" | "danger" | "neutral" {
  const normalized = actionType.trim().toLowerCase();
  if (
    normalized.includes("failed") ||
    normalized === "apirequestfailed" ||
    normalized === "routerequestfailed" ||
    normalized === "topologyrefreshfailed"
  ) {
    return "danger";
  }
  if (
    normalized === "validationfailedblocking" ||
    normalized === "connectionvalidated"
  ) {
    return "warning";
  }
  if (
    normalized.includes("completed") ||
    normalized.includes("connected") ||
    normalized.includes("disconnected") ||
    normalized.includes("started") ||
    normalized.includes("refreshed") ||
    normalized.includes("connectivitychanged") ||
    normalized.includes("statechanged")
  ) {
    return "success";
  }
  return "neutral";
}

function resolveDetailKeyClass(key: string): string | undefined {
  const normalized = key.trim().toLowerCase();
  if (normalized.includes("failure") || normalized.includes("error")) {
    return "history-detail-key-danger";
  }
  if (normalized.includes("validation")) {
    return "history-detail-key-warning";
  }
  if (normalized.includes("change") || normalized.includes("completed")) {
    return "history-detail-key-success";
  }
  return undefined;
}
