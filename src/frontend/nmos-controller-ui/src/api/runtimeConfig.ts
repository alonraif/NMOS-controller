const API_ROOT_STORAGE_KEY = "nmos_controller_api_root";
const DEFAULT_API_ROOT = "/api/v1";

function normalizeApiRoot(rawValue: string): string {
  const trimmed = rawValue.trim();
  if (!trimmed) {
    return DEFAULT_API_ROOT;
  }

  if (trimmed.startsWith("http://") || trimmed.startsWith("https://")) {
    try {
      const parsed = new URL(trimmed);
      if (parsed.pathname === "/" || parsed.pathname === "") {
        parsed.pathname = "/api/v1";
      }

      return parsed.toString().replace(/\/+$/, "");
    } catch {
      return DEFAULT_API_ROOT;
    }
  }

  if (trimmed.startsWith("/")) {
    return trimmed.replace(/\/+$/, "") || DEFAULT_API_ROOT;
  }

  return `/${trimmed.replace(/\/+$/, "")}`;
}

export function getApiRoot(): string {
  const stored = window.localStorage.getItem(API_ROOT_STORAGE_KEY);
  return stored ? normalizeApiRoot(stored) : DEFAULT_API_ROOT;
}

export function setApiRoot(value: string): void {
  const normalized = normalizeApiRoot(value);
  if (normalized === DEFAULT_API_ROOT) {
    window.localStorage.removeItem(API_ROOT_STORAGE_KEY);
    return;
  }

  window.localStorage.setItem(API_ROOT_STORAGE_KEY, normalized);
}
