import { C } from "../styles/tokens";

interface SkeletonProps {
  width?: string | number;
  height?: string | number;
  borderRadius?: string | number;
  style?: React.CSSProperties;
}

export function Skeleton({ width = "100%", height = "16px", borderRadius = "4px", style }: SkeletonProps) {
  return (
    <div
      style={{
        width,
        height,
        borderRadius,
        background: C.border,
        animation: "skeleton-pulse 1.2s ease-in-out infinite",
        ...style,
      }}
    />
  );
}

export function SkeletonCard() {
  return (
    <div
      style={{
        background: C.bgCard,
        border: `1px solid ${C.border}`,
        borderRadius: "8px",
        padding: "16px",
        display: "flex",
        flexDirection: "column",
        gap: "10px",
      }}
    >
      <Skeleton width="40%" height="14px" />
      <Skeleton width="70%" height="10px" />
      <Skeleton width="55%" height="10px" />
    </div>
  );
}

export function SkeletonStatRow() {
  return (
    <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: "8px" }}>
      {[0, 1, 2].map((i) => (
        <div
          key={i}
          style={{
            background: C.bgCard,
            border: `1px solid ${C.border}`,
            borderRadius: "6px",
            padding: "10px",
            display: "flex",
            flexDirection: "column",
            gap: "6px",
          }}
        >
          <Skeleton width="60%" height="8px" />
          <Skeleton width="40%" height="20px" />
        </div>
      ))}
    </div>
  );
}
