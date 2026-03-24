import { Outlet, useNavigate, useLocation } from "react-router-dom";
import { useState } from "react";
import LoginModal from "./Login";
import { C } from "../styles/tokens";

const NAV_ITEMS = [
  { label: "Airports", path: "/airports" },
  { label: "Scenarios", path: "/scenario-config" },
];

function Logo() {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
      <div
        style={{
          width: "28px",
          height: "28px",
          borderRadius: "6px",
          background: C.gradient,
          display: "flex",
          alignItems: "center",
          justifyContent: "center",
          fontSize: "14px",
          flexShrink: 0,
        }}
      >
        ✈
      </div>
      <span style={{ color: C.text, fontWeight: 800, fontSize: "14px", letterSpacing: "0.3px" }}>
        runway<span style={{ color: C.primary, fontWeight: 300 }}>sched</span>
      </span>
    </div>
  );
}

export default function AppShell() {
  const navigate = useNavigate();
  const location = useLocation();
  const [showLogin, setShowLogin] = useState(false);

  const isLoggedIn = !!localStorage.getItem("token");

  function handleLogout() {
    localStorage.removeItem("token");
    localStorage.removeItem("selectedAirportId");
    localStorage.removeItem("selectedAirportName");
    localStorage.removeItem("selectedScenarioId");
    localStorage.removeItem("selectedScenarioName");
    window.location.reload();
  }

  return (
    <div style={{ minHeight: "100vh", background: C.bg }}>
      <header
        style={{
          width: "100%",
          height: "56px",
          borderBottom: `1px solid ${C.border}`,
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "0 24px",
          boxSizing: "border-box",
          gap: "16px",
          position: "sticky",
          top: 0,
          zIndex: 100,
          background: C.bg,
        }}
      >
        <button
          onClick={() => navigate("/")}
          style={{ background: "transparent", border: "none", cursor: "pointer", padding: 0 }}
        >
          <Logo />
        </button>

        <nav style={{ display: "flex", gap: "4px" }}>
          {(!isLoggedIn ? [] : NAV_ITEMS).map((item) => {
            const active = location.pathname === item.path;
            return (
              <button
                key={item.path}
                onClick={() => navigate(item.path)}
                style={{
                  background: "transparent",
                  border: "none",
                  color: active ? C.primary : C.textSub,
                  fontSize: "13px",
                  fontWeight: active ? 700 : 400,
                  cursor: "pointer",
                  padding: "6px 12px",
                  borderRadius: "5px",
                  borderBottom: active ? `2px solid ${C.primary}` : "2px solid transparent",
                }}
              >
                {item.label}
              </button>
            );
          })}
        </nav>

        <div style={{ display: "flex", gap: "8px", alignItems: "center" }}>
          <button
            onClick={() => navigate("/contact")}
            style={{
              background: "transparent",
              border: "none",
              color: C.textMuted,
              fontSize: "12px",
              cursor: "pointer",
              padding: "6px 10px",
            }}
          >
            Contact
          </button>

          {!isLoggedIn ? (
            <button
              onClick={() => setShowLogin(true)}
              style={{
                background: C.gradient,
                color: "white",
                border: "none",
                borderRadius: "5px",
                padding: "7px 16px",
                fontSize: "12px",
                fontWeight: 700,
                cursor: "pointer",
              }}
            >
              Login
            </button>
          ) : (
            <button
              onClick={handleLogout}
              style={{
                background: "rgba(220,38,38,0.1)",
                color: C.danger,
                border: `1px solid rgba(220,38,38,0.3)`,
                borderRadius: "5px",
                padding: "7px 16px",
                fontSize: "12px",
                fontWeight: 600,
                cursor: "pointer",
              }}
            >
              Logout
            </button>
          )}
        </div>
      </header>

      <main style={{ padding: "32px 24px" }}>
        <Outlet />
      </main>

      {showLogin && <LoginModal onClose={() => setShowLogin(false)} />}
    </div>
  );
}
