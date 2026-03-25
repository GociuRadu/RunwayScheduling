import { useNavigate } from "react-router-dom";
import { C } from "../styles/tokens";

export default function HomePage() {
  const navigate = useNavigate();
  const isLoggedIn = !!localStorage.getItem("token");

  return (
    <div
      style={{
        minHeight: "calc(100vh - 56px)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        position: "relative",
        overflow: "hidden",
      }}
    >
      {/* Glow orb */}
      <div
        style={{
          position: "absolute",
          top: "-60px",
          left: "50%",
          transform: "translateX(-50%)",
          width: "600px",
          height: "400px",
          background: "radial-gradient(ellipse, rgba(249,115,22,0.1) 0%, transparent 65%)",
          pointerEvents: "none",
        }}
      />

      <div style={{ textAlign: "center", position: "relative", maxWidth: "560px", padding: "0 24px" }}>
        {/* Badge */}
        <div
          style={{
            display: "inline-flex",
            alignItems: "center",
            gap: "8px",
            background: "rgba(249,115,22,0.1)",
            border: "1px solid rgba(249,115,22,0.2)",
            borderRadius: "20px",
            padding: "5px 14px",
            marginBottom: "24px",
          }}
        >
          <span
            style={{
              width: "7px",
              height: "7px",
              borderRadius: "50%",
              background: C.primary,
              display: "inline-block",
              animation: "glowPulse 2s ease-in-out infinite",
            }}
          />
          <span style={{ color: C.primary, fontSize: "10px", fontWeight: 600, letterSpacing: "1.5px" }}>
            AIRPORT RUNWAY SCHEDULING
          </span>
        </div>

        {/* Title */}
        <h1
          style={{
            color: C.text,
            fontSize: "42px",
            fontWeight: 900,
            margin: "0 0 14px",
            lineHeight: 1.1,
          }}
        >
          Optimize every{" "}
          <span
            style={{
              background: "linear-gradient(90deg, #dc2626, #f97316)",
              WebkitBackgroundClip: "text",
              WebkitTextFillColor: "transparent",
              backgroundClip: "text",
            }}
          >
            runway operation
          </span>
        </h1>

        {/* Subtitle */}
        <p
          style={{
            color: C.textSub,
            fontSize: "14px",
            margin: "0 0 32px",
            lineHeight: 1.6,
          }}
        >
          Greedy scheduling simulation for airport runway management.
          Configure scenarios, generate flights, and solve.
        </p>

        {/* CTA buttons */}
        {isLoggedIn ? (
          <div style={{ display: "flex", gap: "12px", justifyContent: "center" }}>
            <button className="glass-btn-primary" onClick={() => navigate("/airports")}>
              Airports →
            </button>
            <button className="glass-btn-ghost" onClick={() => navigate("/scenario-config")}>
              Scenarios
            </button>
          </div>
        ) : (
          <div style={{ display: "flex", gap: "12px", justifyContent: "center" }}>
            <button className="glass-btn-primary" style={{ padding: "11px 24px", fontSize: "14px" }} onClick={() => navigate("/airports")}>
              Get started →
            </button>
            <button className="glass-btn-ghost" style={{ padding: "11px 24px", fontSize: "14px" }} onClick={() => navigate("/airports")}>
              View airports
            </button>
          </div>
        )}

        {/* Feature pills */}
        <div style={{ display: "flex", gap: "10px", justifyContent: "center", marginTop: "36px", flexWrap: "wrap" }}>
          {["✈ Multi-runway", "⛅ Weather simulation", "⚡ Greedy solver"].map((pill) => (
            <span
              key={pill}
              style={{
                background: "rgba(255,255,255,0.03)",
                border: "1px solid rgba(255,255,255,0.07)",
                borderRadius: "8px",
                padding: "6px 14px",
                fontSize: "11px",
                color: C.textSub,
              }}
            >
              {pill}
            </span>
          ))}
        </div>
      </div>
    </div>
  );
}
