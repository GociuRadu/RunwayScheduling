import { useState } from "react";
import { Link, Outlet } from "react-router-dom";
import { useNavigate } from "react-router-dom";

export default function AppShell() {
  const [menuOpen, setMenuOpen] = useState(false);
  const navigate = useNavigate();

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#000",
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        paddingTop: "25px",
      }}
    >
      {/* NOTCH */}
      <div
        style={{
          width: "600px",
          height: "50px",
          background: "#3e1a78",
          borderRadius: "30px",
          display: "grid",
          gridTemplateColumns: "1fr auto 1fr",
          alignItems: "center",
          color: "white",
          fontWeight: 600,
          padding: "0 20px",
        }}
      >
        {/* MENU */}
        <button
          onClick={() => setMenuOpen(!menuOpen)}
          style={{
            background: "transparent",
            border: "none",
            color: "white",
            cursor: "pointer",
            justifySelf: "start",
          }}
        >
          Menu
        </button>

        {/* TITLE */}
        <Link
          to="/"
          style={{
            textAlign: "center",
            transform: "translateX(-25px)",
            color: "white",
            textDecoration: "none",
          }}
        >
          RunwayScheduling
        </Link>

        {/* RIGHT BUTTONS */}
        <div
          style={{
            display: "flex",
            gap: "10px",
            justifySelf: "end",
          }}
        >
          <Link to="/contact">
            <button
              style={{
                background: "#ff3b3b",
                border: "none",
                borderRadius: "5px",
                color: "white",
                padding: "6px 14px",
                cursor: "pointer",
              }}
            >
              Contact
            </button>
          </Link>

          <Link to="/login">
            <button
              className="loginBtn"
              style={{
                background: "rgba(0, 132, 103, 0.8)",
                border: "none",
                borderRadius: "20px",
                color: "#3e1a78",
                padding: "6px 14px",
                cursor: "pointer",
              }}
            >
              {/* Only this text will animate, not the whole button */}
              <span className="loginBtnText">Login</span>
            </button>
          </Link>
        </div>
      </div>

      {/* MENU PANEL */}
      {menuOpen && (
        <div
          style={{
            marginTop: "20px",
            width: "800px",
            height: "400px",
            background: "#1a1a1a",
            borderRadius: "20px",
            color: "white",
            padding: "20px",
          }}
        >
          {/* Airports & Runways */}
          <div
            onClick={() => {
              setMenuOpen(false); // close menu
              navigate("/airports"); // go to Airports/Runways page
            }}
            style={{
              width: "320px",
              padding: "14px 16px",
              border: "1px solid rgba(255,255,255,0.18)",
              borderRadius: "10px",
              cursor: "pointer",
              userSelect: "none",
              marginBottom: "12px",
            }}
          >
            Airports & Runways
          </div>

          {/* Scenario Config */}
          <div
            onClick={() => {
              setMenuOpen(false); // close menu
              navigate("/scenario-config"); // go to ScenarioConfigPage
            }}
            style={{
              width: "320px",
              padding: "14px 16px",
              border: "1px solid rgba(255,255,255,0.18)",
              borderRadius: "10px",
              cursor: "pointer",
              userSelect: "none",
            }}
          >
            Generate Scenario Config
          </div>
        </div>
      )}
      
      {/* PAGE CONTENT */}
      <div
        style={{
          width: "600px",
          marginTop: "30px",
          textAlign: "left",
        }}
      >
        <Outlet />
      </div>
    </div>
  );
}
