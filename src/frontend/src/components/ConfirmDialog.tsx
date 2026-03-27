import { C } from "../styles/tokens";

type Props = {
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
};

export function ConfirmDialog({ message, onConfirm, onCancel }: Props) {
  return (
    <div className="glass-modal-backdrop" onClick={onCancel}>
      <div
        className="glass-modal-panel"
        onClick={(e) => e.stopPropagation()}
        style={{ width: "360px", maxWidth: "calc(100vw - 32px)", padding: "24px" }}
      >
        <div style={{ fontSize: "15px", fontWeight: 600, marginBottom: "20px", color: C.text }}>
          {message}
        </div>
        <div style={{ display: "flex", justifyContent: "flex-end", gap: "8px" }}>
          <button onClick={onCancel} className="glass-btn-ghost">Cancel</button>
          <button onClick={onConfirm} className="glass-btn-danger">Delete</button>
        </div>
      </div>
    </div>
  );
}
