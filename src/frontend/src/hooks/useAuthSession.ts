import { useEffect, useSyncExternalStore } from "react";
import {
  AUTH_TOKEN_STORAGE_KEY,
  clearAuthSession,
  getAuthSnapshot,
  subscribeToAuthChanges,
} from "../lib/auth";

export function useAuthSession() {
  const snapshot = useSyncExternalStore(
    subscribeToAuthChanges,
    getAuthSnapshot,
    getAuthSnapshot,
  );

  useEffect(() => {
    const hasStoredToken = !!window.localStorage.getItem(AUTH_TOKEN_STORAGE_KEY);
    if (hasStoredToken && !snapshot.isAuthenticated) {
      clearAuthSession();
      return;
    }

    if (!snapshot.expiresAt) {
      return;
    }

    const remainingMs = snapshot.expiresAt - Date.now();
    const timeoutId = window.setTimeout(() => {
      clearAuthSession();
    }, Math.max(remainingMs, 0));

    return () => window.clearTimeout(timeoutId);
  }, [snapshot.expiresAt, snapshot.isAuthenticated]);

  return {
    ...snapshot,
    logout: clearAuthSession,
  };
}
