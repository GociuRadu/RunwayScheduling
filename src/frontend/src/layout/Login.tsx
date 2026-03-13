import { useEffect, useState } from "react";
import { apiFetch } from "../lib/api";

type Props = {
  onClose: () => void;
};

type LoginResponse = {
  accessToken: string;
};

export default function LoginModal({ onClose }: Props) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberEmail, setRememberEmail] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const savedEmail = localStorage.getItem("savedEmail");
    const shouldRemember = localStorage.getItem("rememberEmail") === "true";

    if (savedEmail && shouldRemember) {
      setEmail(savedEmail);
      setRememberEmail(true);
    }
  }, []);

  async function handleLogin() {
    if (!email.trim() || !password.trim()) {
      setError("Please enter email and password");
      return;
    }

    try {
      setLoading(true);
      setError("");

      const response = await apiFetch("/api/login", {
        method: "POST",
        body: JSON.stringify({
          email: email.trim(),
          password,
        }),
      });

      if (!response.ok) {
        if (response.status === 401) {
          setError("Invalid email or password");
          return;
        }

        setError(`Login failed: ${response.status}`);
        return;
      }

      const data = (await response.json()) as LoginResponse;

      if (!data.accessToken) {
        setError("Token missing from response");
        return;
      }

      localStorage.setItem("token", data.accessToken);

      if (rememberEmail) {
        localStorage.setItem("savedEmail", email.trim());
        localStorage.setItem("rememberEmail", "true");
      } else {
        localStorage.removeItem("savedEmail");
        localStorage.removeItem("rememberEmail");
      }

      setPassword("");
      onClose();
      window.location.reload();
    } catch (err) {
      console.error("Login request failed:", err);
      setError("Login failed. Check API/CORS/network.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0, 0, 0, 0.6)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 2000,
      }}
    >
      <div
        style={{
          width: "420px",
          background: "#11181f",
          borderRadius: "24px",
          padding: "28px",
          color: "white",
          boxShadow: "0 20px 50px rgba(0,0,0,0.45)",
          border: "1px solid #222c35",
        }}
      >
        <div
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: "24px",
          }}
        >
          <h2 style={{ margin: 0, fontSize: "24px", fontWeight: 600 }}>
            Log In
          </h2>

          <button
            onClick={onClose}
            style={{
              background: "transparent",
              border: "none",
              color: "#9aa7b5",
              fontSize: "18px",
              cursor: "pointer",
            }}
          >
            ✕
          </button>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
          <input
            type="email"
            autoComplete="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleLogin()}
            style={{
              width: "100%",
              padding: "14px 16px",
              borderRadius: "14px",
              border: "1px solid #2b3946",
              background: "#0b1117",
              color: "white",
              fontSize: "15px",
              boxSizing: "border-box",
              outline: "none",
            }}
          />

          <input
            type="password"
            autoComplete="current-password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleLogin()}
            style={{
              width: "100%",
              padding: "14px 16px",
              borderRadius: "14px",
              border: "1px solid #2b3946",
              background: "#0b1117",
              color: "white",
              fontSize: "15px",
              boxSizing: "border-box",
              outline: "none",
            }}
          />

          <label
            style={{
              display: "flex",
              alignItems: "center",
              gap: "8px",
              fontSize: "14px",
              color: "#c7d0d9",
            }}
          >
            <input
              type="checkbox"
              checked={rememberEmail}
              onChange={(e) => setRememberEmail(e.target.checked)}
            />
            Remember email
          </label>

          {error && (
            <div style={{ color: "#ff7b7b", fontSize: "14px" }}>{error}</div>
          )}

          <button
            onClick={handleLogin}
            disabled={loading}
            style={{
              marginTop: "8px",
              width: "100%",
              padding: "14px",
              borderRadius: "999px",
              border: "none",
              background: "#e85a0c",
              color: "white",
              fontSize: "15px",
              fontWeight: 600,
              cursor: loading ? "not-allowed" : "pointer",
              opacity: loading ? 0.7 : 1,
            }}
          >
            {loading ? "Logging in..." : "Log In"}
          </button>
        </div>
      </div>
    </div>
  );
}