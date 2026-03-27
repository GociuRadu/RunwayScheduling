import { Outlet, useNavigate, useLocation } from "react-router-dom";
import { useState } from "react";
import LoginModal from "./Login";
import { C } from "../styles/tokens";

const NAV_ITEMS = [
  { label: "Airports", path: "/airports" },
  { label: "Scenarios", path: "/scenario-config" },
  { label: "Solver", path: "/solver" },
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
        runway<span style={{ color: C.primary, fontWeight: 300 }}>scheduling</span>
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
        className="glass-nav"
        style={{
          width: "100%",
          height: "56px",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "0 24px",
          boxSizing: "border-box",
          gap: "16px",
          position: "sticky",
          top: 0,
          zIndex: 100,
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
                  background: active ? "rgba(249,115,22,0.12)" : "transparent",
                  border: active ? "1px solid rgba(249,115,22,0.3)" : "1px solid transparent",
                  color: active ? C.primary : C.textSub,
                  fontSize: "13px",
                  fontWeight: active ? 700 : 400,
                  cursor: "pointer",
                  padding: "6px 14px",
                  borderRadius: "20px",
                  transition: "all 0.2s ease",
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
              className="glass-btn-primary"
              style={{ fontSize: "12px", padding: "7px 16px" }}
            >
              Login
            </button>
          ) : (
            <button
              onClick={handleLogout}
              className="glass-btn-danger"
              style={{ fontSize: "12px", padding: "7px 16px" }}
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
