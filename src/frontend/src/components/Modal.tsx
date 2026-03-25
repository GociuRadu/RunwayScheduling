import { C } from "../styles/tokens";

interface ModalProps {
  title: string;
  children: React.ReactNode;
  onClose: () => void;
}

export function Modal({ title, children, onClose }: ModalProps) {
  return (
    <div
      className="glass-modal-backdrop"
      onClick={onClose}
    >
      <div
        className="glass-modal-panel"
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "520px",
          maxWidth: "calc(100vw - 24px)",
          padding: "20px",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: "16px",
          }}
        >
          <div style={{ fontSize: "15px", fontWeight: 800, color: C.text }}>{title}</div>
          <button
            onClick={onClose}
            style={{
              background: "transparent",
              border: "none",
              color: C.textMuted,
              fontSize: "16px",
              cursor: "pointer",
              lineHeight: 1,
            }}
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
}
