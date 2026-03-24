import { useEffect, useState } from "react";
import { apiFetch } from "../lib/api";
import { C, S } from "../styles/tokens";

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
      setError("Enter email and password");
      return;
    }

    try {
      setLoading(true);
      setError("");

      const response = await apiFetch("/api/login", {
        method: "POST",
        body: JSON.stringify({ email: email.trim(), password }),
      });

      if (!response.ok) {
        setError(response.status === 401 ? "Invalid email or password" : `Login failed: ${response.status}`);
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
      setError("Login failed. Check network/CORS.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div
      onClick={onClose}
      style={{
        position: "fixed",
        inset: 0,
        background: "rgba(0,0,0,0.75)",
        display: "flex",
        alignItems: "center",
        justifyContent: "center",
        zIndex: 2000,
      }}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        style={{
          width: "380px",
          maxWidth: "calc(100vw - 32px)",
          background: C.bgModal,
          borderRadius: "8px",
          padding: "28px",
          color: C.text,
          border: `1px solid ${C.border}`,
          boxShadow: "0 24px 60px rgba(0,0,0,0.6)",
        }}
      >
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "24px" }}>
          <div>
            <div style={{ fontSize: "11px", ...S.label, marginBottom: "4px" }}>RUNWAY SCHEDULING</div>
            <div style={{ fontSize: "20px", fontWeight: 800 }}>Sign in</div>
          </div>
          <button
            onClick={onClose}
            style={{ background: "transparent", border: "none", color: C.textMuted, fontSize: "16px", cursor: "pointer" }}
          >
            ✕
          </button>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: "12px" }}>
          <input
            type="email"
            autoComplete="email"
            placeholder="Email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleLogin()}
            style={S.input}
          />

          <input
            type="password"
            autoComplete="current-password"
            placeholder="Password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleLogin()}
            style={S.input}
          />

          <label style={{ display: "flex", alignItems: "center", gap: "8px", fontSize: "12px", color: C.textSub, cursor: "pointer" }}>
            <input
              type="checkbox"
              checked={rememberEmail}
              onChange={(e) => setRememberEmail(e.target.checked)}
            />
            Remember email
          </label>

          {error && (
            <div style={{ color: "#ff9d9d", fontSize: "12px" }}>{error}</div>
          )}

          <button
            onClick={handleLogin}
            disabled={loading}
            style={{
              ...S.primaryBtn,
              width: "100%",
              padding: "12px",
              borderRadius: "6px",
              fontSize: "14px",
              marginTop: "4px",
              opacity: loading ? 0.7 : 1,
              cursor: loading ? "not-allowed" : "pointer",
            }}
          >
            {loading ? "Signing in..." : "Sign in"}
          </button>
        </div>
      </div>
    </div>
  );
}
