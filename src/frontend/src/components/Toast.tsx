import { useCallback, useRef, useState } from "react";
import { ToastContext } from "../hooks/useToast";
import type { ToastItem, ToastType } from "../hooks/useToast";
import { C } from "../styles/tokens";

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([]);
  const counter = useRef(0);

  const showToast = useCallback((message: string, type: ToastType = "info") => {
    const id = ++counter.current;
    setToasts((prev) => [...prev, { id, message, type }]);
    setTimeout(() => {
      setToasts((prev) => prev.filter((t) => t.id !== id));
    }, 3500);
  }, []);

  return (
    <ToastContext.Provider value={{ showToast }}>
      {children}
      <ToastContainer toasts={toasts} onDismiss={(id) => setToasts((p) => p.filter((t) => t.id !== id))} />
    </ToastContext.Provider>
  );
}

function ToastContainer({ toasts, onDismiss }: { toasts: ToastItem[]; onDismiss: (id: number) => void }) {
  if (toasts.length === 0) return null;

  return (
    <div
      style={{
        position: "fixed",
        bottom: "24px",
        right: "24px",
        zIndex: 9999,
        display: "flex",
        flexDirection: "column",
        gap: "8px",
      }}
    >
      {toasts.map((t) => (
        <div
          key={t.id}
          onClick={() => onDismiss(t.id)}
          style={{
            background: C.bgCard,
            border: `1px solid ${t.type === "error" ? C.borderAccentRed : t.type === "success" ? "rgba(249,115,22,0.35)" : C.border}`,
            borderRadius: "6px",
            padding: "12px 16px",
            color: t.type === "error" ? "#ff9d9d" : t.type === "success" ? C.primary : C.textSub,
            fontSize: "13px",
            cursor: "pointer",
            maxWidth: "360px",
            animation: "toast-in 0.25s ease",
            display: "flex",
            alignItems: "center",
            gap: "8px",
          }}
        >
          <span style={{ fontSize: "12px" }}>
            {t.type === "error" ? "✕" : t.type === "success" ? "✓" : "●"}
          </span>
          {t.message}
        </div>
      ))}
    </div>
  );
}
