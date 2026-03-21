import type { ReactNode } from "react";

interface StatusBadgeProps {
  tone?: "success" | "warning" | "danger" | "info" | "muted";
  children: ReactNode;
}

export function StatusBadge({ tone = "muted", children }: StatusBadgeProps) {
  return <span className={`status-badge tone-${tone}`}>{children}</span>;
}
