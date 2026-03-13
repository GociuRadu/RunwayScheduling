import { Outlet, useNavigate } from "react-router-dom";
import SearchBar from "./SearchBar";
import { useState } from "react";
import LoginModal from "./Login";

const secondaryBtn = {
  background: "#232b33",
  color: "white",
  padding: "9px 22px",
  border: "none",
  borderRadius: "999px",
  cursor: "pointer",
  fontWeight: 600,
};

const primaryBtn = {
  ...secondaryBtn,
  background: "#e85a0c",
};

const dangerBtn = {
  ...secondaryBtn,
  background: "#c53030",
};

const logoBtn = {
  background: "transparent",
  border: "none",
  color: "white",
  fontSize: "20px",
  fontWeight: "bold",
  cursor: "pointer",
};

export default function AppShell() {
  const navigate = useNavigate();
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
    <div style={{ minHeight: "100vh", background: "#000" }}>
      <header
        style={{
          width: "100%",
          height: "80px",
          borderBottom: "1px solid #111111",
          display: "flex",
          alignItems: "center",
          justifyContent: "space-between",
          padding: "1px 10px",
          paddingRight: "30px",
          boxSizing: "border-box",
          gap: "20px",
        }}
      >
        <button onClick={() => navigate("/")} style={logoBtn}>
          RunwayScheduling
        </button>

        <div style={{ flex: 1, display: "flex", justifyContent: "center" }}>
          <div style={{ width: "100%", maxWidth: "700px" }}>
            <SearchBar onRequireLogin={() => setShowLogin(true)} />
          </div>
        </div>

        <div style={{ display: "flex", gap: "11px", flexWrap: "wrap" }}>
          <button onClick={() => navigate("/contact")} style={secondaryBtn}>
            Contact
          </button>

          {!isLoggedIn ? (
            <button onClick={() => setShowLogin(true)} style={primaryBtn}>
              Log In
            </button>
          ) : (
            <button onClick={handleLogout} style={dangerBtn}>
              Logout
            </button>
          )}
        </div>
      </header>

      <main style={{ padding: "30px" }}>
        <div style={{ marginTop: "30px" }}>
          <Outlet />
        </div>
      </main>

      {showLogin && <LoginModal onClose={() => setShowLogin(false)} />}
    </div>
  );
}
