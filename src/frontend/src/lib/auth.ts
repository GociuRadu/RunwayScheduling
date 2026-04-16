export const AUTH_TOKEN_STORAGE_KEY = "token";

const AUTH_CHANGED_EVENT = "auth:changed";
const SESSION_STORAGE_KEYS = [
  AUTH_TOKEN_STORAGE_KEY,
  "selectedAirportId",
  "selectedAirportName",
  "selectedScenarioId",
  "selectedScenarioName",
] as const;

type AuthSnapshot = {
  token: string | null;
  expiresAt: number | null;
  isAuthenticated: boolean;
};

export function getValidAccessToken(): string | null {
  const snapshot = getAuthSnapshot();

  if (!snapshot.isAuthenticated && readStoredToken()) {
    clearAuthSession();
    return null;
  }

  return snapshot.token;
}

export function storeAccessToken(token: string): void {
  window.localStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
  notifyAuthChanged();
}

export function clearAuthSession(): void {
  for (const key of SESSION_STORAGE_KEYS) {
    window.localStorage.removeItem(key);
  }

  notifyAuthChanged();
}

let _cachedSnapshot: AuthSnapshot = { token: null, expiresAt: null, isAuthenticated: false };

export function getAuthSnapshot(): AuthSnapshot {
  const token = readStoredToken();

  let next: AuthSnapshot;
  if (!token) {
    next = { token: null, expiresAt: null, isAuthenticated: false };
  } else {
    const expiresAt = getTokenExpiry(token);
    const isExpired = expiresAt !== null && expiresAt <= Date.now();
    next = {
      token: isExpired ? null : token,
      expiresAt: isExpired ? null : expiresAt,
      isAuthenticated: !isExpired,
    };
  }

  if (
    next.token === _cachedSnapshot.token &&
    next.expiresAt === _cachedSnapshot.expiresAt &&
    next.isAuthenticated === _cachedSnapshot.isAuthenticated
  ) {
    return _cachedSnapshot;
  }

  _cachedSnapshot = next;
  return _cachedSnapshot;
}

export function subscribeToAuthChanges(listener: () => void): () => void {
  const handleAuthChanged = () => listener();
  const handleStorage = (event: StorageEvent) => {
    if (!event.key || SESSION_STORAGE_KEYS.includes(event.key as (typeof SESSION_STORAGE_KEYS)[number])) {
      listener();
    }
  };

  window.addEventListener(AUTH_CHANGED_EVENT, handleAuthChanged);
  window.addEventListener("storage", handleStorage);

  return () => {
    window.removeEventListener(AUTH_CHANGED_EVENT, handleAuthChanged);
    window.removeEventListener("storage", handleStorage);
  };
}

function readStoredToken(): string | null {
  return window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
}

function getTokenExpiry(token: string): number | null {
  try {
    const [, payload] = token.split(".");
    if (!payload) {
      return null;
    }

    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/");
    const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), "=");
    const parsed = JSON.parse(window.atob(padded)) as { exp?: unknown };

    return typeof parsed.exp === "number" ? parsed.exp * 1000 : null;
  } catch {
    return null;
  }
}

function notifyAuthChanged(): void {
  window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
}
