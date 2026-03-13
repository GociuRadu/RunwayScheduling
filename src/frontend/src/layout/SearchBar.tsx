import { useState } from "react";
import { useNavigate } from "react-router-dom";

type Props = {
  onRequireLogin: () => void;
};

export default function SearchBar({ onRequireLogin }: Props) {
  const [open, setOpen] = useState(false);
  const navigate = useNavigate();

  const token = localStorage.getItem("token");
  const isAuthenticated = !!token;

  const items = [
    { label: "Airports", path: "/airports" },
    { label: "Scenario Config", path: "/scenario-config" },
  ];

  return (
    <div
      style={{
        width: "100%",
        position: "relative",
        display: "flex",
        justifyContent: "center",
      }}
    >
      <div style={{ width: "70%", position: "relative" }}>
        <button
          onClick={() => {
            if (!isAuthenticated) {
              onRequireLogin();
              return;
            }
            setOpen((prev) => !prev);
          }}
          style={{
            width: "100%",
            height: "52px",
            background: "rgba(5, 11, 16, 0.45)",
            border: "2px solid white",
            borderRadius: open ? "28px 28px 0 0" : "999px",
            cursor: "pointer",
            color: "white",
            fontSize: "17px",
            fontWeight: 500,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            backdropFilter: "blur(6px)",
          }}
        >
          {isAuthenticated ? "Menu" : "Authenticate first"}
        </button>

        {open && isAuthenticated && (
          <div
            style={{
              position: "absolute",
              top: "52px",
              left: 0,
              width: "100%",
              background: "rgba(11, 17, 23, 0.96)",
              border: "2px solid white",
              borderTop: "none",
              borderRadius: "0 0 28px 28px",
              padding: "18px 0",
              zIndex: 1000,
              boxShadow: "0 16px 40px rgba(0,0,0,0.45)",
            }}
          >
            {items.map((item) => (
              <button
                key={item.label}
                onClick={() => {
                  navigate(item.path);
                  setOpen(false);
                }}
                style={{
                  width: "100%",
                  background: "transparent",
                  color: "#d9e1ea",
                  border: "none",
                  padding: "18px 24px",
                  textAlign: "center",
                  cursor: "pointer",
                  fontSize: "18px",
                  fontWeight: 400,
                }}
              >
                {item.label}
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}